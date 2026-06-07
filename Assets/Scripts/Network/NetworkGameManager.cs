using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

/// <summary>
/// Oyun durumu ve tur yönetimini ağ üzerinde senkronize eden merkezi yönetici.
/// Host Mode'da çalışır — tüm oyun kuralları sadece Host tarafında işlenir.
/// 
/// Mevcut GameManager.cs + TurnSystem.cs mantığının Fusion uyumlu birleşimi.
/// Sahneye tek bir NetworkObject olarak eklenir (Host tarafından spawn edilir).
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    // ============================================================
    // NETWORKED STATE — Tüm client'larda otomatik senkronize
    // ============================================================

    /// <summary> Şu anki oyun fazı </summary>
    [Networked] public GamePhase CurrentPhase { get; set; }

    /// <summary> Sırası olan oyuncunun PlayerRef'i </summary>
    [Networked] public PlayerRef ActivePlayerRef { get; set; }

    /// <summary> Sırası olan oyuncu (PlayerId enum ile) </summary>
    [Networked] public PlayerId ActivePlayerId { get; set; }

    /// <summary> Şu anki tur numarası </summary>
    [Networked] public int TurnNumber { get; set; }

    /// <summary> Tur zamanlayıcısı (Fusion tick tabanlı) </summary>
    [Networked] public TickTimer TurnTimer { get; set; }

    /// <summary> Player1'in Fusion PlayerRef'i </summary>
    [Networked] public PlayerRef Player1Ref { get; set; }

    /// <summary> Player2'nin Fusion PlayerRef'i </summary>
    [Networked] public PlayerRef Player2Ref { get; set; }

    /// <summary> İki oyuncu da bağlandı mı? </summary>
    [Networked] public NetworkBool AllPlayersReady { get; set; }

    // ============================================================
    // LOKAL REFERANSLAR
    // ============================================================

    [Header("Tur Ayarları")]
    [Tooltip("Bir oyuncunun tur süresi (saniye). 0 = sınırsız süre.")]
    [SerializeField] private float turnDuration = 60f;

    [Header("Test")]
    [Tooltip("Tek oyuncu ile test modu — 2. oyuncuyu beklemeden oyunu başlatır.")]
    [SerializeField] private bool singlePlayerTest = true;

    [Header("Referanslar")]
    [SerializeField] private HexGrid hexGrid;

    /// <summary> Sahadaki tüm NetworkShip'lerin listesi (lokal cache) </summary>
    private List<NetworkShip> _allShips = new List<NetworkShip>();

    // ============================================================
    // EVENTS — Lokal UI güncellemeleri için
    // ============================================================

    /// <summary> Tur değiştiğinde tetiklenir (lokal) </summary>
    public event Action<PlayerId> OnTurnChanged;

    /// <summary> Oyun fazı değiştiğinde tetiklenir (lokal) </summary>
    public event Action<GamePhase> OnPhaseChanged;

    /// <summary> Oyun bittiğinde tetiklenir (lokal) </summary>
    public event Action<PlayerId> OnGameEnded;

    /// <summary> Zamanlayıcı güncellendiğinde tetiklenir (lokal) </summary>
    public event Action<float> OnTimerUpdated;

    // ============================================================
    // SINGLETON ERIŞIM
    // ============================================================

    /// <summary> Sahnedeki aktif NetworkGameManager instance'ı </summary>
    public static NetworkGameManager Instance { get; private set; }

    // ============================================================
    // FUSION LIFECYCLE
    // ============================================================

    public override void Spawned()
    {
        Instance = this;

        // HexGrid'i bul (sahnede zaten var)
        if (hexGrid == null)
            hexGrid = FindAnyObjectByType<HexGrid>();

        if (Object.HasStateAuthority)
        {
            // Host: Başlangıç durumunu ayarla
            CurrentPhase = GamePhase.Deployment;
            TurnNumber = 1;
            ActivePlayerId = PlayerId.Player1;
            AllPlayersReady = false;

            Debug.Log("[NetworkGameManager] Host olarak spawn edildi. Oyuncular bekleniyor...");
        }
        else
        {
            Debug.Log("[NetworkGameManager] Client olarak spawn edildi.");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        // Sadece Host oyun mantığını çalıştırır
        if (!Object.HasStateAuthority) return;

        // Zamanlayıcı kontrolü
        if (CurrentPhase == GamePhase.Combat && turnDuration > 0 && TurnTimer.Expired(Runner))
        {
            Debug.Log($"[NetworkGameManager] Süre doldu! Oyuncu: {ActivePlayerId}");
            ServerEndTurn();
        }
    }

    public override void Render()
    {
        // Zamanlayıcı UI güncellemesi (her frame)
        if (CurrentPhase == GamePhase.Combat && turnDuration > 0 && TurnTimer.IsRunning)
        {
            float remaining = TurnTimer.RemainingTime(Runner) ?? 0f;
            OnTimerUpdated?.Invoke(remaining);
        }
    }

    // ============================================================
    // OYUNCU BAĞLANTI YÖNETİMİ
    // ============================================================

    /// <summary>
    /// Yeni oyuncu bağlandığında Host tarafından çağrılır.
    /// HexNetworkBootstrap.OnPlayerJoined → burayı çağırmalı.
    /// </summary>
    public void ServerPlayerJoined(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (Player1Ref == default)
        {
            Player1Ref = player;
            Debug.Log($"[NetworkGameManager] Player1 bağlandı: {player}");

            // Tek oyuncu test modu: Player1 bağlandığında direkt başlat
            if (singlePlayerTest)
            {
                Debug.Log("[NetworkGameManager] Tek oyuncu test modu — oyun hemen başlatılıyor.");
                AllPlayersReady = true;
                ServerStartGame();
            }
        }
        else if (Player2Ref == default)
        {
            Player2Ref = player;
            Debug.Log($"[NetworkGameManager] Player2 bağlandı: {player}");

            // İki oyuncu da hazır — oyunu başlat
            AllPlayersReady = true;
            ServerStartGame();
        }
        else
        {
            Debug.LogWarning($"[NetworkGameManager] Fazla oyuncu! {player} reddedildi.");
        }
    }

    /// <summary>
    /// Oyuncu ayrıldığında Host tarafından çağrılır.
    /// </summary>
    public void ServerPlayerLeft(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        if (player == Player1Ref)
        {
            Debug.Log($"[NetworkGameManager] Player1 ayrıldı: {player}");
            if (CurrentPhase == GamePhase.Combat)
                ServerEndGame(PlayerId.Player2);
        }
        else if (player == Player2Ref)
        {
            Debug.Log($"[NetworkGameManager] Player2 ayrıldı: {player}");
            if (CurrentPhase == GamePhase.Combat)
                ServerEndGame(PlayerId.Player1);
        }
    }

    // ============================================================
    // HOST-ONLY GAME LOGIC
    // ============================================================

    /// <summary> Oyunu başlatır. Sadece Host çağırır. </summary>
    private void ServerStartGame()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("[NetworkGameManager] Oyun başlıyor!");

        // Şimdilik direkt Combat fazına geç (Deployment fazı Faz 3'te)
        CurrentPhase = GamePhase.Combat;
        TurnNumber = 1;
        ActivePlayerId = PlayerId.Player1;
        ActivePlayerRef = Player1Ref;

        // Sahnedeki gemilere sahip ata
        AssignShipOwners();

        // Zamanlayıcıyı başlat
        if (turnDuration > 0)
        {
            TurnTimer = TickTimer.CreateFromSeconds(Runner, turnDuration);
        }

        // Tüm client'lara bildir
        RPC_OnGameStarted();
    }

    /// <summary> Sahnedeki gemilere OwnerPlayer atar. Tek oyuncu modunda hepsi Player1'e aittir. </summary>
    private void AssignShipOwners()
    {
        var allShips = FindObjectsByType<NetworkShip>(FindObjectsSortMode.None);
        foreach (var ship in allShips)
        {
            if (ship.OwnerPlayer == default)
            {
                ship.OwnerPlayer = Player1Ref;
                Debug.Log($"[NetworkGameManager] {ship.name} → Owner: {Player1Ref}");
            }

            // _allShips listesine ekle
            if (!_allShips.Contains(ship))
                _allShips.Add(ship);
        }
    }

    /// <summary> Sırayı değiştirir. Sadece Host çağırır. </summary>
    public void ServerEndTurn()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentPhase != GamePhase.Combat) return;

        PlayerId previousPlayer = ActivePlayerId;

        // Sırayı değiştir
        if (ActivePlayerId == PlayerId.Player1)
        {
            ActivePlayerId = PlayerId.Player2;
            ActivePlayerRef = Player2Ref;
        }
        else
        {
            ActivePlayerId = PlayerId.Player1;
            ActivePlayerRef = Player1Ref;

            // Player2 → Player1 geçişinde yeni tur
            TurnNumber++;
        }

        // Yeni oyuncunun gemilerinin aksiyonlarını sıfırla
        ServerResetShipActions(ActivePlayerId);

        // Zamanlayıcıyı yeniden başlat
        if (turnDuration > 0)
        {
            TurnTimer = TickTimer.CreateFromSeconds(Runner, turnDuration);
        }

        Debug.Log($"[NetworkGameManager] Sıra geçti: {ActivePlayerId} | Tur: {TurnNumber}");

        // Tüm client'lara bildir
        RPC_OnTurnChanged(ActivePlayerId, TurnNumber);
    }

    /// <summary> Oyunu bitirir. Sadece Host çağırır. </summary>
    public void ServerEndGame(PlayerId winner)
    {
        if (!Object.HasStateAuthority) return;

        CurrentPhase = GamePhase.Resolution;
        TurnTimer = default; // Zamanlayıcıyı durdur

        Debug.Log($"[NetworkGameManager] Oyun bitti! Kazanan: {winner}");

        RPC_OnGameEnded(winner);
    }

    /// <summary> Kazanma koşulunu kontrol eder. Sadece Host çağırır. </summary>
    public void ServerCheckWinCondition()
    {
        if (!Object.HasStateAuthority) return;
        if (CurrentPhase != GamePhase.Combat) return;

        int player1Alive = 0;
        int player2Alive = 0;

        foreach (var ship in _allShips)
        {
            if (ship == null || !ship.IsAlive) continue;

            var ownerPlayerId = GetPlayerId(ship.OwnerPlayer);
            if (ownerPlayerId == PlayerId.Player1) player1Alive++;
            else if (ownerPlayerId == PlayerId.Player2) player2Alive++;
        }

        if (player1Alive <= 0)
            ServerEndGame(PlayerId.Player2);
        else if (player2Alive <= 0)
            ServerEndGame(PlayerId.Player1);
    }

    /// <summary> Belirtilen oyuncunun tüm gemilerinin aksiyonlarını sıfırlar </summary>
    private void ServerResetShipActions(PlayerId playerId)
    {
        foreach (var ship in _allShips)
        {
            if (ship != null && ship.IsAlive && GetPlayerId(ship.OwnerPlayer) == playerId)
            {
                ship.ServerResetActions();
            }
        }
    }

    // ============================================================
    // YARDIMCI METODLAR
    // ============================================================

    /// <summary> PlayerRef → PlayerId dönüşümü </summary>
    public PlayerId GetPlayerId(PlayerRef playerRef)
    {
        if (playerRef == Player1Ref) return PlayerId.Player1;
        if (playerRef == Player2Ref) return PlayerId.Player2;
        return PlayerId.None;
    }

    /// <summary> PlayerId → PlayerRef dönüşümü </summary>
    public PlayerRef GetPlayerRef(PlayerId playerId)
    {
        return playerId switch
        {
            PlayerId.Player1 => Player1Ref,
            PlayerId.Player2 => Player2Ref,
            _ => default
        };
    }

    /// <summary> Verilen oyuncunun şu an sırası mı? </summary>
    public bool IsPlayerTurn(PlayerRef playerRef)
    {
        return ActivePlayerRef == playerRef;
    }

    /// <summary> Verilen oyuncunun şu an sırası mı? (PlayerId ile) </summary>
    public bool IsPlayerTurn(PlayerId playerId)
    {
        return ActivePlayerId == playerId;
    }

    /// <summary> HexGrid referansı </summary>
    public HexGrid HexGrid => hexGrid;

    /// <summary> Kalan tur süresi (saniye) </summary>
    public float RemainingTime => TurnTimer.IsRunning ? (TurnTimer.RemainingTime(Runner) ?? 0f) : 0f;

    /// <summary> Bir NetworkShip'i listeye kaydet </summary>
    public void RegisterShip(NetworkShip ship)
    {
        if (!_allShips.Contains(ship))
            _allShips.Add(ship);
    }

    /// <summary> Bir NetworkShip'i listeden çıkar </summary>
    public void UnregisterShip(NetworkShip ship)
    {
        _allShips.Remove(ship);
    }

    // ============================================================
    // RPC'LER — Host → Tüm Client'lar bildirimleri
    // ============================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnGameStarted(RpcInfo info = default)
    {
        Debug.Log("[NetworkGameManager] RPC: Oyun başladı!");
        OnPhaseChanged?.Invoke(GamePhase.Combat);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnTurnChanged(PlayerId newPlayerId, int turnNumber, RpcInfo info = default)
    {
        Debug.Log($"[NetworkGameManager] RPC: Sıra değişti → {newPlayerId}, Tur: {turnNumber}");
        OnTurnChanged?.Invoke(newPlayerId);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnGameEnded(PlayerId winner, RpcInfo info = default)
    {
        Debug.Log($"[NetworkGameManager] RPC: Oyun bitti! Kazanan: {winner}");
        OnGameEnded?.Invoke(winner);
    }

    /// <summary>
    /// Client → Host: Sırayı bitirmek istiyorum.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestEndTurn(RpcInfo info = default)
    {
        // Güvenlik: Sadece sırası olan oyuncu tur bitirebilir
        if (info.Source != ActivePlayerRef)
        {
            Debug.LogWarning($"[NetworkGameManager] Yetkisiz tur bitirme talebi: {info.Source}");
            return;
        }

        ServerEndTurn();
    }
}

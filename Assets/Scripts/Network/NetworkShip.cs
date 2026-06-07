using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;

/// <summary>
/// Geminin ağ üzerinde senkronize edilen durumu.
/// Bu script NetworkBehaviour olarak her gemi prefab'ına eklenir.
/// Tüm oyun mantığı (HP, XP, hareket, aksiyon) burada tutulur.
/// 
/// Host Mode'da çalışır:
/// - Sadece Host/Server state'i değiştirebilir
/// - Client'lar RPC ile talep gönderir
/// - State değişiklikleri otomatik olarak tüm client'lara yayılır
/// 
/// Bu script, mevcut Ship.cs'deki mantığın Fusion uyumlu versiyonudur.
/// Görsel işlemler (animasyon, glow) ShipView.cs tarafından yapılır.
/// </summary>
public class NetworkShip : NetworkBehaviour
{
    // ============================================================
    // NETWORKED STATE — Tüm client'larda otomatik senkronize
    // ============================================================

    /// <summary> Mevcut can puanı </summary>
    [Networked] public int CurrentHP { get; set; }

    /// <summary> Mevcut XP </summary>
    [Networked] public int CurrentXP { get; set; }

    /// <summary> Mevcut seviye (1-10) </summary>
    [Networked] public int Level { get; set; }

    /// <summary> Tur içi aksiyon durumu </summary>
    [Networked] public ShipActionState ActionState { get; set; }

    /// <summary> Geminin bulunduğu hex koordinatı </summary>
    [Networked] public Vector3Int HexPosition { get; set; }

    /// <summary> Gemi sahibi oyuncu (Fusion PlayerRef) </summary>
    [Networked] public PlayerRef OwnerPlayer { get; set; }

    /// <summary> Gemi tipi indeksi — ShipData SO'yu tanımlar </summary>
    [Networked] public int ShipTypeIndex { get; set; }

    // ============================================================
    // LOKAL REFERANSLAR — Networked değil
    // ============================================================

    [Header("Gemi Verileri")]
    [Tooltip("Bu geminin base stat'larını tutan ScriptableObject")]
    [SerializeField] private ShipData shipData;

    [Header("Gemi Tipleri Listesi")]
    [Tooltip("Tüm gemi tipleri — ShipTypeIndex ile erişilir")]
    [SerializeField] private ShipData[] allShipTypes;

    /// <summary> ShipView referansı (görsel katman) </summary>
    private ShipView _shipView;

    // ============================================================
    // HESAPLANAN PROPERTIES — ShipData + Level bonusu
    // ============================================================

    /// <summary> Bu geminin base stat verileri </summary>
    public ShipData Data => shipData;

    /// <summary> Maksimum can puanı (base + seviye bonusu) </summary>
    public int MaxHP => shipData != null ? shipData.BaseHP + GetLevelBonus(shipData.BaseHP) : 100;

    /// <summary> Mevcut saldırı gücü (base + seviye bonusu) </summary>
    public int AttackPower => shipData != null ? shipData.BaseAttack + GetLevelBonus(shipData.BaseAttack) : 25;

    /// <summary> Saldırı menzili </summary>
    public int AttackRange => shipData != null ? shipData.AttackRange : 1;

    /// <summary> Hareket puanı (BFS maliyet bütçesi) </summary>
    public int MovementPoints => shipData != null ? shipData.MovementPoints + (Level - 1) : 15;

    /// <summary> Sonraki seviye için gereken XP </summary>
    public int XPForNextLevel => Level * Level * 100;

    /// <summary> Gemi hayatta mı? </summary>
    public bool IsAlive => CurrentHP > 0;

    /// <summary> Bu tur için hâlâ aksiyon yapabilir mi? </summary>
    public bool CanAct => ActionState != ShipActionState.Done && IsAlive;

    /// <summary> Hareket edebilir mi? </summary>
    public bool CanMove => (ActionState == ShipActionState.Idle || ActionState == ShipActionState.Attacked) && IsAlive;

    /// <summary> Saldırabilir mi? </summary>
    public bool CanAttack => (ActionState == ShipActionState.Idle || ActionState == ShipActionState.Moved) && IsAlive;

    // ============================================================
    // FUSION LIFECYCLE
    // ============================================================

    /// <summary>
    /// Fusion tarafından NetworkObject spawn edildiğinde çağrılır.
    /// Awake/Start yerine bu metod kullanılır — networked property'ler
    /// ancak Spawned() sonrasında geçerlidir.
    /// </summary>
    public override void Spawned()
    {
        _shipView = GetComponent<ShipView>();

        // ShipTypeIndex'ten ShipData'yı çöz
        ResolveShipData();

        // ShipView'a animasyon sürelerini ayarla
        if (_shipView != null && shipData != null)
        {
            _shipView.SetAnimationDurations(shipData.MovementDuration, shipData.RotationDuration);
        }

        // Host: İlk state'i ayarla (sadece ilk spawn'da)
        if (Object.HasStateAuthority && CurrentHP <= 0)
        {
            CurrentHP = MaxHP;
            Level = 1;
            CurrentXP = 0;
            ActionState = ShipActionState.Idle;

            // OwnerPlayer henüz atanmamışsa, Player1'e ata (test modu için)
            if (OwnerPlayer == default && NetworkGameManager.Instance != null)
            {
                OwnerPlayer = NetworkGameManager.Instance.Player1Ref;
            }
        }

        // NetworkGameManager'a kaydet
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RegisterShip(this);
        }

        Debug.Log($"[NetworkShip] {name} spawn edildi. Owner: {OwnerPlayer}, HP: {CurrentHP}/{MaxHP}");
    }

    /// <summary>
    /// Her Fusion tick'inde çağrılır (FixedUpdate yerine).
    /// Sadece State Authority (Host) üzerinde oyun mantığı çalıştırılır.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // Şimdilik boş — ileride otomatik tur kontrolleri eklenebilir
    }

    /// <summary>
    /// Her Unity frame'inde çağrılır.
    /// Görsel güncellemeler burada yapılır (interpolasyon, UI).
    /// </summary>
    public override void Render()
    {
        // ShipView var mı ve networked position değiştiyse
        // animasyon tetikleme burada yapılabilir
    }

    // ============================================================
    // HOST-ONLY GAME LOGIC — Sadece sunucu tarafında çalışır
    // ============================================================

    /// <summary>
    /// Gemiyi verilen hex'e taşır. Sadece Host çağırabilir.
    /// Path doğrulaması yapıldıktan sonra çağrılmalıdır.
    /// </summary>
    public void ServerMove(Vector3Int targetHex, List<Vector3Int> path, HexGrid hexGrid)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[NetworkShip] ServerMove sadece Host tarafından çağrılabilir!");
            return;
        }

        if (!CanMove)
        {
            Debug.LogWarning($"[NetworkShip] {name} hareket edemez! Durum: {ActionState}");
            return;
        }

        // State güncelle
        HexPosition = targetHex;
        ActionState = ActionState == ShipActionState.Attacked
            ? ShipActionState.Done
            : ShipActionState.Moved;

        // Tüm client'lara hareket animasyonu göster
        var worldPositions = path
            .Select(pos => hexGrid.GetTileAt(pos).transform.position)
            .ToList();

        RPC_PlayMoveAnimation(worldPositions.ToArray());

        Debug.Log($"[NetworkShip] {name} → {targetHex} konumuna taşındı. Yeni durum: {ActionState}");
    }

    /// <summary> Gemiye hasar verir. Sadece Host çağırabilir. </summary>
    public void ServerTakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        if (!IsAlive) return;

        int actualDamage = Mathf.Max(1, damage);
        CurrentHP = Mathf.Max(0, CurrentHP - actualDamage);

        Debug.Log($"[NetworkShip] {name} hasar aldı: -{actualDamage} (Kalan HP: {CurrentHP}/{MaxHP})");

        if (!IsAlive)
        {
            Debug.Log($"[NetworkShip] {name} batırıldı!");
            // TODO: Batırma animasyonu, GameManager'a bildirim
        }
    }

    /// <summary> Gemiyi iyileştirir. Sadece Host çağırabilir. </summary>
    public void ServerHeal(int amount)
    {
        if (!Object.HasStateAuthority) return;
        if (!IsAlive) return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
    }

    /// <summary> Gemiye XP ekler. Seviye atlatır. Sadece Host çağırabilir. </summary>
    public void ServerAddXP(int amount)
    {
        if (!Object.HasStateAuthority) return;
        if (!IsAlive) return;

        CurrentXP += amount;
        Debug.Log($"[NetworkShip] {name} +{amount} XP (Toplam: {CurrentXP})");

        // Seviye atlama kontrolü
        while (CurrentXP >= XPForNextLevel && Level < 10)
        {
            CurrentXP -= XPForNextLevel;
            Level++;
            CurrentHP = MaxHP; // Seviye atladığında full HP
            Debug.Log($"[NetworkShip] {name} seviye atladı! Yeni seviye: {Level}");
        }
    }

    /// <summary> Aksiyon durumunu değiştirir. Sadece Host çağırabilir. </summary>
    public void ServerSetActionState(ShipActionState newState)
    {
        if (!Object.HasStateAuthority) return;
        ActionState = newState;
    }

    /// <summary> Tur başında aksiyon durumunu sıfırlar. Sadece Host çağırabilir. </summary>
    public void ServerResetActions()
    {
        if (!Object.HasStateAuthority) return;
        ActionState = ShipActionState.Idle;
    }

    // ============================================================
    // RPC'LER — Client → Host talep, Host → Client bildirim
    // ============================================================

    /// <summary>
    /// Client → Host: "Bu hex'e taşınmak istiyorum" talebi.
    /// Host doğrulama yapıp uygunsa ServerMove() çağırır.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestMove(Vector3Int targetHex, RpcInfo info = default)
    {
        // Bu kod sadece Host'ta çalışır
        // Host modunda lokal RPC çağrısında info.Source boş gelebilir
        PlayerRef sender = info.Source == default ? Runner.LocalPlayer : info.Source;
        Debug.Log($"[NetworkShip] Hareket talebi: {sender} → {targetHex}");

        // Güvenlik: Talebi gönderen, geminin sahibi mi?
        if (sender != OwnerPlayer)
        {
            Debug.LogWarning($"[NetworkShip] Yetkisiz hareket talebi! {sender} != {OwnerPlayer}");
            return;
        }

        // Hareket edebilir mi?
        if (!CanMove)
        {
            Debug.LogWarning($"[NetworkShip] {name} hareket edemez! Durum: {ActionState}");
            return;
        }

        // Sıra kontrolü
        if (NetworkGameManager.Instance != null && !NetworkGameManager.Instance.IsPlayerTurn(info.Source))
        {
            Debug.LogWarning($"[NetworkShip] {info.Source} — şu an sırası değil!");
            return;
        }

        // HexGrid referansı al
        var hexGrid = NetworkGameManager.Instance?.HexGrid;
        if (hexGrid == null)
        {
            hexGrid = UnityEngine.Object.FindAnyObjectByType<HexGrid>();
        }

        if (hexGrid == null)
        {
            Debug.LogError("[NetworkShip] HexGrid bulunamadı!");
            return;
        }

        // BFS menzil kontrolü
        Vector3Int currentPos = hexGrid.GetClosestHex(transform.position);
        BFSResult bfs = GraphSearch.BFSGetRange(hexGrid, currentPos, MovementPoints);

        if (!bfs.IsHexPositionInRange(targetHex))
        {
            Debug.LogWarning($"[NetworkShip] Hedef hex menzil dışında! {targetHex}");
            return;
        }

        // Yolu al
        List<Vector3Int> path = bfs.GetPathTo(targetHex);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[NetworkShip] Hedefe yol bulunamadı! {targetHex}");
            return;
        }

        // Host onayladı — hareketi gerçekleştir
        ServerMove(targetHex, path, hexGrid);
    }

    /// <summary>
    /// Host → Tüm Client'lar: "Bu yol üzerinden hareket animasyonu oynat".
    /// State zaten güncellenmiştir, bu sadece görsel bildirimidir.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayMoveAnimation(Vector3[] worldPositions, RpcInfo info = default)
    {
        if (_shipView != null)
        {
            _shipView.AnimateMovement(worldPositions.ToList());
        }
    }

    // ============================================================
    // YARDIMCI METODLAR
    // ============================================================

    /// <summary> Seviye bonusu hesaplar (%5 per level) </summary>
    private int GetLevelBonus(int baseValue)
    {
        return Mathf.RoundToInt(baseValue * (Level - 1) * 0.05f);
    }

    /// <summary> ShipTypeIndex'e göre ShipData SO'yu çözer </summary>
    private void ResolveShipData()
    {
        if (allShipTypes != null && ShipTypeIndex >= 0 && ShipTypeIndex < allShipTypes.Length)
        {
            shipData = allShipTypes[ShipTypeIndex];
        }

        if (shipData == null)
        {
            Debug.LogWarning($"[NetworkShip] {name} için ShipData bulunamadı! (Index: {ShipTypeIndex})");
        }
    }

    /// <summary>
    /// Verilen Fusion PlayerRef'in bu geminin sahibi olup olmadığını kontrol eder.
    /// </summary>
    public bool IsOwnedBy(PlayerRef player)
    {
        return OwnerPlayer == player;
    }
}

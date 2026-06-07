using System;
using UnityEngine;

/// <summary>
/// Oyunun merkezi yönetim singleton'ı.
/// Oyun durumunu (faz, oyuncular, sıra) yönetir ve tüm alt sistemleri koordine eder.
/// Sahneler arasında yok edilmez (DontDestroyOnLoad).
/// </summary>
public class GameManager : MonoBehaviour
{
    // --- Singleton ---
    public static GameManager Instance { get; private set; }

    [Header("Oyuncu Verileri")]
    [SerializeField] private PlayerData player1 = new PlayerData(PlayerId.Player1, "Oyuncu 1", Color.blue);
    [SerializeField] private PlayerData player2 = new PlayerData(PlayerId.Player2, "Oyuncu 2", Color.red);

    [Header("Referanslar")]
    [SerializeField] private TurnSystem turnSystem;

    [Header("Oyun Durumu")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Deployment;

    // --- Events ---

    /// <summary> Oyun fazı değiştiğinde tetiklenir </summary>
    public event Action<GamePhase> OnPhaseChanged;

    /// <summary> Oyun başladığında tetiklenir </summary>
    public event Action OnGameStarted;

    /// <summary> Oyun bittiğinde tetiklenir. Parametre: kazanan oyuncu </summary>
    public event Action<PlayerId> OnGameEnded;

    // --- Properties ---

    /// <summary> Player 1 verileri </summary>
    public PlayerData Player1 => player1;

    /// <summary> Player 2 verileri </summary>
    public PlayerData Player2 => player2;

    /// <summary> Şu anki oyun fazı </summary>
    public GamePhase CurrentPhase => currentPhase;

    /// <summary> Tur sistemi referansı </summary>
    public TurnSystem TurnSystem => turnSystem;

    /// <summary> Şu an sırası olan oyuncunun verileri </summary>
    public PlayerData ActivePlayer
    {
        get
        {
            if (turnSystem == null) return player1;
            return turnSystem.CurrentPlayer == PlayerId.Player1 ? player1 : player2;
        }
    }

    // --- Unity Lifecycle ---

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // TurnSystem yoksa aynı obje üzerinde ara
        if (turnSystem == null)
            turnSystem = GetComponent<TurnSystem>();
    }

    private void Start()
    {
        if (turnSystem != null)
        {
            turnSystem.OnTurnChanged += HandleTurnChanged;
        }
        StartGame();
    }

    private void OnDestroy()
    {
        if (turnSystem != null)
        {
            turnSystem.OnTurnChanged -= HandleTurnChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    private void HandleTurnChanged(PlayerId newPlayerId)
    {
        // Sırası gelen oyuncunun gemilerinin aksiyon durumlarını sıfırla
        PlayerData activePD = GetPlayerData(newPlayerId);
        if (activePD != null)
        {
            activePD.ResetAllShipActions();
            Debug.Log($"[GameManager] {newPlayerId} sırası başladı. Gemilerin aksiyon durumları sıfırlandı.");
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Oyunu başlatır. Deployment fazından başlar.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[GameManager] Oyun başlıyor!");

        // Fazı ayarla — şimdilik direkt Combat'a geçiyoruz
        // Deployment fazı Faz 3'te implemente edilecek
        SetPhase(GamePhase.Combat);

        // Tur sistemini başlat
        if (turnSystem != null)
        {
            turnSystem.Initialize();
        }

        OnGameStarted?.Invoke();
    }

    /// <summary>
    /// Oyun fazını değiştirir.
    /// </summary>
    public void SetPhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase)
            return;

        currentPhase = newPhase;
        Debug.Log($"[GameManager] Faz değişti: {newPhase}");
        OnPhaseChanged?.Invoke(newPhase);
    }

    /// <summary>
    /// Belirtilen oyuncunun PlayerData'sını döndürür.
    /// </summary>
    public PlayerData GetPlayerData(PlayerId playerId)
    {
        return playerId switch
        {
            PlayerId.Player1 => player1,
            PlayerId.Player2 => player2,
            _ => null
        };
    }

    /// <summary>
    /// Rakip oyuncunun PlayerData'sını döndürür.
    /// </summary>
    public PlayerData GetOpponentData(PlayerId playerId)
    {
        return playerId switch
        {
            PlayerId.Player1 => player2,
            PlayerId.Player2 => player1,
            _ => null
        };
    }

    /// <summary>
    /// Aktif oyuncunun sırasında olup olmadığını kontrol eder.
    /// </summary>
    public bool IsCurrentPlayerTurn(PlayerId playerId)
    {
        return turnSystem != null && turnSystem.IsPlayerTurn(playerId);
    }

    /// <summary>
    /// Oyunu bitirir ve kazananı bildirir.
    /// </summary>
    public void EndGame(PlayerId winner)
    {
        SetPhase(GamePhase.Resolution);

        if (turnSystem != null)
            turnSystem.PauseTimer();

        string winnerName = GetPlayerData(winner)?.PlayerName ?? "Bilinmeyen";
        Debug.Log($"[GameManager] Oyun bitti! Kazanan: {winnerName}");

        OnGameEnded?.Invoke(winner);
    }

    /// <summary>
    /// Bir oyuncunun tüm gemileri batırıldı mı kontrol eder.
    /// Eğer evet ise oyunu bitirir.
    /// </summary>
    public void CheckWinCondition()
    {
        if (currentPhase != GamePhase.Combat)
            return;

        if (!player1.HasShipsRemaining())
        {
            EndGame(PlayerId.Player2);
        }
        else if (!player2.HasShipsRemaining())
        {
            EndGame(PlayerId.Player1);
        }
    }
}

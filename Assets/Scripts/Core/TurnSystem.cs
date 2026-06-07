using System;
using UnityEngine;

/// <summary>
/// Sıra tabanlı tur yönetim sistemi.
/// İki oyuncu arasındaki sıra geçişini ve tur zamanlayıcısını yönetir.
/// Inspector'dan tur süresi ayarlanabilir.
/// </summary>
public class TurnSystem : MonoBehaviour
{
    [Header("Tur Ayarları")]
    [Tooltip("Bir oyuncunun tur süresi (saniye). 0 = sınırsız süre.")]
    [SerializeField] private float turnDuration = 60f;

    [Header("Durum (Read-Only)")]
    [SerializeField] private PlayerId currentPlayer = PlayerId.Player1;
    [SerializeField] private int turnNumber = 1;
    [SerializeField] private float remainingTime;

    private bool isTimerRunning = false;

    // --- Events ---

    /// <summary> Sıra değiştiğinde tetiklenir. Parametre: yeni aktif oyuncu </summary>
    public event Action<PlayerId> OnTurnChanged;

    /// <summary> Tur zamanlayıcısı her saniye güncellendiğinde tetiklenir. Parametre: kalan süre </summary>
    public event Action<float> OnTimerUpdated;

    /// <summary> Tur süresi dolduğunda tetiklenir </summary>
    public event Action OnTimerExpired;

    /// <summary> Tur numarası değiştiğinde tetiklenir. Parametre: yeni tur numarası </summary>
    public event Action<int> OnTurnNumberChanged;

    // --- Properties ---

    /// <summary> Şu an sırası olan oyuncu </summary>
    public PlayerId CurrentPlayer => currentPlayer;

    /// <summary> Şu anki tur numarası (her iki oyuncu da oynadığında 1 artar) </summary>
    public int TurnNumber => turnNumber;

    /// <summary> Kalan süre (saniye) </summary>
    public float RemainingTime => remainingTime;

    /// <summary> Tur zamanlayıcısı aktif mi? </summary>
    public bool IsTimerRunning => isTimerRunning;

    /// <summary> Inspector'dan ayarlanmış tur süresi </summary>
    public float TurnDuration => turnDuration;

    // --- Public Methods ---

    /// <summary>
    /// Tur sistemini başlatır. İlk oyuncu Player1 olarak ayarlanır.
    /// </summary>
    public void Initialize()
    {
        currentPlayer = PlayerId.Player1;
        turnNumber = 1;
        StartTimer();
        OnTurnChanged?.Invoke(currentPlayer);
        OnTurnNumberChanged?.Invoke(turnNumber);
    }

    /// <summary>
    /// Sırayı diğer oyuncuya geçirir.
    /// Player2'nin sırası bittikten sonra tur numarası artar.
    /// </summary>
    public void EndTurn()
    {
        PlayerId previousPlayer = currentPlayer;

        // Sırayı değiştir
        currentPlayer = currentPlayer == PlayerId.Player1
            ? PlayerId.Player2
            : PlayerId.Player1;

        // Player2 → Player1 geçişinde yeni tur
        if (previousPlayer == PlayerId.Player2)
        {
            turnNumber++;
            OnTurnNumberChanged?.Invoke(turnNumber);
        }

        // Zamanlayıcıyı yeniden başlat
        StartTimer();

        OnTurnChanged?.Invoke(currentPlayer);

        Debug.Log($"[TurnSystem] Sıra geçti: {currentPlayer} | Tur: {turnNumber}");
    }

    /// <summary>
    /// Verilen oyuncunun şu an sırası mı kontrol eder.
    /// </summary>
    public bool IsPlayerTurn(PlayerId playerId)
    {
        return currentPlayer == playerId;
    }

    /// <summary>
    /// Zamanlayıcıyı duraklatır (örn: animasyon sırasında).
    /// </summary>
    public void PauseTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// Duraklatılmış zamanlayıcıyı devam ettirir.
    /// </summary>
    public void ResumeTimer()
    {
        if (turnDuration > 0)
            isTimerRunning = true;
    }

    // --- Private Methods ---

    private void Update()
    {
        if (!isTimerRunning || turnDuration <= 0)
            return;

        remainingTime -= Time.deltaTime;
        OnTimerUpdated?.Invoke(remainingTime);

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isTimerRunning = false;
            Debug.Log($"[TurnSystem] Süre doldu! Oyuncu: {currentPlayer}");
            OnTimerExpired?.Invoke();
            EndTurn();
        }
    }

    private void StartTimer()
    {
        if (turnDuration > 0)
        {
            remainingTime = turnDuration;
            isTimerRunning = true;
        }
        else
        {
            // Süre 0 ise zamanlayıcı devre dışı
            remainingTime = 0f;
            isTimerRunning = false;
        }
    }
}

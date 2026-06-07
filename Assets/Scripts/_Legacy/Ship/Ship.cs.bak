using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sahne üzerindeki bir gemi instance'ını temsil eder.
/// Unit sınıfının yerini alır — hareket, seçim, sahiplik, HP, XP, seviye ve aksiyon yönetimi içerir.
/// Base stat'ları ShipData ScriptableObject'ten alır.
/// </summary>
[SelectionBase]
public class Ship : MonoBehaviour
{
    [Header("Gemi Verileri")]
    [Tooltip("Bu geminin base stat'larını tutan ScriptableObject")]
    [SerializeField] private ShipData shipData;

    [Header("Sahiplik")]
    [SerializeField] private PlayerId ownerPlayerId = PlayerId.None;

    [Header("Runtime Durumu")]
    [SerializeField] private int currentHP;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int level = 1;
    [SerializeField] private ShipActionState actionState = ShipActionState.Idle;

    [Header("Animasyon (Override)")]
    [Tooltip("0 ise ShipData'daki değer kullanılır")]
    [SerializeField] private float movementDurationOverride = 0f;
    [SerializeField] private float rotationDurationOverride = 0f;

    // --- Private ---
    private GlowHighlight glowHighlight;
    private Queue<Vector3> pathPositions = new Queue<Vector3>();

    // --- Events ---

    /// <summary> Hareket tamamlandığında tetiklenir </summary>
    public event Action<Ship> MovementFinished;

    /// <summary> Gemi hasar aldığında tetiklenir. Params: (Ship, hasarMiktarı) </summary>
    public event Action<Ship, int> OnDamageTaken;

    /// <summary> Gemi batırıldığında tetiklenir </summary>
    public event Action<Ship> OnDestroyed;

    /// <summary> Aksiyon durumu değiştiğinde tetiklenir </summary>
    public event Action<Ship, ShipActionState> OnActionStateChanged;

    // --- Properties ---

    /// <summary> Bu geminin base stat verileri </summary>
    public ShipData Data => shipData;

    /// <summary> Geminin sahibi olan oyuncu </summary>
    public PlayerId OwnerPlayerId => ownerPlayerId;

    /// <summary> Mevcut can puanı </summary>
    public int CurrentHP => currentHP;

    /// <summary> Maksimum can puanı (base + seviye bonusu) </summary>
    public int MaxHP => shipData != null ? shipData.BaseHP + GetLevelBonus(shipData.BaseHP) : 100;

    /// <summary> Mevcut saldırı gücü (base + seviye bonusu) </summary>
    public int AttackPower => shipData != null ? shipData.BaseAttack + GetLevelBonus(shipData.BaseAttack) : 25;

    /// <summary> Saldırı menzili </summary>
    public int AttackRange => shipData != null ? shipData.AttackRange : 1;

    /// <summary> Hareket puanı (MovementSystem ile uyumluluk için) </summary>
    public int MovementPoints => shipData != null ? shipData.MovementPoints + (level - 1) : 15;

    /// <summary> Mevcut XP </summary>
    public int CurrentXP => currentXP;

    /// <summary> Mevcut seviye </summary>
    public int Level => level;

    /// <summary> Sonraki seviye için gereken XP </summary>
    public int XPForNextLevel => level * level * 100;

    /// <summary> Mevcut aksiyon durumu </summary>
    public ShipActionState ActionState => actionState;

    /// <summary> Gemi hayatta mı? </summary>
    public bool IsAlive => currentHP > 0;

    /// <summary> Bu tur için hâlâ aksiyon yapabilir mi? </summary>
    public bool CanAct => actionState != ShipActionState.Done && IsAlive;

    /// <summary> Hareket edebilir mi? </summary>
    public bool CanMove => (actionState == ShipActionState.Idle || actionState == ShipActionState.Attacked) && IsAlive;

    /// <summary> Saldırabilir mi? </summary>
    public bool CanAttack => (actionState == ShipActionState.Idle || actionState == ShipActionState.Moved) && IsAlive;

    // --- Animasyon süreleri ---
    private float MovementDuration => movementDurationOverride > 0 ? movementDurationOverride
        : (shipData != null ? shipData.MovementDuration : 1f);

    private float RotationDuration => rotationDurationOverride > 0 ? rotationDurationOverride
        : (shipData != null ? shipData.RotationDuration : 0.3f);

    // --- Unity Lifecycle ---

    private void Awake()
    {
        glowHighlight = GetComponent<GlowHighlight>();

        // HP'yi başlat (ShipData yoksa fallback değerler kullanılır: MaxHP = 100)
        if (currentHP <= 0)
        {
            currentHP = MaxHP;
        }
    }

    private void Start()
    {
        // GameManager'a kendini kaydettir
        RegisterWithPlayer();
    }

    private void OnDestroy()
    {
        // GameManager'dan kendini kaldır
        UnregisterFromPlayer();
    }

    // --- Sahiplik ---

    /// <summary> Gemi sahipliğini ayarlar (spawn sırasında çağrılır) </summary>
    public void SetOwner(PlayerId owner)
    {
        if (ownerPlayerId == owner) return;

        UnregisterFromPlayer();
        ownerPlayerId = owner;
        RegisterWithPlayer();
    }

    private void RegisterWithPlayer()
    {
        if (GameManager.Instance != null && ownerPlayerId != PlayerId.None)
        {
            PlayerData pd = GameManager.Instance.GetPlayerData(ownerPlayerId);
            if (pd != null)
            {
                pd.AddShip(this);
                Debug.Log($"[Ship] {name} {ownerPlayerId} filosuna kaydedildi.");
            }
        }
    }

    private void UnregisterFromPlayer()
    {
        if (GameManager.Instance != null && ownerPlayerId != PlayerId.None)
        {
            PlayerData pd = GameManager.Instance.GetPlayerData(ownerPlayerId);
            if (pd != null)
            {
                pd.RemoveShip(this);
                Debug.Log($"[Ship] {name} {ownerPlayerId} filosundan kaldırıldı.");
            }
        }
    }

    /// <summary> Verilen oyuncunun bu geminin sahibi olup olmadığını kontrol eder </summary>
    public bool IsOwnedBy(PlayerId playerId)
    {
        return ownerPlayerId == playerId;
    }

    // --- Seçim ---

    public void Select()
    {
        if (glowHighlight != null)
            glowHighlight.ToggleGlow();
    }

    public void Deselect()
    {
        if (glowHighlight != null)
            glowHighlight.ToggleGlow(false);
    }

    // --- Hareket ---

    /// <summary> Gemiyi verilen yol üzerinden hareket ettirir </summary>
    public bool MoveThroughPath(List<Vector3> currentPath)
    {
        if (!CanMove)
        {
            Debug.LogWarning($"[Ship] {name} hareket edemez! Durum: {actionState}");
            return false;
        }

        pathPositions = new Queue<Vector3>(currentPath);
        Vector3 firstTarget = pathPositions.Dequeue();
        StartCoroutine(RotationCoroutine(firstTarget, RotationDuration));
        return true;
    }

    private IEnumerator RotationCoroutine(Vector3 endPosition, float duration)
    {
        Quaternion startRotation = transform.rotation;
        endPosition.y = transform.position.y;
        Vector3 direction = endPosition - transform.position;
        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (Mathf.Approximately(Mathf.Abs(Quaternion.Dot(startRotation, endRotation)), 1.0f) == false)
        {
            float timeElapsed = 0;
            while (timeElapsed < duration)
            {
                timeElapsed += Time.deltaTime;
                float lerpStep = timeElapsed / duration;
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);
                yield return null;
            }
            transform.rotation = endRotation;
        }
        StartCoroutine(MovementCoroutine(endPosition));
    }

    private IEnumerator MovementCoroutine(Vector3 endPosition)
    {
        Vector3 startPosition = transform.position;
        endPosition.y = startPosition.y;
        float timeElapsed = 0;

        while (timeElapsed < MovementDuration)
        {
            timeElapsed += Time.deltaTime;
            float lerpStep = timeElapsed / MovementDuration;
            transform.position = Vector3.Lerp(startPosition, endPosition, lerpStep);
            yield return null;
        }

        transform.position = endPosition;

        if (pathPositions.Count > 0)
        {
            StartCoroutine(RotationCoroutine(pathPositions.Dequeue(), RotationDuration));
        }
        else
        {
            // Hareket tamamlandı — aksiyon durumunu güncelle
            SetActionState(actionState == ShipActionState.Attacked
                ? ShipActionState.Done
                : ShipActionState.Moved);

            MovementFinished?.Invoke(this);
        }
    }

    // --- Savaş (Faz 2'de genişletilecek) ---

    /// <summary> Gemiye hasar verir </summary>
    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        int actualDamage = Mathf.Max(1, damage);
        currentHP = Mathf.Max(0, currentHP - actualDamage);

        Debug.Log($"[Ship] {name} hasar aldı: -{actualDamage} (Kalan HP: {currentHP}/{MaxHP})");
        OnDamageTaken?.Invoke(this, actualDamage);

        if (!IsAlive)
        {
            Die();
        }
    }

    /// <summary> Gemiyi iyileştirir (HP ekler) </summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;

        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    private void Die()
    {
        Debug.Log($"[Ship] {name} batırıldı!");
        OnDestroyed?.Invoke(this);
        // Not: Obje yok etme kararı GameManager tarafından verilir (kalıcı kayıp sistemi)
    }

    // --- Aksiyon Durumu ---

    /// <summary> Aksiyon durumunu değiştirir </summary>
    public void SetActionState(ShipActionState newState)
    {
        if (actionState == newState) return;

        actionState = newState;
        OnActionStateChanged?.Invoke(this, newState);
    }

    /// <summary> Tur başında aksiyon durumunu sıfırlar </summary>
    public void ResetActions()
    {
        SetActionState(ShipActionState.Idle);
    }

    // --- XP & Seviye ---

    /// <summary> Gemiye XP ekler. Yeterli XP'de seviye atlar. </summary>
    public void AddXP(int amount)
    {
        if (!IsAlive) return;

        currentXP += amount;
        Debug.Log($"[Ship] {name} +{amount} XP (Toplam: {currentXP})");

        // Seviye atlama kontrolü
        while (currentXP >= XPForNextLevel && level < 10)
        {
            currentXP -= XPForNextLevel;
            level++;
            currentHP = MaxHP; // Seviye atladığında full HP
            Debug.Log($"[Ship] {name} seviye atladı! Yeni seviye: {level}");
        }
    }

    /// <summary> Seviye bonusu hesaplar (%5 per level) </summary>
    private int GetLevelBonus(int baseValue)
    {
        return Mathf.RoundToInt(baseValue * (level - 1) * 0.05f);
    }

    // --- ShipData Ayarlama ---

    /// <summary> ShipData'yı runtime'da ayarlar ve HP'yi başlatır </summary>
    public void Initialize(ShipData data, PlayerId owner)
    {
        shipData = data;
        ownerPlayerId = owner;
        level = 1;
        currentXP = 0;
        currentHP = MaxHP;
        actionState = ShipActionState.Idle;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Geminin görsel katmanı.
/// Hareket animasyonu, dönüş, glow highlight gibi sadece
/// ekrana yansıyan işleri yönetir.
/// Mantık katmanından (NetworkShip) bağımsızdır — sadece veri okur ve görsel günceller.
/// 
/// NOT: Şu an geçiş aşamasında mevcut Ship.cs ile birlikte çalışır.
/// Fusion entegrasyonunda NetworkShip ile değiştirilecek.
/// </summary>
public class ShipView : MonoBehaviour
{
    [Header("Animasyon Ayarları")]
    [SerializeField] private float movementDuration = 1f;
    [SerializeField] private float rotationDuration = 0.3f;

    private GlowHighlight glowHighlight;
    private Queue<Vector3> pathPositions = new Queue<Vector3>();

    /// <summary> Hareket animasyonu tamamlandığında tetiklenir </summary>
    public event System.Action OnMovementComplete;

    /// <summary> Gemi şu an hareket ediyor mu? </summary>
    public bool IsMoving { get; private set; } = false;

    private void Awake()
    {
        glowHighlight = GetComponent<GlowHighlight>();
    }

    // --- Animasyon Süreleri ---

    /// <summary> Hareket süresi ayarla (ShipData'dan okunacak) </summary>
    public void SetAnimationDurations(float moveDuration, float rotateDuration)
    {
        movementDuration = moveDuration;
        rotationDuration = rotateDuration;
    }

    // --- Seçim Görseli ---

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

    // --- Hareket Animasyonu ---

    /// <summary>
    /// Verilen world-position listesi üzerinden gemiyi görsel olarak hareket ettirir.
    /// Bu metod sadece animasyon yapar — oyun mantığını değiştirmez.
    /// </summary>
    public void AnimateMovement(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
            return;

        if (IsMoving)
        {
            Debug.LogWarning($"[ShipView] {name} zaten hareket ediyor!");
            return;
        }

        IsMoving = true;
        pathPositions = new Queue<Vector3>(path);
        Vector3 firstTarget = pathPositions.Dequeue();
        StartCoroutine(RotationCoroutine(firstTarget, rotationDuration));
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

        while (timeElapsed < movementDuration)
        {
            timeElapsed += Time.deltaTime;
            float lerpStep = timeElapsed / movementDuration;
            transform.position = Vector3.Lerp(startPosition, endPosition, lerpStep);
            yield return null;
        }

        transform.position = endPosition;

        if (pathPositions.Count > 0)
        {
            StartCoroutine(RotationCoroutine(pathPositions.Dequeue(), rotationDuration));
        }
        else
        {
            // Hareket tamamlandı
            IsMoving = false;
            OnMovementComplete?.Invoke();
        }
    }
}

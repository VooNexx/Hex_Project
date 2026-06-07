using UnityEngine;

/// <summary>
/// Bir gemi tipinin temel (base) verilerini tutan ScriptableObject.
/// Her gemi tipi için bir tane oluşturulur (ör: Frigate_Data, Destroyer_Data).
/// Runtime'da değişmez — Ship instance'ı bu verileri referans olarak kullanır.
/// </summary>
[CreateAssetMenu(fileName = "New ShipData", menuName = "Hex Project/Ship Data")]
public class ShipData : ScriptableObject
{
    [Header("Kimlik")]
    [SerializeField] private string shipName = "Yeni Gemi";
    [SerializeField] private ShipType shipType = ShipType.Frigate;
    [TextArea(2, 4)]
    [SerializeField] private string description = "";

    [Header("Temel İstatistikler")]
    [SerializeField] private int baseHP = 100;
    [SerializeField] private int baseAttack = 25;
    [SerializeField] private int attackRange = 1;
    [SerializeField] private int movementPoints = 15;

    [Header("Animasyon Süreleri")]
    [SerializeField] private float movementDuration = 1f;
    [SerializeField] private float rotationDuration = 0.3f;

    [Header("Görseller")]
    [SerializeField] private Sprite shipIcon;
    [SerializeField] private GameObject shipPrefab;

    // --- Properties (Read-Only) ---

    /// <summary> Gemi adı </summary>
    public string ShipName => shipName;

    /// <summary> Gemi tipi </summary>
    public ShipType ShipType => shipType;

    /// <summary> Gemi açıklaması </summary>
    public string Description => description;

    /// <summary> Temel can puanı </summary>
    public int BaseHP => baseHP;

    /// <summary> Temel saldırı gücü </summary>
    public int BaseAttack => baseAttack;

    /// <summary> Saldırı menzili (hex cinsinden) </summary>
    public int AttackRange => attackRange;

    /// <summary> Hareket puanı (BFS maliyet bütçesi) </summary>
    public int MovementPoints => movementPoints;

    /// <summary> Hareket animasyonu süresi (saniye) </summary>
    public float MovementDuration => movementDuration;

    /// <summary> Dönüş animasyonu süresi (saniye) </summary>
    public float RotationDuration => rotationDuration;

    /// <summary> UI'da gösterilecek gemi ikonu </summary>
    public Sprite ShipIcon => shipIcon;

    /// <summary> Sahneye spawn edilecek prefab </summary>
    public GameObject ShipPrefab => shipPrefab;
}

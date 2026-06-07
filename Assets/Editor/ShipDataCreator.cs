using UnityEditor;
using UnityEngine;

/// <summary>
/// Başlangıç gemi tiplerini oluşturan editor yardımcı scripti.
/// Menü: Tools > Hex Project > Create Ship Data Assets
/// </summary>
public class ShipDataCreator : EditorWindow
{
    [MenuItem("Tools/Hex Project/Create Ship Data Assets")]
    public static void CreateAllShipData()
    {
        string path = "Assets/Data/Ships";

        // Klasörü kontrol et
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Ships");
        }

        CreateShipData(path, "Frigate_Data", "Firkateyn", ShipType.Frigate,
            "Hizli ve cevik kesif gemisi. Dusuk ates gucune sahip ama yuksek hareketliligiyle stratejik pozisyonlar alir.",
            80, 20, 2, 20, 0.8f, 0.25f);

        CreateShipData(path, "Destroyer_Data", "Muhrip", ShipType.Destroyer,
            "Dengeli savas gemisi. Hem saldiri hem savunmada guvenilir performans sunar.",
            120, 30, 2, 15, 1f, 0.3f);

        CreateShipData(path, "Cruiser_Data", "Kruvazoer", ShipType.Cruiser,
            "Agir ates gucune sahip savas gemisi. Uzun menzilli toplariyla dusmani mesafeden vurur.",
            160, 40, 3, 12, 1.2f, 0.4f);

        CreateShipData(path, "Battleship_Data", "Zirhli", ShipType.Battleship,
            "Filonun en dayanikli gemisi. Yavas ama devasa zirh ve ates gucuyle cephenin kalbinde durur.",
            250, 50, 3, 8, 1.5f, 0.5f);

        CreateShipData(path, "Submarine_Data", "Denizalti", ShipType.Submarine,
            "Golgelerde avlayan olumcul silah. Dusuk dayanikliligi yuksek hasar ve hizla telafi eder.",
            60, 45, 1, 18, 0.7f, 0.2f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ShipDataCreator] 5 gemi tipi basariyla olusturuldu! Assets/Data/Ships/ klasorune bakin.");
    }

    private static void CreateShipData(string folder, string fileName, string shipName, ShipType shipType,
        string description, int hp, int attack, int range, int movement, float moveDuration, float rotDuration)
    {
        string fullPath = $"{folder}/{fileName}.asset";
        ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(fullPath);
        bool isNew = false;

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ShipData>();
            isNew = true;
        }

        SerializedObject so = new SerializedObject(data);

        so.FindProperty("shipName").stringValue = shipName;
        so.FindProperty("shipType").enumValueIndex = (int)shipType;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("baseHP").intValue = hp;
        so.FindProperty("baseAttack").intValue = attack;
        so.FindProperty("attackRange").intValue = range;
        so.FindProperty("movementPoints").intValue = movement;
        so.FindProperty("movementDuration").floatValue = moveDuration;
        so.FindProperty("rotationDuration").floatValue = rotDuration;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ship_Base.prefab");
        if (prefab != null)
        {
            so.FindProperty("shipPrefab").objectReferenceValue = prefab;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        if (isNew)
        {
            AssetDatabase.CreateAsset(data, fullPath);
            Debug.Log($"[ShipDataCreator] {shipName} ({fileName}) olusturuldu.");
        }
        else
        {
            EditorUtility.SetDirty(data);
            Debug.Log($"[ShipDataCreator] {shipName} ({fileName}) guncellendi.");
        }
    }
}

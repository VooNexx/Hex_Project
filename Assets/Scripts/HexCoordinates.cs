using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCoordinates : MonoBehaviour
{
    public static float xOffset = 1.73f, yOffset = 1, zOffset = 1.5f;

    internal Vector3Int GetHexCoords()
    => offsetCoordinates;

    [Header("Offset coordinates")]
    [SerializeField]
    private Vector3Int offsetCoordinates;

    private void Awake()
    {
        offsetCoordinates = ConvertPositionToOffset(transform.position);
    }

    public static Vector3Int ConvertPositionToOffset(Vector3 position)
    {
        int z = Mathf.RoundToInt(position.z / zOffset);
    
        // Çift satırlar için x hesaplaması farklı
        float adjustedX = position.x;
        if (z % 2 == 0) // Çift satırlar kaydırılmış
        {
            // Çift satırlar için x koordinatını geri kaydır
            adjustedX = position.x - (xOffset * 0.5f);
        }
    
        int x = Mathf.CeilToInt(adjustedX / xOffset);
        int y = Mathf.RoundToInt(position.y / yOffset);
    
        return new Vector3Int(x, y, z);
    }

}

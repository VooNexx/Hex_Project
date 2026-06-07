using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hex grid yön vektörlerini tanımlar.
/// Offset koordinat sisteminde tek/çift satır farklılığını yönetir.
/// Saf veri yapısı — Unity bileşenlerine bağımlılığı yoktur.
/// </summary>
public static class Direction
{
    public static List<Vector3Int> directionsOffsetOdd = new List<Vector3Int>
    {
        new Vector3Int(-1, 0, 1), //N1
        new Vector3Int(0, 0, 1), //N2
        new Vector3Int(1, 0, 0), //E
        new Vector3Int(0, 0, -1), //S2
        new Vector3Int(-1, 0, -1), //S1
        new Vector3Int(-1, 0, 0), //W
    };

    public static List<Vector3Int> directionsOffsetEven = new List<Vector3Int>
    {
        new Vector3Int(0, 0, 1), //N1
        new Vector3Int(1, 0, 1), //N2
        new Vector3Int(1, 0, 0), //E
        new Vector3Int(1, 0, -1), //S2
        new Vector3Int(0, 0, -1), //S1
        new Vector3Int(-1, 0, 0), //W
    };

    public static List<Vector3Int> GetDirectionList(int z)
       => z %  2 == 0 ? directionsOffsetEven : directionsOffsetOdd;
}

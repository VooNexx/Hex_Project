using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCoordinates : MonoBehaviour
{
    internal Vector3Int GetHexCoords()
    => offsetCoordinates;

    [Header("Offset coordinates")]
    [SerializeField]
    private Vector3Int offsetCoordinates;

    private void Awake()
    {
        offsetCoordinates = HexCoordinateHelper.ConvertPositionToOffset(transform.position);
    }

    /// <summary>
    /// Geriye uyumluluk için mevcut imza korunur.
    /// Gerçek hesaplama HexCoordinateHelper'da yapılır.
    /// </summary>
    public static Vector3Int ConvertPositionToOffset(Vector3 position)
    {
        return HexCoordinateHelper.ConvertPositionToOffset(position);
    }
}

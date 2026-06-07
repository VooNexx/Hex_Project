using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hareket menzili hesaplama ve yol gösterme sistemi.
/// BFS pathfinding kullanarak geminin gidebileceği hex'leri hesaplar.
/// 
/// NetworkShip ile çalışır — SelectionView tarafından kullanılır.
/// Bu script görsel bir bileşendir — sadece highlight gösterimi ve yol hesaplama yapar.
/// Gerçek hareket komutu NetworkShip.ServerMove() üzerinden verilir.
/// </summary>
public class MovementSystem : MonoBehaviour
{
    private BFSResult movementRange = new BFSResult();
    private List<Vector3Int> currentPath = new List<Vector3Int>();

    public void HideRange(HexGrid hexGrid)
    {
        foreach (Vector3Int hexPosition in movementRange.GetRangePositions())
        {
            hexGrid.GetTileAt(hexPosition).DisableHighlight();
        }
        movementRange = new BFSResult();
    }

    // ============================================================
    // SHOW RANGE — NetworkShip
    // ============================================================

    /// <summary> Seçili geminin hareket menzilini hesaplar ve gösterir </summary>
    public void ShowRange(NetworkShip selectedShip, HexGrid hexGrid)
    {
        CalculateRange(selectedShip, hexGrid);

        Vector3Int shipPos = hexGrid.GetClosestHex(selectedShip.transform.position);

        foreach (Vector3Int hexPosition in movementRange.GetRangePositions())
        {
            if (shipPos == hexPosition)
                continue;
            hexGrid.GetTileAt(hexPosition).EnableHighlight();
        }
    }

    /// <summary> Hareket menzilini hesaplar (BFS) </summary>
    public void CalculateRange(NetworkShip selectedShip, HexGrid hexGrid)
    {
        movementRange = GraphSearch.BFSGetRange(hexGrid, hexGrid.GetClosestHex(selectedShip.transform.position), selectedShip.MovementPoints);
    }

    // ============================================================
    // PATH & YARDIMCI METODLAR
    // ============================================================

    /// <summary> Seçili hex'e giden yolu vurgular </summary>
    public void ShowPath(Vector3Int selectedHexPosition, HexGrid hexGrid)
    {
        if (movementRange.GetRangePositions().Contains(selectedHexPosition))
        {
            foreach (Vector3Int hexPosition in currentPath)
            {
                hexGrid.GetTileAt(hexPosition).ResetHighlight();
            }
            currentPath = movementRange.GetPathTo(selectedHexPosition);

            foreach (Vector3Int hexPosition in currentPath)
            {
                hexGrid.GetTileAt(hexPosition).HighlightPath();
            }
        }
    }

    /// <summary> Mevcut path'i döndürür (NetworkShip hareket talebi için) </summary>
    public List<Vector3Int> GetCurrentPath()
    {
        return new List<Vector3Int>(currentPath);
    }

    /// <summary> Verilen hex menzil içinde mi? </summary>
    public bool IsHexInRange(Vector3Int hexPosition)
    {
        return movementRange.IsHexPositionInRange(hexPosition);
    }
}

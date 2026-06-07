using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hareket menzili hesaplama ve yol gösterme sistemi.
/// BFS pathfinding kullanarak geminin gidebileceği hex'leri hesaplar.
/// 
/// Hem eski Ship hem de yeni NetworkShip ile çalışır (geçiş dönemi uyumu).
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
    // SHOW RANGE — Eski Ship uyumu
    // ============================================================

    /// <summary> Seçili geminin hareket menzilini hesaplar ve gösterir (eski Ship) </summary>
    public void ShowRange(Ship selectedShip, HexGrid hexGrid)
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

    /// <summary> Hareket menzilini hesaplar (BFS) — eski Ship </summary>
    public void CalculateRange(Ship selectedShip, HexGrid hexGrid)
    {
        movementRange = GraphSearch.BFSGetRange(hexGrid, hexGrid.GetClosestHex(selectedShip.transform.position), selectedShip.MovementPoints);
    }

    // ============================================================
    // SHOW RANGE — Yeni NetworkShip uyumu
    // ============================================================

    /// <summary> Seçili geminin hareket menzilini hesaplar ve gösterir (NetworkShip) </summary>
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

    /// <summary> Hareket menzilini hesaplar (BFS) — NetworkShip </summary>
    public void CalculateRange(NetworkShip selectedShip, HexGrid hexGrid)
    {
        movementRange = GraphSearch.BFSGetRange(hexGrid, hexGrid.GetClosestHex(selectedShip.transform.position), selectedShip.MovementPoints);
    }

    // ============================================================
    // PATH & HAREKET — Her iki tip için ortak
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

    /// <summary> Gemiyi hesaplanan yol üzerinden hareket ettirir (eski Ship — geriye uyumluluk) </summary>
    public bool MoveShip(Ship selectedShip, HexGrid hexGrid)
    {
        Debug.Log("Moving Ship " + selectedShip.name);
        return selectedShip.MoveThroughPath(currentPath.Select(pos => hexGrid.GetTileAt(pos).transform.position).ToList());
    }

    /// <summary> Mevcut path'i döndürür (NetworkShip hareket talebi için) </summary>
    public List<Vector3Int> GetCurrentPath()
    {
        return new List<Vector3Int>(currentPath);
    }

    public bool IsHexInRange(Vector3Int hexPosition)
    {
        return movementRange.IsHexPositionInRange(hexPosition);
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// BFS araması sonucunu tutan yapı.
/// Ziyaret edilen node'lar ve onlara giden yolları içerir.
/// Saf veri yapısı — Unity bileşenlerine bağımlılığı yoktur.
/// </summary>
public struct BFSResult
{
    public Dictionary<Vector3Int, Vector3Int?> visitedNodesDict;

    public List<Vector3Int> GetPathTo(Vector3Int destination)
    {
        if (visitedNodesDict == null || visitedNodesDict.ContainsKey(destination) == false)
            return new List<Vector3Int>();
        return GraphSearch.GeneratePathBFS(destination, visitedNodesDict);
    }

    public bool IsHexPositionInRange(Vector3Int position)
    {
        if (visitedNodesDict == null)
            return false;
        return visitedNodesDict.ContainsKey(position);
    }

    public IEnumerable<Vector3Int> GetRangePositions()
    {
        if (visitedNodesDict == null)
            return Enumerable.Empty<Vector3Int>();
        return visitedNodesDict.Keys;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bir oyuncunun tüm verilerini tutan sınıf.
/// Oyuncu kimliği, filo rengi, sahaya sürülmüş gemiler ve hangar referansı içerir.
/// Serializable olduğu için Inspector'da görüntülenebilir.
/// </summary>
[Serializable]
public class PlayerData
{
    [Header("Oyuncu Bilgileri")]
    [SerializeField] private PlayerId playerId;
    [SerializeField] private string playerName;
    [SerializeField] private Color fleetColor = Color.white;

    [Header("Filo Durumu")]
    [SerializeField] private List<Ship> deployedShips = new List<Ship>();

    /// <summary> Oyuncunun benzersiz kimliği </summary>
    public PlayerId Id => playerId;

    /// <summary> Oyuncunun görünen adı </summary>
    public string PlayerName => playerName;

    /// <summary> Oyuncunun filo rengi (gemilerin vurgulama rengi) </summary>
    public Color FleetColor => fleetColor;

    /// <summary> Sahada aktif olan gemiler </summary>
    public List<Ship> DeployedShips => deployedShips;

    /// <summary> Sahada hayatta kalan gemi sayısı </summary>
    public int AliveShipCount
    {
        get
        {
            int count = 0;
            foreach (var ship in deployedShips)
            {
                if (ship != null && ship.IsAlive)
                    count++;
            }
            return count;
        }
    }

    public PlayerData(PlayerId id, string name, Color color)
    {
        playerId = id;
        playerName = name;
        fleetColor = color;
    }

    /// <summary> Sahaya bir gemi ekler </summary>
    public void AddShip(Ship ship)
    {
        if (!deployedShips.Contains(ship))
            deployedShips.Add(ship);
    }

    /// <summary> Sahadan bir gemiyi kaldırır (batırıldığında) </summary>
    public void RemoveShip(Ship ship)
    {
        deployedShips.Remove(ship);
    }

    /// <summary> Tüm gemileri temizler </summary>
    public void ClearShips()
    {
        deployedShips.Clear();
    }

    /// <summary> Bu oyuncunun sahada gemisi kaldı mı? </summary>
    public bool HasShipsRemaining()
    {
        return AliveShipCount > 0;
    }

    /// <summary> Tüm gemilerin aksiyon durumunu sıfırlar (tur başı) </summary>
    public void ResetAllShipActions()
    {
        foreach (var ship in deployedShips)
        {
            if (ship != null && ship.IsAlive)
                ship.ResetActions();
        }
    }

    /// <summary> Tüm gemiler aksiyonlarını tamamladı mı? </summary>
    public bool AllShipsDone()
    {
        foreach (var ship in deployedShips)
        {
            if (ship != null && ship.IsAlive && ship.CanAct)
                return false;
        }
        return true;
    }
}

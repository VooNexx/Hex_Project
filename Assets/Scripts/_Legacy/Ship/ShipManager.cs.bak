using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gemi seçimi ve hareket komutlarını yöneten sistem.
/// UnitManager'ın yerini alır — Ship referansı kullanır.
/// </summary>
public class ShipManager : MonoBehaviour
{
    [SerializeField]
    private HexGrid hexGrid;

    [SerializeField]
    private MovementSystem movementSystem;

    [Header("Oyuncu Sahipliği")]
    [Tooltip("Bu ShipManager hangi oyuncuya ait?")]
    [SerializeField]
    private PlayerId ownerPlayerId = PlayerId.Player1;

    [SerializeField]
    private Ship selectedShip;
    private Hex previouslySelectedHex;

    /// <summary> Bu ShipManager'ın ait olduğu oyuncu </summary>
    public PlayerId OwnerPlayerId => ownerPlayerId;

    /// <summary> Şu an seçili gemi </summary>
    public Ship SelectedShip => selectedShip;

    /// <summary>
    /// Sıranın bu oyuncuda olup olmadığını GameManager üzerinden kontrol eder.
    /// </summary>
    private bool IsMyTurn
    {
        get
        {
            if (GameManager.Instance == null || GameManager.Instance.TurnSystem == null)
                return true;
            return GameManager.Instance.TurnSystem.IsPlayerTurn(ownerPlayerId);
        }
    }

    public void HandleShipSelected(GameObject shipGO)
    {
        if (IsMyTurn == false)
            return;

        Ship shipReference = shipGO.GetComponent<Ship>();

        // Sadece kendi gemilerini seçebilir
        if (shipReference == null || !shipReference.IsOwnedBy(ownerPlayerId))
            return;

        // Aksiyonu tükenmiş gemiyi seçme
        if (!shipReference.CanAct)
        {
            Debug.Log($"[ShipManager] {shipReference.name} bu turda aksiyonlarını tamamladı.");
            return;
        }

        if (CheckIfTheSameShipSelected(shipReference))
            return;

        PrepareShipForMovement(shipReference);
    }

    private bool CheckIfTheSameShipSelected(Ship shipReference)
    {
        if (this.selectedShip == shipReference)
        {
            ClearOldSelection();
            return true;
        }
        return false;
    }

    public void HandleTerrainSelected(GameObject hexGO)
    {
        if (selectedShip == null || IsMyTurn == false)
        {
            return;
        }

        Hex selectedHex = hexGO.GetComponent<Hex>();

        if (HandleHexOutOfRange(selectedHex.HexCoords) || HandleSelectedHexIsShipHex(selectedHex.HexCoords))
            return;

        HandleTargetHexSelected(selectedHex);
    }

    private void PrepareShipForMovement(Ship shipReference)
    {
        if (this.selectedShip != null)
        {
            ClearOldSelection();
        }

        this.selectedShip = shipReference;
        this.selectedShip.Select();

        // Sadece gemi hareket edebiliyorsa hareket menzilini göster
        if (this.selectedShip.CanMove)
        {
            movementSystem.ShowRange(this.selectedShip, this.hexGrid);
        }
        else
        {
            Debug.Log($"[ShipManager] {this.selectedShip.name} hareket edemez (Durum: {this.selectedShip.ActionState}).");
        }
    }

    private void ClearOldSelection()
    {
        previouslySelectedHex = null;
        if (this.selectedShip != null)
        {
            this.selectedShip.Deselect();
        }
        movementSystem.HideRange(this.hexGrid);
        this.selectedShip = null;
    }

    private void HandleTargetHexSelected(Hex selectedHex)
    {
        if (previouslySelectedHex == null || previouslySelectedHex != selectedHex)
        {
            previouslySelectedHex = selectedHex;
            movementSystem.ShowPath(selectedHex.HexCoords, this.hexGrid);
        }
        else
        {
            // Gemiyi hareket ettir
            movementSystem.MoveShip(selectedShip, this.hexGrid);
            ClearOldSelection();
        }
    }

    private bool HandleSelectedHexIsShipHex(Vector3Int hexPosition)
    {
        if (hexPosition == hexGrid.GetClosestHex(selectedShip.transform.position))
        {
            selectedShip.Deselect();
            ClearOldSelection();
            return true;
        }
        return false;
    }

    private bool HandleHexOutOfRange(Vector3Int hexPosition)
    {
        if (movementSystem.IsHexInRange(hexPosition) == false)
        {
            Debug.Log("Hex Out Of Range!");
            return true;
        }
        return false;
    }
}

using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// Oyuncu girişini işler ve ağ üzerinden komut gönderir.
/// Mevcut SelectionManager + PlayerInput + ShipManager mantığının birleşimi.
/// 
/// Bu script sadece LOKAl client'ta çalışır:
/// - Fare girişi algılar (PlayerInput)
/// - Raycast ile hex/gemi seçimi yapar (SelectionManager)
/// - Seçim/hareket highlight'larını gösterir (ShipManager)
/// - Hareket onayında NetworkShip'e RPC gönderir
/// 
/// Her client kendi SelectionView'ına sahiptir.
/// </summary>
public class SelectionView : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HexGrid hexGrid;

    [Header("Seçim Ayarları")]
    public LayerMask selectionMask;

    [Header("Hareket Sistemi")]
    [SerializeField] private MovementSystem movementSystem;

    // --- Seçim Durumu ---
    private NetworkShip _selectedShip;
    private Hex _previouslySelectedHex;

    /// <summary> Şu an seçili gemi </summary>
    public NetworkShip SelectedShip => _selectedShip;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        // Referansları otomatik bul
        if (hexGrid == null)
            hexGrid = FindAnyObjectByType<HexGrid>();

        if (movementSystem == null)
            movementSystem = FindAnyObjectByType<MovementSystem>();
    }

    private void Update()
    {
        // Fare tıklaması algıla
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick(Input.mousePosition);
        }
    }

    // ============================================================
    // TIKLAMA İŞLEME
    // ============================================================

    private void HandleClick(Vector3 mousePosition)
    {
        // NetworkGameManager var mı?
        if (NetworkGameManager.Instance == null) return;

        // Benim sıram mı? (Lokal runner üzerinden kontrol)
        var runner = NetworkGameManager.Instance.Runner;
        if (runner == null) return;

        PlayerRef localPlayer = runner.LocalPlayer;
        if (!NetworkGameManager.Instance.IsPlayerTurn(localPlayer))
            return;

        // Raycast yap
        GameObject hitObject;
        if (!FindTarget(mousePosition, out hitObject))
            return;

        // Ship mi Terrain mi?
        NetworkShip shipComponent = hitObject.GetComponent<NetworkShip>();
        if (shipComponent != null)
        {
            HandleShipSelected(shipComponent, localPlayer);
        }
        else
        {
            HandleTerrainSelected(hitObject, localPlayer);
        }
    }

    // ============================================================
    // GEMİ SEÇİMİ
    // ============================================================

    private void HandleShipSelected(NetworkShip ship, PlayerRef localPlayer)
    {
        // Sadece kendi gemilerimi seçebilirim
        if (!ship.IsOwnedBy(localPlayer))
            return;

        // Aksiyonu tükenmiş gemiyi seçme
        if (!ship.CanAct)
        {
            Debug.Log($"[SelectionView] {ship.name} bu turda aksiyonlarını tamamladı.");
            return;
        }

        // Aynı gemiyi tekrar seçtiyse — deselect
        if (_selectedShip == ship)
        {
            ClearSelection();
            return;
        }

        // Yeni gemi seç
        PrepareShipForMovement(ship);
    }

    // ============================================================
    // ARAZİ (HEX) SEÇİMİ
    // ============================================================

    private void HandleTerrainSelected(GameObject hexGO, PlayerRef localPlayer)
    {
        if (_selectedShip == null) return;

        Hex selectedHex = hexGO.GetComponent<Hex>();
        if (selectedHex == null) return;

        Vector3Int hexCoords = selectedHex.HexCoords;

        // Menzil dışı mı?
        if (!movementSystem.IsHexInRange(hexCoords))
        {
            Debug.Log("[SelectionView] Hex menzil dışında!");
            return;
        }

        // Geminin bulunduğu hex mi?
        if (hexCoords == hexGrid.GetClosestHex(_selectedShip.transform.position))
        {
            ClearSelection();
            return;
        }

        // İlk tıklama: Yolu göster | İkinci tıklama (aynı hex): Hareket et
        if (_previouslySelectedHex == null || _previouslySelectedHex != selectedHex)
        {
            _previouslySelectedHex = selectedHex;
            movementSystem.ShowPath(hexCoords, hexGrid);
        }
        else
        {
            // İkinci tıklama — hareket RPC gönder
            RequestMoveShip(hexCoords);
        }
    }

    // ============================================================
    // HAREKET TALEBİ
    // ============================================================

    /// <summary>
    /// Seçili gemiyi hedef hex'e taşıma talebi gönderir.
    /// Client → Host RPC üzerinden.
    /// </summary>
    private void RequestMoveShip(Vector3Int targetHex)
    {
        if (_selectedShip == null) return;

        // RPC ile Host'a hareket talebi gönder
        _selectedShip.RPC_RequestMove(targetHex);

        // Lokal olarak seçimi temizle (sonuç Host'tan gelecek)
        ClearSelection();
    }

    // ============================================================
    // SEÇİM YÖNETİMİ
    // ============================================================

    private void PrepareShipForMovement(NetworkShip ship)
    {
        // Eski seçimi temizle
        if (_selectedShip != null)
        {
            ClearSelection();
        }

        _selectedShip = ship;

        // Görsel glow
        var shipView = ship.GetComponent<ShipView>();
        if (shipView != null)
            shipView.Select();

        // Hareket menzilini göster (lokal hesaplama — NetworkShip overload)
        if (ship.CanMove && movementSystem != null)
        {
            movementSystem.ShowRange(ship, hexGrid);
        }
    }

    private void ClearSelection()
    {
        _previouslySelectedHex = null;

        if (_selectedShip != null)
        {
            var shipView = _selectedShip.GetComponent<ShipView>();
            if (shipView != null)
                shipView.Deselect();

            if (movementSystem != null)
                movementSystem.HideRange(hexGrid);

            _selectedShip = null;
        }
    }

    // ============================================================
    // RAYCAST
    // ============================================================

    private bool FindTarget(Vector3 mousePosition, out GameObject result)
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectionMask))
        {
            result = hit.collider.gameObject;
            return true;
        }

        result = null;
        return false;
    }
}

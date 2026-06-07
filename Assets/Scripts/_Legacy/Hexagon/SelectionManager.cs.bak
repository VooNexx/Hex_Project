using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fare tıklamalarıyla raycast yaparak hedef nesneyi belirler.
/// Ship mi yoksa Terrain mi tıklandığını tespit eder ve ShipManager'a iletir.
/// ShipManager'ı otomatik olarak bulur — Inspector'da event bağlantısına gerek yok.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [SerializeField]
    private Camera mainCamera;
    public LayerMask selectionMask;

    private ShipManager shipManager;

    // --- C# Events (UnityEvent yerine — Inspector bağımlılığı yok) ---

    /// <summary> Bir gemi seçildiğinde tetiklenir </summary>
    public event Action<GameObject> OnShipSelected;

    /// <summary> Arazi (hex) seçildiğinde tetiklenir </summary>
    public event Action<GameObject> OnTerrainSelected;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        // ShipManager'ı otomatik bul ve event'lere bağla
        shipManager = FindAnyObjectByType<ShipManager>();

        if (shipManager != null)
        {
            OnShipSelected += (go) => shipManager.HandleShipSelected(go);
            OnTerrainSelected += (go) => shipManager.HandleTerrainSelected(go);
        }
        else
        {
            Debug.LogWarning("[SelectionManager] ShipManager bulunamadı! Gemi seçimi çalışmayacak.");
        }
    }

    public void HandleClick(Vector3 mousePosition)
    {
        GameObject result;
        if (FindTarget(mousePosition, out result))
        {
            if (IsShipSelected(result))
            {
                OnShipSelected?.Invoke(result);
            }
            else
            {
                OnTerrainSelected?.Invoke(result);
            }
        }
    }

    private bool IsShipSelected(GameObject result)
    {
        return result.GetComponent<Ship>() != null;
    }

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fare girişini algılar ve SelectionManager'a iletir.
/// SelectionManager'ı otomatik olarak bulur — Inspector'da bağlantıya gerek yok.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    private SelectionManager selectionManager;

    private void Start()
    {
        selectionManager = FindAnyObjectByType<SelectionManager>();

        if (selectionManager == null)
            Debug.LogError("[PlayerInput] SelectionManager bulunamadı!");
    }

    void Update()
    {
        DetectMouseClick();
    }

    private void DetectMouseClick()
    {
        if (Input.GetMouseButtonDown(0) && selectionManager != null)
        {
            Vector3 mousePos = Input.mousePosition;
            selectionManager.HandleClick(mousePos);
        }
    }
}

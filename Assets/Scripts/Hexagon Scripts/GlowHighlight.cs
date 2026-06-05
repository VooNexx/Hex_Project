using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowHighlight : MonoBehaviour
{
    Dictionary<Renderer, Material[]> glowMaterialDictionary = new Dictionary<Renderer, Material[]>();
    Dictionary<Renderer, Material[]> originalMaterialDictionary = new Dictionary<Renderer, Material[]>();
    Dictionary<Color, Material> cachedGlowMaterials = new Dictionary<Color, Material>();

    public Material glowMaterial;

    private bool isGlowing = false;

    private Color validSpaceColor = Color.green;
    private Color originalGlowColor;

    private void Awake()
    {
        PrepareMaterialDictionaries();
        originalGlowColor = glowMaterial.GetColor("_BaseColor");
    }
    private void PrepareMaterialDictionaries()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            Material[] originalMaterials = renderer.materials;
            originalMaterialDictionary.Add(renderer, originalMaterials);

            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                Material mat = null;
                if (cachedGlowMaterials.TryGetValue(originalMaterials[i].color, out mat) == false)
                {
                    mat = new Material(glowMaterial);
                    mat.SetColor("_Color", originalMaterials[i].color);
                    cachedGlowMaterials.Add(originalMaterials[i].color, mat);
                }
                newMaterials[i] = mat;
            }
            glowMaterialDictionary.Add(renderer, newMaterials);
        }
    }

    internal void HighlightValidPath()
    {
        if (isGlowing == false)
            return;
        foreach (Renderer renderer in glowMaterialDictionary.Keys)
        {
            foreach (Material item in glowMaterialDictionary[renderer])
            {
                item.SetColor("_BaseColor", validSpaceColor);
            }
        }
    }

    internal void ResetGlowHighlight()
    {
        foreach (Renderer renderer in glowMaterialDictionary.Keys)
        {
            foreach (Material item in glowMaterialDictionary[renderer])
            {
                item.SetColor("_BaseColor", originalGlowColor);
            }
        }
    }

    public void ToggleGlow()
    {
        if (isGlowing == false)
        {
            ResetGlowHighlight();
            foreach (Renderer renderer in originalMaterialDictionary.Keys)
            {
                renderer.materials = glowMaterialDictionary[renderer];
            }
        }

        else
        {
            foreach (Renderer renderer in originalMaterialDictionary.Keys)
            {
                renderer.materials = originalMaterialDictionary[renderer];
            }
        }
        isGlowing = !isGlowing;
    }

    public void ToggleGlow(bool state)
    {
        if (isGlowing == state)
            return;
        isGlowing = !state;
        ToggleGlow();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSkinManager : MonoBehaviour
{
    public static CardSkinManager Instance;

    [Header("Available Skins (assign in Inspector)")]
    public List<CardSkin> skins = new();

    private Dictionary<string, CardSkin> skinMap;

    [Header("Runtime")]
    [SerializeField] private CardSkin currentSkin;

    public CardSkin CurrentSkin => currentSkin;

    private void Awake()
    {
        // Singleton (local-only)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Init();
    }

    private void Init()
    {
        skinMap = new Dictionary<string, CardSkin>(StringComparer.OrdinalIgnoreCase);

        if (skins == null || skins.Count == 0)
        {
            Debug.LogError("[CardSkinManager] No skins assigned in Inspector!");
            return;
        }

        foreach (var skin in skins)
        {
            if (skin == null)
            {
                Debug.LogWarning("[CardSkinManager] Null CardSkin found, skipped.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(skin.skinName))
            {
                Debug.LogWarning($"[CardSkinManager] Skin with empty name skipped: {skin.name}");
                continue;
            }

            if (skinMap.ContainsKey(skin.skinName))
            {
                Debug.LogWarning($"[CardSkinManager] Duplicate skin name skipped: {skin.skinName}");
                continue;
            }

            skinMap.Add(skin.skinName, skin);
        }

        // Pick first valid skin as default
        if (skinMap.Count > 0)
        {
            currentSkin = skins.Find(s => s != null);
        }

        if (currentSkin == null)
        {
            Debug.LogError("[CardSkinManager] No valid skin could be set as default!");
        }
    }

    // --------------------------------------------------
    // API
    // --------------------------------------------------

    public bool ChangeSkin(string skinName)
    {
        if (string.IsNullOrWhiteSpace(skinName))
            return false;

        if (!skinMap.TryGetValue(skinName, out var skin))
        {
            Debug.LogWarning($"[CardSkinManager] Skin not found: {skinName}");
            return false;
        }

        currentSkin = skin;
        return true;
    }

    public Sprite GetCardSprite(int cardId)
    {
        if (currentSkin == null)
        {
            Debug.LogError("[CardSkinManager] Current skin is null!");
            return null;
        }

        if (cardId < 0 || cardId >= currentSkin.cardSprites.Count)
        {
            Debug.LogError($"[CardSkinManager] Invalid cardId: {cardId}");
            return null;
        }

        return currentSkin.cardSprites[cardId];
    }
}

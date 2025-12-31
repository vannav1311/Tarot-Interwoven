using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card/Card Skin")]
public class CardSkin : ScriptableObject
{
    public string skinName;

    [Tooltip("Index phải khớp với CardID")]
    public List<Sprite> cardSprites;
}

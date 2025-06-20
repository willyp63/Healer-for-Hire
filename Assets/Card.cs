using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
public class Card : ScriptableObject
{
    [Header("Card Properties")]
    public string cardName;
    public string cardDescription;
    public Sprite cardArt;

    public int resourceCost;
    public float castTime;
    public int healAmount;
    public StatusEffect statusEffectPrefab;
}

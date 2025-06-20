using System.Collections.Generic;
using UnityEngine;

public class DeckManager : Singleton<DeckManager>
{
    [SerializeField]
    private List<Card> activeCards = new();
    public List<Card> ActiveCards => activeCards;
}

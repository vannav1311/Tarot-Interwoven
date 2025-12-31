using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Database")]
public class CardDatabase : ScriptableObject
{
    public static CardDatabase Instance;
    public List<CardData> cards = new List<CardData>();

    private void OnEnable()
    {
        Instance = this;

        cards.Clear();

        List<Value> values = new List<Value> {
            Value.Ace, Value.Two, Value.Three, Value.Four,
            Value.Five, Value.Six, Value.Seven, Value.Eight,
            Value.Nine, Value.Ten, Value.Page, Value.Knight,
            Value.Queen, Value.King
        };

        List<Type> types = new List<Type> {
            Type.Swords, Type.Cups, Type.Wands, Type.Pentacles
        };

        foreach (var type in types)
        {
            foreach (var value in values)
            {
                cards.Add(new CardData {
                    name = $"{value} of {type}",
                    value = value,
                    type = type,
                });
            }
        }
    }

    public CardData GetCardById(int id)
    {
        if (id >= 0 && id < cards.Count)
            return cards[id];
        return null;
    }

    public int GetIdByCard(CardData card)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].value == card.value && cards[i].type == card.type)
                return i;
        }
        return -1;
    }

    
    public bool valueIsEqual(Value card1, Value card2)
    {
        // So sánh trực tiếp
        if (card1 == card2) {
            return true;
        }

        // Page tương đương 1 hoặc 2
        if ((card1 == Value.Page && (card2 == Value.Ace || card2 == Value.Two)) ||
            (card2 == Value.Page && (card1 == Value.Ace || card1 == Value.Two))) {
            return true;
        }

        // Knight tương đương 3 hoặc 4
        if ((card1 == Value.Knight && (card2 == Value.Three || card2 == Value.Four)) ||
            (card2 == Value.Knight && (card1 == Value.Three || card1 == Value.Four))) {
            return true;
        }

        // Queen tương đương 5, 6, hoặc 7
        if ((card1 == Value.Queen && (card2 == Value.Five || card2 == Value.Six || card2 == Value.Seven)) ||
            (card2 == Value.Queen && (card1 == Value.Five || card1 == Value.Six || card1 == Value.Seven))) {
            return true;
        }

        // King tương đương 8, 9, hoặc 10
        if ((card1 == Value.King && (card2 == Value.Eight || card2 == Value.Nine || card2 == Value.Ten)) ||
            (card2 == Value.King && (card1 == Value.Eight || card1 == Value.Nine || card1 == Value.Ten))) {
            return true;
        }

        return false;
    }

    private List<CardData> SortAndConvertHand(List<int> cardsID)
    {
        for (int i = 0; i < cardsID.Count - 1; i++)
        {
            for (int j = i + 1; j < cardsID.Count; j++)
            {
                if (GetCardById(cardsID[j]).value < GetCardById(cardsID[i]).value)
                {
                    int temp = cardsID[i];
                    cardsID[i] = cardsID[j];
                    cardsID[j] = temp;
                }
            }
        }

        List<CardData> handSort = new List<CardData>();

        for (int i = 0; i < cardsID.Count; i++)
        {
            handSort.Add(GetCardById(cardsID[i]));
        }

        return handSort;
    }

    //------------------
    //      CALL
    //------------------

    //4 ace
    public bool have4Aces(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1)
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 4)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        for (int i = 0; i < cardsOnHand.Count-3; i++)
        {
            if (gm.CardSet[cardsID[i]] == Set.NoneSet && cardsOnHand[i].value == Value.Ace)
            {
                for (int j = i + 1; j < cardsOnHand.Count-2; j++)
                {
                    if (gm.CardSet[cardsID[j]] == Set.NoneSet && cardsOnHand[j].value == Value.Ace)
                    {
                        for (int k = j + 1; k < cardsOnHand.Count-1; k++)
                        {
                            if (gm.CardSet[cardsID[k]] == Set.NoneSet && cardsOnHand[k].value == Value.Ace)
                            {
                                for (int l = k + 1; l < cardsOnHand.Count; l++)
                                {
                                    if (gm.CardSet[cardsID[l]] == Set.NoneSet && cardsOnHand[l].value == Value.Ace)
                                    {
                                        if (!seperateCheck)
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.League);
                                            gm.CardSet.Set(cardsID[j], Set.League);
                                            gm.CardSet.Set(cardsID[k], Set.League);
                                            gm.CardSet.Set(cardsID[l], Set.League);
                                        }
                                        /* Prevent must have card is not in the right set, we set all cards we just found, 
                                        and finding the same set again */
                                        else if (mustHaveCardID != -1)
                                        {
                                            CardData mhc = GetCardById(mustHaveCardID);

                                            if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                            mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.League);
                                                gm.CardSet.Set(cardsID[j], Set.League);
                                                gm.CardSet.Set(cardsID[k], Set.League);
                                                gm.CardSet.Set(cardsID[l], Set.League);
                                                bool haveTargetInAnotherSet = false;
                                                if (have4Aces(cardsID, true, mustHaveCardID))
                                                    haveTargetInAnotherSet = true;

                                                foreach (var card in cardsID)
                                                    gm.CardSet.Set(card, Set.NoneSet);

                                                if (haveTargetInAnotherSet)
                                                {
                                                    return true;
                                                }

                                                return false;
                                            }                                           
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    // 4 bài hình giống nhau
    public bool have1RoyalQuartet(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1)
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 4)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        for (int i = 0; i < cardsOnHand.Count-3; i++)
        {
            if (cardsOnHand[i].value > Value.Ten && gm.CardSet[cardsID[i]] == Set.NoneSet)
            {
                for (int j = i + 1; j < cardsOnHand.Count-2; j++)
                {
                    if (gm.CardSet[cardsID[j]] == Set.NoneSet && valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value))
                    {
                        for (int k = j + 1; k < cardsOnHand.Count-1; k++)
                        {
                            if (gm.CardSet[cardsID[k]] == Set.NoneSet && valueIsEqual(cardsOnHand[j].value, cardsOnHand[k].value))
                            {
                                for (int l = k + 1; l < cardsOnHand.Count; l++)
                                {
                                    if (gm.CardSet[cardsID[l]] == Set.NoneSet && valueIsEqual(cardsOnHand[k].value, cardsOnHand[l].value))
                                    {
                                        if (!seperateCheck)
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.League);
                                            gm.CardSet.Set(cardsID[j], Set.League);
                                            gm.CardSet.Set(cardsID[k], Set.League);
                                            gm.CardSet.Set(cardsID[l], Set.League);
                                        }
                                        else if (mustHaveCardID != -1)
                                        {
                                            CardData mhc = GetCardById(mustHaveCardID);

                                            if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                            mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.League);
                                                gm.CardSet.Set(cardsID[j], Set.League);
                                                gm.CardSet.Set(cardsID[k], Set.League);
                                                gm.CardSet.Set(cardsID[l], Set.League);
                                                bool haveTargetInAnotherSet = false;
                                                if (have1RoyalQuartet(cardsID, true, mustHaveCardID))
                                                    haveTargetInAnotherSet = true;

                                                foreach (var card in cardsID)
                                                    gm.CardSet.Set(card, Set.NoneSet);

                                                if (haveTargetInAnotherSet)
                                                {
                                                    return true;
                                                }
                                                    
                                                return false;
                                            }                                           
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    //4 bài hình liên tiếp cùng chất
    public bool have1RoyalStraight(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 4)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit =Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }

        for (int i = 0; i < cardsOnHand.Count-3; i++)
        {
            if (cardsOnHand[i].value > Value.Ten && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = i + 1; j < cardsOnHand.Count-2; j++)
                {
                    if (cardsOnHand[j].value == cardsOnHand[i].value + 1 && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && cardsOnHand[j].type == cardsOnHand[i].type)
                    {
                        for (int k = j + 1; k < cardsOnHand.Count-1; k++)
                        {
                            if (cardsOnHand[k].value == cardsOnHand[j].value + 1 && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && cardsOnHand[k].type == cardsOnHand[j].type)
                            {
                                for (int l = k + 1; l < cardsOnHand.Count; l++)
                                {
                                    if (cardsOnHand[l].value == cardsOnHand[k].value + 1  && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && cardsOnHand[l].type == cardsOnHand[k].type)
                                    {
                                        if (!seperateCheck)
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.RoyalStraight);
                                            gm.CardSet.Set(cardsID[j], Set.RoyalStraight);
                                            gm.CardSet.Set(cardsID[k], Set.RoyalStraight);
                                            gm.CardSet.Set(cardsID[l], Set.RoyalStraight);
                                        }
                                        else if (mustHaveCardID != -1)
                                        {
                                            CardData mhc = GetCardById(mustHaveCardID);

                                            if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                            mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.RoyalStraight);
                                                gm.CardSet.Set(cardsID[j], Set.RoyalStraight);
                                                gm.CardSet.Set(cardsID[k], Set.RoyalStraight);
                                                gm.CardSet.Set(cardsID[l], Set.RoyalStraight);
                                                bool haveTargetInAnotherSet = false;
                                                if (have1RoyalStraight(cardsID, true, mustHaveCardID))
                                                    haveTargetInAnotherSet = true;

                                                foreach (var card in cardsID)
                                                    gm.CardSet.Set(card, Set.NoneSet);

                                                if (haveTargetInAnotherSet)
                                                {
                                                    return true;
                                                }
                                                    
                                                return false;
                                            }                                           
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    //3 bài hình liên tiếp cùng chất
    public bool have1Straight(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 3)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit = Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }

        for (int i = 0; i < cardsOnHand.Count-2; i++)
        {
            if (cardsOnHand[i].value > Value.Ten && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = i + 1; j < cardsOnHand.Count-1; j++)
                {
                    if (cardsOnHand[j].value == cardsOnHand[i].value + 1 && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && cardsOnHand[j].type == cardsOnHand[i].type)
                    {
                        for (int k = j + 1; k < cardsOnHand.Count; k++)
                        {
                            if (cardsOnHand[k].value == cardsOnHand[j].value + 1  && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && cardsOnHand[k].type == cardsOnHand[j].type)
                            {
                                if (!seperateCheck)
                                {
                                    gm.CardSet.Set(cardsID[i], Set.Straight);
                                    gm.CardSet.Set(cardsID[j], Set.Straight);
                                    gm.CardSet.Set(cardsID[k], Set.Straight);
                                }
                                else if (mustHaveCardID != -1)
                                {
                                    CardData mhc = GetCardById(mustHaveCardID);

                                    if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                    mhc != cardsOnHand[k])
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Straight);
                                        gm.CardSet.Set(cardsID[j], Set.Straight);
                                        gm.CardSet.Set(cardsID[k], Set.Straight);
                                        bool haveTargetInAnotherSet = false;
                                        if (have1Straight(cardsID, true, mustHaveCardID))
                                            haveTargetInAnotherSet = true;

                                        foreach (var card in cardsID)
                                            gm.CardSet.Set(card, Set.NoneSet);

                                        if (haveTargetInAnotherSet)
                                        {
                                            return true;
                                        }
                                            
                                        return false;
                                    }                                           
                                }
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    
    //4 lá bài liên tiếp cùng chất
    public bool have1Line4(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 4)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit = Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }
        
        for (int i = 0; i < cardsOnHand.Count; i++)
        {
            if ((cardsOnHand[i].value == Value.Ace || cardsOnHand[i].value == Value.Page)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Two 
                            || (cardsOnHand[j].value == Value.Page
                                && cardsOnHand[i].value != Value.Page))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Three 
                                    || cardsOnHand[k].value == Value.Knight)
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Four 
                                            || (cardsOnHand[l].value == Value.Knight 
                                                && cardsOnHand[k].value != Value.Knight))
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Two || cardsOnHand[i].value == Value.Page)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Three || cardsOnHand[j].value == Value.Knight)
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Four 
                                    || (cardsOnHand[k].value == Value.Knight 
                                        && cardsOnHand[j].value != Value.Knight))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Five 
                                            || cardsOnHand[l].value == Value.Queen)
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Three || cardsOnHand[i].value == Value.Knight)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Four 
                            || (cardsOnHand[j].value == Value.Knight
                                && cardsOnHand[i].value != Value.Knight))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Five 
                                    || cardsOnHand[k].value == Value.Queen)
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Six 
                                            || (cardsOnHand[l].value == Value.Queen 
                                                && cardsOnHand[k].value != Value.Queen))
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Four || cardsOnHand[i].value == Value.Knight)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Five || cardsOnHand[j].value == Value.Queen)
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Six 
                                    || (cardsOnHand[k].value == Value.Queen
                                        && cardsOnHand[j].value != Value.Queen))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Seven 
                                            || (cardsOnHand[l].value == Value.Queen 
                                                && cardsOnHand[k].value != Value.Queen
                                                && cardsOnHand[j].value != Value.Queen))
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Five || cardsOnHand[i].value == Value.Queen)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Six 
                            || (cardsOnHand[j].value == Value.Queen
                                && cardsOnHand[i].value != Value.Queen))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Seven
                                    || (cardsOnHand[k].value == Value.Queen 
                                        && cardsOnHand[j].value != Value.Queen
                                        && cardsOnHand[i].value != Value.Queen))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Eight 
                                            || cardsOnHand[l].value == Value.King)
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Six || cardsOnHand[i].value == Value.Queen)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Seven 
                            || (cardsOnHand[j].value == Value.Queen 
                                && cardsOnHand[i].value != Value.Queen))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Eight
                                    || cardsOnHand[k].value == Value.King)
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Nine 
                                            || (cardsOnHand[l].value == Value.King 
                                                && cardsOnHand[k].value != Value.King))
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Seven || cardsOnHand[i].value == Value.Queen)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Eight || cardsOnHand[j].value == Value.King)
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Nine
                                    || (cardsOnHand[k].value == Value.King 
                                        && cardsOnHand[j].value != Value.King))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Ten 
                                            || (cardsOnHand[l].value == Value.King 
                                                && cardsOnHand[k].value != Value.King
                                                && cardsOnHand[j].value != Value.King))
                                        && cardsOnHand[l].type == cardsOnHand[k].type
                                        && gm.CardSet[cardsID[l]] == Set.NoneSet
                                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[l].type == typeSuit))
                                        {
                                            if (!seperateCheck)
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Line4);
                                                gm.CardSet.Set(cardsID[j], Set.Line4);
                                                gm.CardSet.Set(cardsID[k], Set.Line4);
                                                gm.CardSet.Set(cardsID[l], Set.Line4);
                                            }
                                            else if (mustHaveCardID != -1)
                                            {
                                                CardData mhc = GetCardById(mustHaveCardID);

                                                if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                                mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                                {
                                                    gm.CardSet.Set(cardsID[i], Set.Line4);
                                                    gm.CardSet.Set(cardsID[j], Set.Line4);
                                                    gm.CardSet.Set(cardsID[k], Set.Line4);
                                                    gm.CardSet.Set(cardsID[l], Set.Line4);
                                                    bool haveTargetInAnotherSet = false;
                                                    if (have1Line4(cardsID, true, mustHaveCardID))
                                                        haveTargetInAnotherSet = true;

                                                    foreach (var card in cardsID)
                                                        gm.CardSet.Set(card, Set.NoneSet);

                                                    if (haveTargetInAnotherSet)
                                                    {
                                                        return true;
                                                    }
                                                        
                                                    return false;
                                                }                                           
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                }
            }
        }

        return false;
    }
    
    
    //3 lá bài liên tiếp cùng chất
    public bool have1Line(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 3)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit = Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }
        
        for (int i = 0; i < cardsOnHand.Count; i++)
        {
            if ((cardsOnHand[i].value == Value.Ace || cardsOnHand[i].value == Value.Page)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Two 
                            || (cardsOnHand[j].value == Value.Page
                                && cardsOnHand[i].value != Value.Page))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Three 
                                    || cardsOnHand[k].value == Value.Knight)
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Two || cardsOnHand[i].value == Value.Page)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Three || cardsOnHand[j].value == Value.Knight)
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Four 
                                    || (cardsOnHand[k].value == Value.Knight 
                                        && cardsOnHand[j].value != Value.Knight))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Three || cardsOnHand[i].value == Value.Knight)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Four 
                            || (cardsOnHand[j].value == Value.Knight
                                && cardsOnHand[i].value != Value.Knight))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Five 
                                    || cardsOnHand[k].value == Value.Queen)
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Four || cardsOnHand[i].value == Value.Knight)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Five || cardsOnHand[j].value == Value.Queen)
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Six 
                                    || (cardsOnHand[k].value == Value.Queen
                                        && cardsOnHand[j].value != Value.Queen))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Five || cardsOnHand[i].value == Value.Queen)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Six 
                            || (cardsOnHand[j].value == Value.Queen
                                && cardsOnHand[i].value != Value.Queen))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Seven
                                    || (cardsOnHand[k].value == Value.Queen 
                                        && cardsOnHand[j].value != Value.Queen
                                        && cardsOnHand[i].value != Value.Queen))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Six || cardsOnHand[i].value == Value.Queen)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Seven 
                            || (cardsOnHand[j].value == Value.Queen 
                                && cardsOnHand[i].value != Value.Queen))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Eight
                                    || cardsOnHand[k].value == Value.King)
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Seven || cardsOnHand[i].value == Value.Queen)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Eight || cardsOnHand[j].value == Value.King)
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Nine
                                    || (cardsOnHand[k].value == Value.King 
                                        && cardsOnHand[j].value != Value.King))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
            if ((cardsOnHand[i].value == Value.Eight || cardsOnHand[i].value == Value.King)
            && gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Nine 
                            || (cardsOnHand[j].value == Value.King 
                                && cardsOnHand[i].value != Value.King))
                        && cardsOnHand[j].type == cardsOnHand[i].type
                        && gm.CardSet[cardsID[j]] == Set.NoneSet
                        && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[j].type == typeSuit))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Ten
                                    || (cardsOnHand[k].value == Value.King 
                                        && cardsOnHand[j].value != Value.King
                                        && cardsOnHand[i].value != Value.King))
                                && cardsOnHand[k].type == cardsOnHand[j].type
                                && gm.CardSet[cardsID[k]] == Set.NoneSet
                                && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[k].type == typeSuit))
                                {
                                    if (!seperateCheck)
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Line);
                                        gm.CardSet.Set(cardsID[j], Set.Line);
                                        gm.CardSet.Set(cardsID[k], Set.Line);
                                    }
                                    else if (mustHaveCardID != -1)
                                    {
                                        CardData mhc = GetCardById(mustHaveCardID);

                                        if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                        mhc != cardsOnHand[k])
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Line);
                                            gm.CardSet.Set(cardsID[j], Set.Line);
                                            gm.CardSet.Set(cardsID[k], Set.Line);
                                            bool haveTargetInAnotherSet = false;
                                            if (have1Line(cardsID, true, mustHaveCardID))
                                                haveTargetInAnotherSet = true;

                                            foreach (var card in cardsID)
                                                gm.CardSet.Set(card, Set.NoneSet);

                                            if (haveTargetInAnotherSet)
                                            {
                                                return true;
                                            }
                                                
                                            return false;
                                        }                                           
                                    }
                                    return true;
                                }
                            }
                        }
                }
            }
        }

        return false;
    }

    
    //4 lá bài giống nhau
    public bool have1Quartet(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 4)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit = Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }
        
        for (int i = 0; i < cardsOnHand.Count-2; i++)
        {
            if (gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = i + 1; j < cardsOnHand.Count-1; j++)
                {
                    if (gm.CardSet[cardsID[j]] == Set.NoneSet
                    && valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                    && (suit == "none" || (typeSuit == Type.NoneType && cardsOnHand[j].type == cardsOnHand[i].type) 
                        || (cardsOnHand[j].type == typeSuit)))
                    {
                        for (int k = j + 1; k < cardsOnHand.Count; k++)
                        {
                            if (gm.CardSet[cardsID[k]] == Set.NoneSet
                            && valueIsEqual(cardsOnHand[j].value, cardsOnHand[k].value)
                            && (suit == "none" || (typeSuit == Type.NoneType && cardsOnHand[k].type == cardsOnHand[j].type)
                                || cardsOnHand[k].type == typeSuit))
                            {
                                for (int l = k + 1; l < cardsOnHand.Count; l++)
                                {
                                    if (gm.CardSet[cardsID[l]] == Set.NoneSet
                                    && valueIsEqual(cardsOnHand[k].value, cardsOnHand[l].value)
                                    && (suit == "none" || (typeSuit == Type.NoneType && cardsOnHand[l].type == cardsOnHand[k].type)
                                        || cardsOnHand[l].type == typeSuit))
                                    {
                                        if (!seperateCheck)
                                        {
                                            gm.CardSet.Set(cardsID[i], Set.Quartet);
                                            gm.CardSet.Set(cardsID[j], Set.Quartet);
                                            gm.CardSet.Set(cardsID[k], Set.Quartet);
                                            gm.CardSet.Set(cardsID[l], Set.Quartet);
                                        }
                                        else if (mustHaveCardID != -1)
                                        {
                                            CardData mhc = GetCardById(mustHaveCardID);

                                            if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                            mhc != cardsOnHand[k] && mhc != cardsOnHand[l])
                                            {
                                                gm.CardSet.Set(cardsID[i], Set.Quartet);
                                                gm.CardSet.Set(cardsID[j], Set.Quartet);
                                                gm.CardSet.Set(cardsID[k], Set.Quartet);
                                                gm.CardSet.Set(cardsID[l], Set.Quartet);
                                                bool haveTargetInAnotherSet = false;
                                                if (have1Quartet(cardsID, true, mustHaveCardID))
                                                    haveTargetInAnotherSet = true;

                                                foreach (var card in cardsID)
                                                    gm.CardSet.Set(card, Set.NoneSet);

                                                if (haveTargetInAnotherSet)
                                                {
                                                    return true;
                                                }
                                                    
                                                return false;
                                            }                                           
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    
    //3 lá bài giống nhau
    public bool have1Triplet(List<int> cardsID, bool seperateCheck, int mustHaveCardID = -1, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 3)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit = Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }
        
        for (int i = 0; i < cardsOnHand.Count-2; i++)
        {
            if (gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = i + 1; j < cardsOnHand.Count-1; j++)
                {
                    if (gm.CardSet[cardsID[j]] == Set.NoneSet
                    && valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                    && (suit == "none" || (typeSuit == Type.NoneType && cardsOnHand[j].type == cardsOnHand[i].type) 
                        || (cardsOnHand[j].type == typeSuit)))
                    {
                        for (int k = j + 1; k < cardsOnHand.Count; k++)
                        {
                            if (gm.CardSet[cardsID[k]] == Set.NoneSet
                            && valueIsEqual(cardsOnHand[j].value, cardsOnHand[k].value)
                            && (suit == "none" || (typeSuit == Type.NoneType && cardsOnHand[k].type == cardsOnHand[j].type)
                                || cardsOnHand[k].type == typeSuit))
                            {
                                if (!seperateCheck)
                                {
                                    gm.CardSet.Set(cardsID[i], Set.Triplet);
                                    gm.CardSet.Set(cardsID[j], Set.Triplet);
                                    gm.CardSet.Set(cardsID[k], Set.Triplet);
                                }
                                else if (mustHaveCardID != -1)
                                {
                                    CardData mhc = GetCardById(mustHaveCardID);

                                    if (mhc != cardsOnHand[i] && mhc != cardsOnHand[j] && 
                                    mhc != cardsOnHand[k])
                                    {
                                        gm.CardSet.Set(cardsID[i], Set.Triplet);
                                        gm.CardSet.Set(cardsID[j], Set.Triplet);
                                        gm.CardSet.Set(cardsID[k], Set.Triplet);
                                        bool haveTargetInAnotherSet = false;
                                        if (have1Triplet(cardsID, true, mustHaveCardID))
                                            haveTargetInAnotherSet = true;

                                        foreach (var card in cardsID)
                                            gm.CardSet.Set(card, Set.NoneSet);

                                        if (haveTargetInAnotherSet)
                                        {
                                            return true;
                                        }
                                            
                                        return false;
                                    }                                           
                                }
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    
    //2 lá bài giống nhau
    public bool have1Duo(List<int> cardsID, bool seperateCheck, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 2)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        Type typeSuit = Type.NoneType;
        if (suit == "suit")
        {
            for (int i = 0; i < cardsOnHand.Count; i++)
            {
                if (gm.CardSet[cardsID[i]] != Set.NoneSet)
                {
                    typeSuit = cardsOnHand[i].type;
                    break;
                }
            }
        }

        for (int i = 0; i < cardsOnHand.Count-1; i++)
        {
            if (gm.CardSet[cardsID[i]] == Set.NoneSet
            && (suit == "none" || typeSuit == Type.NoneType || cardsOnHand[i].type == typeSuit))
            {
                for (int j = i + 1; j < cardsOnHand.Count; j++)
                {
                    if (gm.CardSet[cardsID[j]] == Set.NoneSet
                    && valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                    && (suit == "none" || (typeSuit == Type.NoneType && cardsOnHand[j].type == cardsOnHand[i].type) 
                        || (cardsOnHand[j].type == typeSuit)))
                    {
                        if (!seperateCheck)
                        {
                            gm.CardSet.Set(cardsID[i], Set.Duo);
                            gm.CardSet.Set(cardsID[j], Set.Duo);
                        }
                        return true;
                    }
                }
            }
        }

        return false;
    }


    //8 lá liên tiếp
    public bool haveGodSeries(List<int> cardsID, string suit = "none")
    {
        GameManager gm = GameManager.Instance;

        if (cardsID.Count < 8)
            return false;

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);
        
        for (int i = 0; i < cardsOnHand.Count; i++)
        {
            if (cardsOnHand[i].value == Value.Ace || cardsOnHand[i].value == Value.Page)
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Two 
                        || (cardsOnHand[j].value == Value.Page
                            && cardsOnHand[i].value != Value.Page))
                    && (suit == "none" || cardsOnHand[j].type == cardsOnHand[i].type))
                    {
                        for (int k = 0; k < cardsOnHand.Count; k++)
                        {
                            if ((cardsOnHand[k].value == Value.Three 
                                || cardsOnHand[k].value == Value.Knight)
                            && (suit == "none"|| cardsOnHand[k].type == cardsOnHand[i].type))
                            {
                                for (int l = 0; l < cardsOnHand.Count; l++)
                                {
                                    if ((cardsOnHand[l].value == Value.Four 
                                        || (cardsOnHand[l].value == Value.Knight 
                                            && cardsOnHand[k].value != Value.Knight))
                                    && (suit == "none" || cardsOnHand[l].type == cardsOnHand[i].type))
                                    {
                                        for (int m = 0; m < cardsOnHand.Count; m++)
                                        {
                                            if ((cardsOnHand[m].value == Value.Five || cardsOnHand[m].value == Value.Queen)
                                            && (suit == "none" || cardsOnHand[m].type == cardsOnHand[i].type))
                                            {
                                                for (int n = 0; n < cardsOnHand.Count; n++)
                                                {
                                                    if ((cardsOnHand[n].value == Value.Six 
                                                        || (cardsOnHand[n].value == Value.Queen
                                                            && cardsOnHand[m].value != Value.Queen))
                                                    && (suit == "none" || cardsOnHand[n].type == cardsOnHand[i].type))
                                                    {
                                                        for (int p = 0; p < cardsOnHand.Count; p++)
                                                        {
                                                            if (cardsOnHand[p].value == Value.Seven
                                                                || (cardsOnHand[p].value == Value.Queen
                                                                    && cardsOnHand[n].value != Value.Queen
                                                                    && cardsOnHand[m].value != Value.Queen)
                                                            && (suit == "none" || cardsOnHand[p].type == cardsOnHand[i].type))
                                                            {
                                                                for (int q = 0; q < cardsOnHand.Count; q++)
                                                                {
                                                                    if ((cardsOnHand[q].value == Value.Eight 
                                                                        || cardsOnHand[q].value == Value.King)
                                                                    && (suit == "none" || cardsOnHand[q].type == cardsOnHand[i].type))
                                                                    {
                                                                        return true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (cardsOnHand[i].value == Value.Two || cardsOnHand[i].value == Value.Page)
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Three || cardsOnHand[j].value == Value.Knight)
                    && (suit == "none" || cardsOnHand[j].type == cardsOnHand[i].type))
                    {
                        for (int k = 0; k < cardsOnHand.Count; k++)
                        {
                            if ((cardsOnHand[k].value == Value.Four 
                                || (cardsOnHand[k].value == Value.Knight 
                                    && cardsOnHand[j].value != Value.Knight))
                            && (suit == "none" || cardsOnHand[k].type == cardsOnHand[i].type))
                            {
                                for (int l = 0; l < cardsOnHand.Count; l++)
                                {
                                    if ((cardsOnHand[l].value == Value.Five 
                                        || cardsOnHand[l].value == Value.Queen)
                                    && (suit == "none" || cardsOnHand[l].type == cardsOnHand[i].type))
                                    {
                                        for (int m = 0; m < cardsOnHand.Count; m++)
                                        {
                                            if ((cardsOnHand[m].value == Value.Six || 
                                                (cardsOnHand[m].value == Value.Queen 
                                                    && cardsOnHand[l].value != Value.Queen))
                                            && (suit == "none" || cardsOnHand[m].type == cardsOnHand[i].type))
                                            {
                                                for (int n = 0; n < cardsOnHand.Count; n++)
                                                {
                                                    if ((cardsOnHand[n].value == Value.Seven 
                                                        || (cardsOnHand[n].value == Value.Queen 
                                                            && cardsOnHand[m].value != Value.Queen
                                                            && cardsOnHand[l].value != Value.Queen))
                                                    && (suit == "none" || cardsOnHand[n].type == cardsOnHand[i].type))
                                                    {
                                                        for (int p = 0; p < cardsOnHand.Count; p++)
                                                        {
                                                            if ((cardsOnHand[p].value == Value.Eight
                                                                || cardsOnHand[p].value == Value.King)
                                                            && (suit == "none" || cardsOnHand[p].type == cardsOnHand[i].type))
                                                            {
                                                                for (int q = 0; q < cardsOnHand.Count; q++)
                                                                {
                                                                    if ((cardsOnHand[q].value == Value.Nine 
                                                                        || (cardsOnHand[q].value == Value.King 
                                                                            && cardsOnHand[p].value != Value.King))
                                                                    && (suit == "none" || cardsOnHand[q].type == cardsOnHand[i].type))
                                                                    {
                                                                        return true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (cardsOnHand[i].value == Value.Three || cardsOnHand[i].value == Value.Knight)
            {
                for (int j = 0; j < cardsOnHand.Count; j++)
                {
                    if ((cardsOnHand[j].value == Value.Four 
                            || (cardsOnHand[j].value == Value.Knight
                                && cardsOnHand[i].value != Value.Knight))
                        && (suit == "none" || cardsOnHand[j].type == cardsOnHand[i].type))
                        {
                            for (int k = 0; k < cardsOnHand.Count; k++)
                            {
                                if ((cardsOnHand[k].value == Value.Five 
                                    || cardsOnHand[k].value == Value.Queen)
                                && (suit == "none" || cardsOnHand[k].type == cardsOnHand[i].type))
                                {
                                    for (int l = 0; l < cardsOnHand.Count; l++)
                                    {
                                        if ((cardsOnHand[l].value == Value.Six 
                                            || (cardsOnHand[l].value == Value.Queen 
                                                && cardsOnHand[k].value != Value.Queen))
                                        && (suit == "none" || cardsOnHand[l].type == cardsOnHand[i].type))
                                        {
                                            for (int m = 0; m < cardsOnHand.Count; m++)
                                            {
                                                if ((cardsOnHand[m].value == Value.Seven 
                                                    || (cardsOnHand[m].value == Value.Queen
                                                        && cardsOnHand[l].value != Value.Queen
                                                        && cardsOnHand[k].value != Value.Queen))
                                                && (suit == "none" || cardsOnHand[m].type == cardsOnHand[i].type))
                                                {
                                                    for (int n = 0; n < cardsOnHand.Count; n++)
                                                    {
                                                        if ((cardsOnHand[n].value == Value.Eight || cardsOnHand[n].value == Value.King)
                                                            && (suit == "none" || cardsOnHand[n].type == cardsOnHand[i].type))
                                                            {
                                                                for (int p = 0; p < cardsOnHand.Count; p++)
                                                                {
                                                                    if ((cardsOnHand[p].value == Value.Nine
                                                                        || (cardsOnHand[p].value == Value.King
                                                                                && cardsOnHand[n].value != Value.King))
                                                                    && (suit == "none" || cardsOnHand[p].type == cardsOnHand[i].type))
                                                                    {
                                                                        for (int q = 0; q < cardsOnHand.Count; q++)
                                                                        {
                                                                            if ((cardsOnHand[q].value == Value.Ten 
                                                                                || (cardsOnHand[q].value == Value.King 
                                                                                    && cardsOnHand[p].value != Value.King
                                                                                    && cardsOnHand[n].value != Value.King))
                                                                            && (suit == "none" || cardsOnHand[q].type == cardsOnHand[i].type))
                                                                            {
                                                                                return true;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                }
            }
        }

        return false;
    }

    //------------------
    //      SNAP
    //------------------

    //1 line, 1 triplet, 1 duo
    public bool SimpleSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;

        bool isSimpleSnap = false;

        if ((CallSet == Set.Line || have1Line(cardsID, false)) && 
        (CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
        (CallSet == Set.Duo || have1Duo(cardsID, false)))
        {
            isSimpleSnap = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Line || have1Line(cardsID, false)) && 
            (CallSet == Set.Duo || have1Duo(cardsID, false)) && 
            (CallSet == Set.Triplet || have1Triplet(cardsID, false)))
            {
                isSimpleSnap = true;
            }
            else
            {
                for (int i = 0; i < cardsID.Count; i++)
                {
                    gm.CardSet.Set(cardsID[i], Set.NoneSet);
                }

                if ((CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
                (CallSet == Set.Line || have1Line(cardsID, false)) && 
                (CallSet == Set.Duo || have1Duo(cardsID, false)))
                {
                    isSimpleSnap = true;
                }
                else
                {
                    for (int i = 0; i < cardsID.Count; i++)
                    {
                        gm.CardSet.Set(cardsID[i], Set.NoneSet);
                    }

                    if ((CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
                    (CallSet == Set.Duo || have1Duo(cardsID, false)) && 
                    (CallSet == Set.Line || have1Line(cardsID, false)))
                    {
                        isSimpleSnap = true;
                    }
                    else
                    {
                        for (int i = 0; i < cardsID.Count; i++)
                        {
                            gm.CardSet.Set(cardsID[i], Set.NoneSet);
                        }

                        if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
                        (CallSet == Set.Line || have1Line(cardsID, false)) && 
                        (CallSet == Set.Triplet || have1Triplet(cardsID, false)))
                        {
                            isSimpleSnap = true;
                        }
                        else
                        {
                            for (int i = 0; i < cardsID.Count; i++)
                            {
                                gm.CardSet.Set(cardsID[i], Set.NoneSet);
                            }

                            if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
                            (CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
                            (CallSet == Set.Line || have1Line(cardsID, false)))
                            {
                                isSimpleSnap = true;
                            }
                        }
                    }
                }
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isSimpleSnap)
        {
            return true;
        }

        return false;
    }

    //2 triplet, 1 duo
    public bool MatchPairSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;

        bool isMatchPair = false;

        if ((CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
        have1Triplet(cardsID, false) && 
        (CallSet == Set.Duo || have1Duo(cardsID, false)))
        {
            isMatchPair = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
            (CallSet == Set.Duo || have1Duo(cardsID, false)) && 
            have1Triplet(cardsID, false))
            {
                isMatchPair = true;
            }
            else
            {
                for (int i = 0; i < cardsID.Count; i++)
                {
                    gm.CardSet.Set(cardsID[i], Set.NoneSet);
                }

                if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
                (CallSet == Set.Triplet || have1Triplet(cardsID, false)) && 
                have1Triplet(cardsID, false))
                {
                    isMatchPair = true;
                }
            }
        }

        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isMatchPair)
        {
            return true;
        }

        return false;
    }
    
    //2 line, 1 duo
    public bool LinePairSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;

        bool isLinePair = false;

        if ((CallSet == Set.Line || have1Line(cardsID, false)) && 
        have1Line(cardsID, false) && 
        (CallSet == Set.Duo || have1Duo(cardsID, false)))
        {
            isLinePair = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Line || have1Line(cardsID, false)) && 
            (CallSet == Set.Duo || have1Duo(cardsID, false)) && 
            have1Line(cardsID, false))
            {
                isLinePair = true;
            }
            else
            {
                for (int i = 0; i < cardsID.Count; i++)
                {
                    gm.CardSet.Set(cardsID[i], Set.NoneSet);
                }

                if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
                (CallSet == Set.Line || have1Line(cardsID, false)) && 
                have1Line(cardsID, false))
                {
                    isLinePair = true;
                }
            }
        }

        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isLinePair)
        {
            return true;
        }

        return false;
    }
    
    //2 line và 1 duo tất cả cùng type
    public bool LineSuitSnap(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;

        bool isLinePair = false;

        Set CallSet = Set.NoneSet;

        List<int> allCards = new List<int>();
        foreach (int card in cardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }

        if (CalledCardsID.Count > 0)
        {
            // Check if callcard is in right combo
            if (gm.CardSet[CalledCardsID[0]] != Set.Line && gm.CardSet[CalledCardsID[0]] != Set.Duo)
                return false;

            // Check if callcard is suited
            Type callType = GetCardById(CalledCardsID[0]).type;
            foreach (int card in CalledCardsID)
            {
                if (GetCardById(card).type != callType)
                    return false;
            }

            foreach (int card in CalledCardsID)
            {
                int temp = card;
                allCards.Add(temp);
            }

            CallSet = gm.CardSet[CalledCardsID[0]];
        }

        if ((CallSet == Set.Line || have1Line(allCards, false, -1, "suit")) && 
        have1Line(allCards, false, -1, "suit") && 
        (CallSet == Set.Duo || have1Duo(allCards, false, "suit")))
        {
            isLinePair = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Line || have1Line(allCards, false, -1, "suit")) && 
            (CallSet == Set.Duo || have1Duo(allCards, false, "suit")) && 
            have1Line(allCards, false, -1, "suit"))
            {
                isLinePair = true;
            }
            else
            {
                for (int i = 0; i < cardsID.Count; i++)
                {
                    gm.CardSet.Set(cardsID[i], Set.NoneSet);
                }

                if ((CallSet == Set.Duo || have1Duo(allCards, false, "suit")) && 
                have1Line(allCards, false, -1, "suit") && 
                (CallSet == Set.Line || have1Line(allCards, false, -1, "suit")))
                {
                    isLinePair = true;
                }
            }
        }
        
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isLinePair)
        {
            return true;
        }

        return false;
    }

    //4 Duo
    public bool QuadDuoSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isQuadDuo = false;

        if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
        have1Duo(cardsID, false) && have1Duo(cardsID, false) && have1Duo(cardsID, false))
        {
            isQuadDuo = true;
        }

        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isQuadDuo)
        {
            return true;
        }

        return false;
    }

    //4 Duo cùng type
    public bool SuitedQuadDuoSnap(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;
        
        bool isSuitedQuadDuo = false;

        Set CallSet = Set.NoneSet;

        List<int> allCards = new List<int>();
        foreach (int card in cardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }

        if (CalledCardsID.Count > 0)
        {
            // Check if callcard is in right combo
            if (gm.CardSet[CalledCardsID[0]] != Set.Duo)
                return false;

            // Check if callcard is suited
            Type callType = GetCardById(CalledCardsID[0]).type;
            foreach (int card in CalledCardsID)
            {
                if (GetCardById(card).type != callType)
                    return false;
            }

            foreach (int card in CalledCardsID)
            {
                int temp = card;
                allCards.Add(temp);
            }

            CallSet = gm.CardSet[CalledCardsID[0]];
        }

        if ((CallSet == Set.Duo || have1Duo(allCards, false, "suit")) && 
        have1Duo(allCards, false, "suit") && have1Duo(allCards, false, "suit") && have1Duo(allCards, false, "suit"))
        {
            isSuitedQuadDuo = true;
        }
        
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isSuitedQuadDuo)
        {
            return true;
        }

        return false;
    }

    //4 duo với type duo khác nhau
    public bool FullHouseQuadDuo(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;
        
        bool stop = false;
        Type typeDuo1 = Type.NoneType;
        Type typeDuo2 = Type.NoneType;
        Type typeDuo3 = Type.NoneType;
        Type typeDuo4 = Type.NoneType;

        if (CalledCardsID.Count > 0)
        {
            // Check if callcard is in right combo
            if (gm.CardSet[CalledCardsID[0]] != Set.Duo)
                return false;

            // Check if callcard is suited
            Type callType = GetCardById(CalledCardsID[0]).type;
            foreach (int card in CalledCardsID)
            {
                if (GetCardById(card).type != callType)
                    return false;
            }

            /* Ở trường hợp không có callcard, sét cả 8 card trên tay và check bình thường, nhưng nếu có callcard, 
            card trên tay chỉ còn lại 6 lá và typeDuo1 sẽ được set luôn */
            typeDuo1 = callType;
        }

        List<CardData> cardsOnHand = SortAndConvertHand(cardsID);

        // Chỉ chạy khi không có callcard nào
        if (CalledCardsID.Count == 0)
        {
            for (int i = 0; i < cardsOnHand.Count-1; i++)
            {
                for (int j = i + 1; j < cardsOnHand.Count; j++)
                {
                    if (valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                    && cardsOnHand[j].type == cardsOnHand[i].type)
                    {
                        typeDuo1 = cardsOnHand[i].type;
                        stop = true;
                        break;
                    }
                }

                if (stop) {break;}
            }
        }

        stop = false;

        if (typeDuo1 != Type.NoneType)
        {
            for (int i = 0; i < cardsOnHand.Count-1; i++)
            {
                if (cardsOnHand[i].type != typeDuo1)
                {
                    for (int j = i + 1; j < cardsOnHand.Count; j++)
                    {
                        if (valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                        && cardsOnHand[j].type == cardsOnHand[i].type)
                        {
                            typeDuo2 = cardsOnHand[i].type;
                            stop = true;
                            break;
                        }
                    }
                }

                if (stop) {break;}
            }
        }

        stop = false;

        if (typeDuo2 != Type.NoneType)
        {
            for (int i = 0; i < cardsOnHand.Count-1; i++)
            {
                if (cardsOnHand[i].type != typeDuo1 && cardsOnHand[i].type != typeDuo2)
                {
                    for (int j = i + 1; j < cardsOnHand.Count; j++)
                    {
                        if (valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                        && cardsOnHand[j].type == cardsOnHand[i].type)
                        {
                            typeDuo3 = cardsOnHand[i].type;
                            stop = true;
                            break;
                        }
                    }
                }

                if (stop) {break;}
            }
        }

        stop = false;

        if (typeDuo3 != Type.NoneType)
        {
            for (int i = 0; i < cardsOnHand.Count-1; i++)
            {
                if (cardsOnHand[i].type != typeDuo1 && cardsOnHand[i].type != typeDuo2 && cardsOnHand[i].type != typeDuo3)
                {
                    for (int j = i + 1; j < cardsOnHand.Count; j++)
                    {
                        if (valueIsEqual(cardsOnHand[i].value, cardsOnHand[j].value)
                        && cardsOnHand[j].type == cardsOnHand[i].type)
                        {
                            typeDuo4 = cardsOnHand[i].type;
                            stop = true;
                            break;
                        }
                    }
                }

                if (stop) {break;}
            }
        }

        if (typeDuo1 != Type.NoneType && typeDuo2 != Type.NoneType && typeDuo3 != Type.NoneType && typeDuo4 != Type.NoneType)
        {
            return true;
        }

        return false;
    }

    //8 card bằng giá trị
    public bool QuartetQuadDuo(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;
        
        if (CalledCardsID.Count > 0 && 
        gm.CardSet[CalledCardsID[0]] != Set.Duo && gm.CardSet[CalledCardsID[0]] != Set.Quartet)
            return false;
        
        List<int> allCards = new List<int>();
        foreach (int card in cardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }
        foreach (int card in CalledCardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }

        List<CardData> cardsOnHand = SortAndConvertHand(allCards);
        
        for (int i = 0; i < cardsOnHand.Count; i++)
        {
            if (!valueIsEqual(cardsOnHand[i].value, cardsOnHand[0].value))
                return false;
        }

        return true;
    }
    
    //1 quartet và 2 duo
    public bool QuartetDuoSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isQuartetDuo = false;

        if ((CallSet == Set.Quartet || have1Quartet(cardsID, false)) && 
        (CallSet == Set.Duo || have1Duo(cardsID, false)) && (CallSet == Set.Duo || have1Duo(cardsID, false)))
        {
            isQuartetDuo = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
            (CallSet == Set.Quartet || have1Quartet(cardsID, false)) && (CallSet == Set.Duo || have1Duo(cardsID, false)))
            {
                isQuartetDuo = true;
            }
            else
            {
                for (int i = 0; i < cardsID.Count; i++)
                {
                    gm.CardSet.Set(cardsID[i], Set.NoneSet);
                }

                if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
                (CallSet == Set.Duo || have1Duo(cardsID, false)) && (CallSet == Set.Quartet || have1Quartet(cardsID, false)))
                {
                    isQuartetDuo = true;
                }
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isQuartetDuo)
        {
            return true;
        }

        return false;
    }

    //1 quartet và 1 line 4
    public bool QuartetLineSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isQuartetLine = false;

        if ((CallSet == Set.Quartet || have1Quartet(cardsID, false)) && 
        (CallSet == Set.Line4 || have1Line4(cardsID, false)))
        {
            isQuartetLine = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Line4 || have1Line4(cardsID, false)) && 
            (CallSet == Set.Quartet || have1Quartet(cardsID, false)))
            {
                isQuartetLine = true;
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isQuartetLine)
        {
            return true;
        }

        return false;
    }

    // 1 league (4 ace hoặc hình giống nhau) và 2 duo
    public bool LeagueQuartetDuoSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isLeagueQuartetDuo = false;

        if ((CallSet == Set.League || have4Aces(cardsID, false) || have1RoyalQuartet(cardsID, false)) && 
        (CallSet == Set.Duo || have1Duo(cardsID, false)) && have1Duo(cardsID, false))
        {
            isLeagueQuartetDuo = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && 
            (CallSet == Set.League || have4Aces(cardsID, false) || have1RoyalQuartet(cardsID, false)) && 
            have1Duo(cardsID, false))
            {
                isLeagueQuartetDuo = true;
            }
            else
            {
                for (int i = 0; i < cardsID.Count; i++)
                {
                    gm.CardSet.Set(cardsID[i], Set.NoneSet);
                }

                if ((CallSet == Set.Duo || have1Duo(cardsID, false)) && have1Duo(cardsID, false) && 
                (CallSet == Set.League || have4Aces(cardsID, false) || have1RoyalQuartet(cardsID, false)))
                {
                    isLeagueQuartetDuo = true;
                }
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isLeagueQuartetDuo)
        {
            return true;
        }

        return false;
    }

    // 1 league (4 ace hoặc hình giống nhau) và 1 line 4
    public bool LeagueQuartetLineSnap(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isLeagueQuartetLine = false;

        if ((CallSet == Set.League || have4Aces(cardsID, false) || have1RoyalQuartet(cardsID, false)) && 
        (CallSet == Set.Line4 || have1Line4(cardsID, false)))
        {
            isLeagueQuartetLine = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Line4 || have1Line4(cardsID, false)) && 
            (CallSet == Set.League || have4Aces(cardsID, false) || have1RoyalQuartet(cardsID, false)))
            {
                isLeagueQuartetLine = true;
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isLeagueQuartetLine)
        {
            return true;
        }

        return false;
    }

    // 4 ace và 1 straight 4 (line 4 bài hình)
    public bool RoyalAceStraight(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isRoyalAceStraight = false;
        
        if ((CallSet == Set.League || have4Aces(cardsID, false)) && 
        (CallSet == Set.RoyalStraight || have1RoyalStraight(cardsID, false)))
        {
            isRoyalAceStraight = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.RoyalStraight || have1RoyalStraight(cardsID, false)) && 
            (CallSet == Set.League || have4Aces(cardsID, false)))
            {
                isRoyalAceStraight = true;
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isRoyalAceStraight)
        {
            return true;
        }

        return false;
    }

    // 2 straight 4 (line 4 bài hình)
    public bool AdvanceRoyal(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isAdvanceRoyal = false;
        
        if ((CallSet == Set.RoyalStraight || have1RoyalStraight(cardsID, false)) && 
        have1RoyalStraight(cardsID, false))
        {
            isAdvanceRoyal = true;
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isAdvanceRoyal)
        {
            return true;
        }

        return false;
    }

    // 1 straight 4 và 1 line 4
    public bool RoyalLine(List<int> cardsID, Set CallSet)
    {
        GameManager gm = GameManager.Instance;
        
        bool isRoyalLine = false;
        
        if ((CallSet == Set.RoyalStraight || have1RoyalStraight(cardsID, false)) && 
        (CallSet == Set.Line4 || have1Line4(cardsID, false)))
        {
            isRoyalLine = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set.Line4 || have1Line4(cardsID, false)) && 
            (CallSet == Set.RoyalStraight || have1RoyalStraight(cardsID, false)))
            {
                isRoyalLine = true;
            }
        }

        //Reset for other checks
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isRoyalLine)
        {
            return true;
        }

        return false;
    }

    // 1 straight 4 và 1 line 4 tất cả cùng chất
    public bool SuitedRoyalLine(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;
        
        bool isSuitedRoyalLine = false;

        Set CallSet = Set.NoneSet;

        List<int> allCards = new List<int>();
        foreach (int card in cardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }

        if (CalledCardsID.Count > 0)
        {
            // Check if callcard is in right combo
            if (gm.CardSet[CalledCardsID[0]] != Set.RoyalStraight && gm.CardSet[CalledCardsID[0]] != Set.Line4)
                return false;

            // Check if callcard is suited
            Type callType = GetCardById(CalledCardsID[0]).type;
            foreach (int card in CalledCardsID)
            {
                if (GetCardById(card).type != callType)
                    return false;
            }

            foreach (int card in CalledCardsID)
            {
                int temp = card;
                allCards.Add(temp);
            }

            CallSet = gm.CardSet[CalledCardsID[0]];
        }
        
        if ((CallSet == Set. RoyalStraight || have1RoyalStraight(allCards, false, -1, "suit")) && 
        (CallSet == Set. Line4 || have1Line4(allCards, false, -1, "suit")))
        {
            isSuitedRoyalLine = true;
        }
        else
        {
            for (int i = 0; i < cardsID.Count; i++)
            {
                gm.CardSet.Set(cardsID[i], Set.NoneSet);
            }

            if ((CallSet == Set. Line4 || have1Line4(allCards, false, -1, "suit")) && 
            (CallSet == Set. RoyalStraight || have1RoyalStraight(allCards, false, -1, "suit")))
            {
                isSuitedRoyalLine = true;
            }
        }
        
        for (int i = 0; i < cardsID.Count; i++)
        {
            gm.CardSet.Set(cardsID[i], Set.NoneSet);
        }

        if (isSuitedRoyalLine)
        {
            return true;
        }

        return false;
    }

    public bool GodSeries(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;
        
        bool isGodSeries = false;

        if (CalledCardsID.Count > 0 && 
        gm.CardSet[CalledCardsID[0]] != Set.Line && gm.CardSet[CalledCardsID[0]] != Set.Line4 &&
        gm.CardSet[CalledCardsID[0]] != Set.Straight && gm.CardSet[CalledCardsID[0]] != Set.RoyalStraight)
            return false;

        List<int> allCards = new List<int>();
        foreach (int card in cardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }
        foreach (int card in CalledCardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }
        
        if (haveGodSeries(allCards))
        {
            isGodSeries = true;
        }

        if (isGodSeries)
        {
            return true;
        }

        return false;
    }

    public bool SuitedGodSeries(List<int> cardsID, List<int> CalledCardsID)
    {
        GameManager gm = GameManager.Instance;
        
        bool isSuitedGodSeries = false;

        List<int> allCards = new List<int>();
        foreach (int card in cardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }
        foreach (int card in CalledCardsID)
        {
            int temp = card;
            allCards.Add(temp);
        }
        
        if (haveGodSeries(allCards, "suit"))
        {
            isSuitedGodSeries = true;
        }

        if (isSuitedGodSeries)
        {
            return true;
        }

        return false;
    }
}

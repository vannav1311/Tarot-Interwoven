using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class DeckManager : MonoBehaviour
{
    private List<int> deckIds = new List<int>();

    public int CountCards()
    {
        return deckIds.Count;
    }

    public void InitializeDeck()
    {
        deckIds.Clear();
        for (int i = 0; i < CardDatabase.Instance.cards.Count; i++)
        {
            deckIds.Add(i);
        }

        Debug.Log("Deck is initialized!");

        Shuffle();
    }

    public void Shuffle()
    {
        for (int i = 0; i < deckIds.Count; i++)
        {
            int r = Random.Range(i, deckIds.Count);
            (deckIds[i], deckIds[r]) = (deckIds[r], deckIds[i]);
        }
        Debug.Log("Deck is shuffled.");
    }

    public void AddCardToDeck(int card)
    {
        deckIds.Add(card);
        GameManager.Instance.CardPlaces.Set(card, Place.inDeck);
    }

    public int DrawCard(Place whereToGo)
    {
        if (deckIds.Count == 0) return -1;

        int card = deckIds[0];
        deckIds.RemoveAt(0);
        GameManager.Instance.CardPlaces.Set(card, whereToGo);
        
        return card;
    }

    public int MillCard()
    {
        if (deckIds.Count == 0) return -1;
        
        int card = deckIds[0];
        deckIds.RemoveAt(0);
        GameManager.Instance.CardPlaces.Set(card, Place.inDiscard);
        return card;
    }
}


/*Note:

Phải làm:
-. Thêm phần đếm thời gian một lượt chơi.
-. Thêm hoạt ảnh.
-. Từ từ làm: Điểm nhận được từ match sẽ là điểm thật ngoài đời. Top 1 đạt 100%, top 2 50, top 3 25, top 4 0.
-. Thêm phần trang hướng dẫn.
-. Thêm nhạc
-. Thêm cài đặt, chỉnh âm lượng, giftcode.
-. Quan trọng: Thay đổi hình ảnh bản quyền, cập nhật UI.
-. Quan trọng: Xử lý vấn đề hack qua client.

*/
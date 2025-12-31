using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
IPointerClickHandler, 
IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject cardNameText;
    private TextMeshProUGUI nameTMP;
    private RectTransform nameRect;

    // Chọn sprite mặt lưng trong inspector
    [SerializeField] private Sprite backSprite;

    public int thisCardID = -1;
    public PlayerController owner;

    RectTransform rect;
    CanvasGroup canvasGroup;

    bool allowDrag = false;

    int originIndex = -1;
    int previewIndex = -1;

    Vector2 dragOffset;
    private Dictionary<RectTransform, float> baseSlotX;

    private void Awake()
    {
        if (cardNameText != null)
        {
            nameTMP = cardNameText.GetComponentInChildren<TextMeshProUGUI>();
            nameRect = cardNameText.GetComponent<RectTransform>();
            cardNameText.SetActive(false);
        }

        rect = GetComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetCard(int cardID, Place place, PlayerController ownPlayer, int indexCard = -1, int totalAmount = -1)
    {
        thisCardID = cardID;

        if (nameTMP != null)
            nameTMP.text = CardDatabase.Instance.GetCardById(cardID).name;

        owner = ownPlayer;

        Image cardImage = GetComponent<Image>();
        SetSkinImage(cardID);
        cardImage.color = Color.white;

        RectTransform rt = GetComponent<RectTransform>();

        if (place == Place.inHand || place == Place.inCall)
        {
            int indexTarget = -1;
            int totalHandCards = 0;
            int totalCallCards = 0;
            int totalCards = 0;
            for (int i = 0; i < ownPlayer.CardIds.Length; i++)
            {
                if (ownPlayer.CardIds[i] == -1)
                    continue;

                if (ownPlayer.CardIds[i] == cardID)
                    indexTarget = totalHandCards;
                
                totalHandCards++;
            }
            for (int i = 0; i < ownPlayer.CallCardIds.Length; i++)
            {
                if (ownPlayer.CallCardIds[i] == -1)
                    continue;

                if (ownPlayer.CallCardIds[i] == cardID)
                    indexTarget = totalCallCards;
                
                totalCallCards++;
            }
            totalCards = totalHandCards + totalCallCards;

            if (place == Place.inHand)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(184f, 300f);

                indexTarget += totalCallCards / 2;
                float xChange = (totalHandCards - 1) / 2f - indexTarget;
                float gainDistance = totalCallCards * 30;
                if (totalCallCards == 4) gainDistance -= 96;
                rt.anchoredPosition += new Vector2( - xChange * 170 + gainDistance, 10);
            }
            else if (place == Place.inCall)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(184f, 300f);

                float xChange = -(totalCards * 170 / 2f) + indexTarget * 170;
                float gainDistance = totalCallCards * 30;
                if (totalCallCards == 4) gainDistance -= 40;
                rt.anchoredPosition += new Vector2(xChange - 20 + gainDistance, 10);
            }
        }
        if (place == Place.inDiscard)
        {
            rt.sizeDelta = new Vector2(120f, 178f);

            //Discard panel
            if (indexCard != -1 && totalAmount != -1)
            {
                // ép hệ tọa độ CHỈ CHO DISCARD
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0f, 1f);

                // grid
                int cardsPerRow = 8;
                float cardAreaW = 140f;
                float cardAreaH = 200f;

                int row = indexCard / cardsPerRow;
                int col = indexCard % cardsPerRow;

                // lệch vào trong một chút
                Vector2 start = new Vector2(30f, -20f);

                rt.anchoredPosition =
                    start + new Vector2(col * cardAreaW, -row * cardAreaH);
            }
        }
        else if (place == Place.inSpread)
        {
            for (int i = 0; i < GameManager.Instance.SpreadCardIds.Length; i++)
            {
                if (GameManager.Instance.SpreadCardIds[i] == cardID)
                {
                    rt.sizeDelta = new Vector2(126f, 200f);

                    rt.anchoredPosition += new Vector2(260 * (i - 1), 0);
                    break;
                }
            }
        }
    }

    public void SetCardBack(int cardID, Place place, PlayerController ownPlayer)
    {
        if (nameTMP != null)
            nameTMP.text = "";
            
        owner = ownPlayer;

        Image cardImage = GetComponent<Image>();
        cardImage.sprite = backSprite;
        cardImage.color = Color.white;

        RectTransform rt = GetComponent<RectTransform>();

        if (place == Place.inHand || place == Place.inCall)
        {
            int indexTarget = -1;
            int totalHandCards = 0;
            int totalCallCards = 0;
            int totalCards = 0;
            for (int i = 0; i < ownPlayer.CardIds.Length; i++)
            {
                if (ownPlayer.CardIds[i] == -1)
                    continue;

                if (ownPlayer.CardIds[i] == cardID)
                    indexTarget = totalHandCards;
                
                totalHandCards++;
            }
            for (int i = 0; i < ownPlayer.CallCardIds.Length; i++)
            {
                if (ownPlayer.CallCardIds[i] == -1)
                    continue;

                if (ownPlayer.CallCardIds[i] == cardID)
                    indexTarget = totalCallCards;
                
                totalCallCards++;
            }
            totalCards = totalHandCards + totalCallCards;
            
            if (place == Place.inHand)
            {
                rt.sizeDelta = new Vector2(82f, 140f);
                rt.anchoredPosition = Vector2.zero;

                indexTarget += totalCallCards / 2;
                float xChange = (totalHandCards - 1) / 2f - indexTarget;
                float gainDistance = totalCallCards * 14;
                if (totalCallCards == 4) gainDistance -= 40;
                rt.anchoredPosition += new Vector2(-xChange * 82 + gainDistance, 0);
            }
            if (place == Place.inCall)
            {
                thisCardID = cardID;
                if (nameTMP != null)
                    nameTMP.text = CardDatabase.Instance.GetCardById(cardID).name;

                SetSkinImage(cardID);
                rt.sizeDelta = new Vector2(82f, 140f);
                rt.anchoredPosition = Vector2.zero;

                
                float xChange = -(totalCards * 82 / 2f) + indexTarget * 82 - 20;
                float gainDistance = totalCallCards * 14;
                if (totalCallCards == 4) gainDistance -= 25;
                rt.anchoredPosition += new Vector2(xChange + gainDistance, 0);
            }
        }
        if (place == Place.inDiscard)
        {
            rt.sizeDelta = new Vector2(120f, 178f);
        }
    }

    public void SetSkinImage(int cardID)
    {
        if (CardSkinManager.Instance == null)
            return;

        Image cardImage = GetComponent<Image>();
        cardImage.sprite = CardSkinManager.Instance.GetCardSprite(cardID);
    }

    public void Refresh()
    {
        if (thisCardID < 0) return;
        
        Image cardImage = GetComponent<Image>();
        cardImage.sprite = CardSkinManager.Instance.GetCardSprite(thisCardID);
    }

    public void ShowCardName(bool showFull)
    {
        if (thisCardID < 0 || cardNameText == null || nameRect == null)
            return;

        cardNameText.SetActive(true);
        nameTMP.enableWordWrapping = false;

        // 🔒 Luôn upright – không inherit rotation từ card
        nameRect.localRotation = Quaternion.identity;
        if (showFull)
            nameRect.localScale = Vector3.one;
        else
        {
            if (GameManager.Instance.CardPlaces[thisCardID] == Place.inDiscard)
                nameRect.localScale = Vector3.one * 0.46f;
            else
                nameRect.localScale = Vector3.one * 0.6f;
        }

        const float offset = -10f;
        float zRot = 0f;

        if (owner != null)
        {
            switch (owner.displaySeat)
            {
                // Bottom (local player)
                case 0:
                    nameRect.anchorMin = new Vector2(0.5f, 1f);
                    nameRect.anchorMax = new Vector2(0.5f, 1f);
                    nameRect.pivot     = new Vector2(0.5f, 0f);
                    nameRect.anchoredPosition = new Vector2(0f, offset);
                    break;

                // Right
                case 1:
                    nameRect.anchorMin = new Vector2(0f, 0.5f);
                    nameRect.anchorMax = new Vector2(0f, 0.5f);
                    nameRect.pivot     = new Vector2(1f, 0.5f);
                    nameRect.anchoredPosition = new Vector2(-offset, 0f);
                    // zRot = -180f;
                    break;

                // Top
                case 2:
                    nameRect.anchorMin = new Vector2(0.5f, 0f);
                    nameRect.anchorMax = new Vector2(0.5f, 0f);
                    nameRect.pivot     = new Vector2(0.5f, 1f);
                    nameRect.anchoredPosition = new Vector2(0f, -offset);
                    break;

                // Left
                case 3:
                    nameRect.anchorMin = new Vector2(1f, 0.5f);
                    nameRect.anchorMax = new Vector2(1f, 0.5f);
                    nameRect.pivot     = new Vector2(0f, 0.5f);
                    nameRect.anchoredPosition = new Vector2(offset, 0f);
                    // zRot = 180f;
                    break;

                default:
                    // fallback
                    nameRect.anchoredPosition = Vector2.up * offset;
                    break;
            }
        }
        else
        {
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot     = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, offset);
        }

        nameRect.rotation = Quaternion.Euler(0f, 0f, zRot);
        cardNameText.transform.SetAsLastSibling();
    }

    public void TurnOffShowCardName()
    {
        if (cardNameText == null)
            return;

        cardNameText.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowCardName(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        TurnOffShowCardName();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        GameManager gm = GameManager.Instance;

        //Bấm card trên tay trong stage
        if (owner != null && thisCardID != -1 && gm.currentPlayer == owner.SeatIndex + 1)
        {
            if (gm.stage == Stage.Abandon)
                owner.Rpc_DiscardInAbandonPhase(thisCardID);
            else if (gm.stage == Stage.Decide && owner.isCalling)
                owner.Rpc_RequestToggleCallCard(thisCardID);
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (owner == null || !owner.Object.HasInputAuthority)
            return;

        if (owner.isInCallCards(thisCardID))
            return;

        allowDrag = true;

        originIndex = owner.GetIndexOfCardIds(thisCardID);
        previewIndex = originIndex;

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();

        // 🔑 offset để card dính tay
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset
        );

        // 🔑 chỉ capture CardIds
        baseSlotX = owner.CaptureBaseSlotX_HandOnly();
    }

    // ===============================
    // DRAG
    // ===============================
    public void OnDrag(PointerEventData eventData)
    {
        if (!allowDrag) return;

        RectTransform parentRect = rect.parent as RectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
            return;

        float draggedX = localPoint.x - dragOffset.x;

        // Card đang kéo
        rect.anchoredPosition = new Vector2(draggedX, rect.anchoredPosition.y);

        int newPreviewIndex = owner.GetPreviewIndexByX(
            draggedX,
            baseSlotX
        );

        if (newPreviewIndex != previewIndex)
        {
            previewIndex = newPreviewIndex;
            owner.PreviewShiftCards(this, originIndex, previewIndex, baseSlotX);
        }

    }

    // ===============================
    // END DRAG
    // ===============================
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!allowDrag) return;

        allowDrag = false;
        canvasGroup.blocksRaycasts = true;

        // 2️⃣ Nếu có reorder thì mới RPC
        if (originIndex != previewIndex)
        {
            owner.Rpc_RequestReorderCard(originIndex, previewIndex);
        }
        else
        {
            // 1️⃣ Luôn reset UI về layout hợp lệ
            owner.ResetAllToBasePositions(baseSlotX);
            // 3️⃣ Nếu KHÔNG reorder → force render lại hand local
            owner.RenderHand();
        }

        previewIndex = -1;
        baseSlotX = null;
    }

}

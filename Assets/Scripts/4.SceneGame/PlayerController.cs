using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using Unity.VisualScripting;
using System;

public static class TransformExt {
    public static string GetHierarchyPath(this Transform t) {
        string s = t.name;
        while (t.parent != null) {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }
}
public enum ClientReadyState
{
    None = 0,          // mới spawn
    Spawned = 1,       // PlayerController đã spawn
    SceneLoaded = 2,   // Gameplay scene load xong
    UIReady = 3,       // UI + Camera + refs ok
    FullyReady = 4     // Sẵn sàng chơi
}

public class PlayerController : NetworkBehaviour
{
    // UI Areas
    public Transform uiRoot;
    public Transform handArea;
    public Transform discardArea;
    public Transform spreadArea;
    public Transform discardPanel;
    public Transform discardContent;
    public Transform deck;
    public Transform compass;
    public Transform indicatePhase;

    public GameObject cardPrefab;
    public GameObject[] transcriptPrefabs;
    private GameObject transcriptObj;
    private bool transcriptSpawned = false;
    
    [Networked]
    public ClientReadyState ReadyState { get; set; }

    [Networked] public int SeatIndex { get; set; }
    public int displaySeat = -1;
    //For render:
    private int lastSeat = -1;

    [Networked, Capacity(8), OnChangedRender(nameof(OnHandCardsChanged))]
    public NetworkArray<int> CardIds { get; }
    [Networked, Capacity(4), OnChangedRender(nameof(OnCallCardsChanged))]
    public NetworkArray<int> CallCardIds { get; }
    [Networked] public int lastCardID { get; set; }
    [Networked] public int point { get; private set; }

    //internal
    bool uiSetupDone = false;
    private bool localReportedReady = false;

    [Networked] public NetworkBool canCall { get; set; }
    [Networked] public NetworkBool isCalling { get; set; }

    [Networked] public NetworkBool canSnap { get; set; }
    [Networked] public NetworkBool isSnaped { get; set; }

    [Networked] public NetworkBool phaseCountdown { get; set; }
    


    // -----------------------------
    // SPAWN
    // -----------------------------
    public override void Spawned()
    {
        // Cho phép chạy Render() ở client
        Runner.SetIsSimulated(Object, true);

        // Đăng ký event localSeat
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnLocalSeatSet += OnLocalSeatSet;
        }
        else
        {
            Debug.LogWarning("[Spawned] UIManager.Instance is NULL!");
        }

        // --------------------------------------
        // 1) Nếu object này thuộc quyền điều khiển của máy local
        //    → set localSeat = SeatIndex
        // --------------------------------------
        if (Object.HasInputAuthority)
        {
            UIManager.Instance.SetLocalSeat(SeatIndex);
            StartCoroutine(WaitForChangeSkin());
            StartCoroutine(WaitForSeatAndReportSpawned());
            GameManager.Instance.thisPlayer = this;
        }
        else if (Object.HasStateAuthority)
        {
            point = 0;

            canCall = false;
            isCalling = false;

            canSnap = false;
            isSnaped = false;

            phaseCountdown = false;
        }

        StartCoroutine(Register());
    }

    private IEnumerator Register()
    {
        while (GameManager.Instance == null)
            yield return null;

        if (Object.HasStateAuthority)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        TryAdvanceReadyState();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OpenAllSeeableCardNametag(true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            OpenAllSeeableCardNametag(false);
        }
    }

    void TryAdvanceReadyState()
    {
        // Đã báo xong thì thôi
        if (localReportedReady)
            return;

        // --------------------------------
        // Spawned → SceneLoaded
        // --------------------------------
        if (ReadyState == ClientReadyState.Spawned)
        {
            if (UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name == "4.Gameplay")
            {
                Rpc_ReportReadyState(ClientReadyState.SceneLoaded);
            }
            return;
        }

        // --------------------------------
        // SceneLoaded → UIReady
        // --------------------------------
        if (ReadyState == ClientReadyState.SceneLoaded)
        {
            // 2. Kiểm tra ref local
            if (GameManager.Instance == null) return;
            if (UIManager.Instance == null) return;
            if (uiRoot == null) return;
            if (handArea == null) return;
            if (spreadArea == null) return;
            if (discardArea == null) return;
            if (discardPanel == null) return;
            if (discardContent == null) return;
            if (deck == null) return;
            if (compass == null) return;
            if (indicatePhase == null) return;
            if (!uiSetupDone) return;

            Rpc_ReportReadyState(ClientReadyState.UIReady);
            return;
        }

        // --------------------------------
        // UIReady → FullyReady
        // --------------------------------
        if (ReadyState == ClientReadyState.UIReady)
        {
            if (SeatIndex < 0)
                return;

            Rpc_ReportReadyState(ClientReadyState.FullyReady);
            localReportedReady = true;
        }
    }

    private IEnumerator WaitForSeatAndReportSpawned()
    {
        while (SeatIndex < 0)
            yield return null;

        UIManager.Instance.SetLocalSeat(SeatIndex);
        Rpc_ReportReadyState(ClientReadyState.Spawned);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void Rpc_ReportReadyState(ClientReadyState state)
    {
        // chỉ cho tăng, không cho lùi
        if (state > ReadyState)
        {
            ReadyState = state;
            Debug.Log($"[Host] Player {Object.InputAuthority} ready state = {state}");
        }
    }

    void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnLocalSeatSet -= OnLocalSeatSet;
    }


    void OnLocalSeatSet(int newSeat)
    {
        TrySetupUI();
    }
    
    IEnumerator WaitForChangeSkin()
    {
        while (CardSkinManager.Instance == null)
            yield return null;

        CardSkinManager.Instance.ChangeSkin("Modern Way");
    }


    // -----------------------------
    // UI MAPPING
    // -----------------------------
    void TrySetupUI()
    {
        if (uiSetupDone) return;

        if (UIManager.Instance == null)
        {
            StartCoroutine(WaitThenSetupUI());
            return;
        }

        if (UIManager.Instance.localSeat < 0)
            return;

        UIManager ui = UIManager.Instance;

        uiRoot = ui.UIRoot;

        displaySeat = ui.GetDisplaySeat(SeatIndex);

        Transform uiHand = ui.handAreas[displaySeat];
        if (uiHand == null)
        {
            Debug.LogWarning($"SeatUI null for displaySeat {displaySeat} (SeatIndex {SeatIndex})");
            return;
        }

        handArea = uiHand;

        if (displaySeat == 0)
        {
            if (handArea != null) handArea.gameObject.SetActive(true);
        }

        RenderHand();

        discardArea = ui.discardArea;

        discardArea.GetComponent<Button>().onClick.RemoveAllListeners();
        discardArea.GetComponent<Button>().onClick.AddListener(() => OpenDiscardPanel(true));


        if (ui.discardPanel != null)
        {
            discardPanel = ui.discardPanel;

            ui.discardPanel.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            ui.discardPanel.GetComponentInChildren<Button>().onClick.
            AddListener(() => OpenDiscardPanel(false));

            discardContent = discardPanel.transform
                    .Find("DiscardPanel Scroll View/Viewport/DiscardContent")
                    .GetComponent<RectTransform>();
        }

        spreadArea = ui.spreadArea;
        
        deck = ui.deck;
        compass = ui.compass;
        indicatePhase = ui.indicatePhase;

        if (!transcriptSpawned)
        {
            SpawnTranscript();
            transcriptSpawned = true;
        }

        uiSetupDone = true;
    }

    IEnumerator WaitThenSetupUI()
    {
        float t = 0f;
        while (UIManager.Instance == null && t < 1f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (UIManager.Instance != null)
            TrySetupUI();
    }


    // -----------------------------
    // RENDER UPDATE (Fusion 2.x UI Loop)
    // -----------------------------
    public override void Render()
    {
        if (!GameManager.Instance.GameReady)
            return;

        if (UIManager.Instance == null)
            return;

        if (UIManager.Instance.localSeat < 0)
            return;

        if (SeatIndex != lastSeat)
        {
            lastSeat = SeatIndex;
            TrySetupUI();
        }
    }

    public void OnHandCardsChanged()
    {
        RenderHand();
    }

    public void OnCallCardsChanged()
    {
        RenderHand();
    }
    

    // -----------------------------
    // MANUAL UI UPDATE
    // -----------------------------
    public void RenderHand()
    {
        if (UIManager.Instance == null)
            return;

        if (UIManager.Instance.localSeat < 0)
        {
            // chưa biết localSeat -> đợi event
            return;
        }

        if (displaySeat == 0)
            StartCoroutine(ShowHand(true));
        else
        {
            //tránh việc lộ call card của người khác khi họ chưa chốt
            if (!isCalling)
                StartCoroutine(ShowHand(false));
        }
    }

    // -----------------------------
    // SORT (SERVER ONLY)
    // -----------------------------
    void SortHandCard()
    {
        if (!Object.HasStateAuthority)
            return;

        int len = CardIds.Length;

        for (int i = 0; i < len - 1; i++)
        {
            for (int j = i + 1; j < len; j++)
            {
                int a = CardIds.Get(i);
                int b = CardIds.Get(j);

                bool swap = false;

                if (a == -1 && b != -1)
                    swap = true;
                    
                else if (a != -1 && b != -1 && b < a)
                    swap = true;

                if (swap)
                {
                    CardIds.Set(i, b);
                    CardIds.Set(j, a);
                }
            }
        }
    }


    // -----------------------------
    // SHOW REAL CARDS (LOCAL ONLY)
    // -----------------------------
    public IEnumerator ShowHand(bool face)
    {
        if (CardDatabase.Instance == null) 
            Debug.LogError("[CardDB] Instance is NULL on this client!");

        if (cardPrefab == null)
            Debug.LogError("[CardPrefab] is NULL!");

        if (handArea == null) { Debug.LogWarning("ShowHandCards: handArea null for seat " + SeatIndex); yield break; }

        yield return new WaitForSeconds(0.09f);

        foreach (Transform t in handArea)
            Destroy(t.gameObject);
        
        for (int i = 0; i < CardIds.Length; i++)
        {
            int id = CardIds[i];
            if (id < 0) continue;

            GameObject obj = Instantiate(cardPrefab, handArea);

            if (face)
                obj.GetComponent<CardView>().SetCard(id, Place.inHand, this);
            else
                obj.GetComponent<CardView>().SetCardBack(id, Place.inHand, this);
        }

        for (int i = 0; i < CallCardIds.Length; i++)
        {
            int id = CallCardIds[i];
            if (id < 0) continue;

            GameObject obj = Instantiate(cardPrefab, handArea);
            
            if (face)
                obj.GetComponent<CardView>().SetCard(id, Place.inCall, this);
            else
                obj.GetComponent<CardView>().SetCardBack(id, Place.inCall, this);
        }

        if ((GameManager.Instance.stage == Stage.Abandon || (isCalling && GameManager.Instance.stage == Stage.Decide)) &&
        GameManager.Instance.currentPlayer == SeatIndex + 1)
        {
            foreach (Transform hc in handArea)
            {
                if (hc.GetComponent<CardView>().thisCardID != -1 && 
                GameManager.Instance.CardPlaces[hc.GetComponent<CardView>().thisCardID] != Place.inCall)
                    hc.GetComponent<Image>().color = new Color(249f / 255f, 242f / 255f, 163f / 255f);
            }
        }
    }

    public IEnumerator RenderDiscardZone()
    {
        if (discardArea == null) { Debug.LogWarning("discardArea null"); yield break; }
        if (CardSkinManager.Instance == null || GameManager.Instance == null) yield break;

        yield return new WaitForSeconds(0.3f);

        if (GameManager.Instance.lastDiscard != -1)
        {
            discardArea.GetComponent<Image>().sprite = 
            CardSkinManager.Instance.GetCardSprite(GameManager.Instance.lastDiscard);
        }
        else
        {
            discardArea.GetComponent<Image>().sprite = 
            UIManager.Instance.discardAreaImage;
        }
    }

    public IEnumerator RenderAugurCard()
    {
        if (spreadArea == null) { Debug.LogWarning("spreadArea null"); yield break; }

        foreach (Transform t in spreadArea)
            Destroy(t.gameObject);

        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < GameManager.Instance.SpreadCardIds.Length; i++)
        {
            int id = GameManager.Instance.SpreadCardIds[i];
            if (id < 0) continue;

            GameObject obj = Instantiate(cardPrefab, spreadArea);
            obj.GetComponent<CardView>().SetCard(id, Place.inSpread, null);
        }

        //Gọi hiệu ứng cho augur card sau khi tự động render xong cho đúng tick
        if (Object.HasInputAuthority && GameManager.Instance.currentPlayer == SeatIndex + 1 &&
        (GameManager.Instance.stage == Stage.ChoosePath || GameManager.Instance.stage == Stage.Spread))
        {
            AugurCardManager();
        }
    }
    
    public void OpenDiscardPanel(bool open)
    {
        if (discardPanel == null) return;

        discardPanel.gameObject.SetActive(open);

        if (open)
        {
            GameManager gm = GameManager.Instance;
            List<int> discardIDCurrent = new List<int>();
            for (int i = 0; i < gm.DiscardIds.Length; i++)
            {
                if (gm.DiscardIds[i] != -1)
                    discardIDCurrent.Add(gm.DiscardIds[i]);
            }

            for (int i = 0; i < discardIDCurrent.Count; i++)
            {
                GameObject obj = Instantiate(cardPrefab, discardContent);

                obj.GetComponent<CardView>().SetCard(discardIDCurrent[i], Place.inDiscard, null, i, discardIDCurrent.Count);
            }
        }
        else
        {
            foreach (Transform card in discardContent)
                Destroy(card.gameObject);
        }
    }

    void SpawnTranscript()
    {
        if (transcriptPrefabs == null || transcriptPrefabs.Length < 4)
        {
            Debug.LogError("TranscriptPrefabs phải có 4 phần tử!");
            return;
        }

        if (handArea == null)
        {
            Debug.LogWarning("handArea chưa map xong → không spawn transcript");
            return;
        }

        // Hủy object cũ nếu có
        if (transcriptObj != null)
            Destroy(transcriptObj);

        // Chọn prefab theo seatIndex GỐC
        var prefab = transcriptPrefabs[SeatIndex];
        if (prefab == null)
        {
            Debug.LogError($"TranscriptPrefab[{SeatIndex}] = NULL");
            return;
        }

        // Lấy RectTransform của handArea
        RectTransform handRect = handArea.GetComponent<RectTransform>();
        RectTransform parentCanvas = handRect.parent.GetComponent<RectTransform>();

        transcriptObj = Instantiate(prefab, parentCanvas);
        RectTransform transRect = transcriptObj.GetComponent<RectTransform>();

        // Copy scale UI để khỏi bị phóng to thu nhỏ sai
        transRect.localScale = handRect.localScale;

        if (displaySeat == 0)
            transRect.anchoredPosition = new Vector2(780.35f, -443f);
        else if (displaySeat == 1)
            transRect.anchoredPosition = new Vector2(780.35f, 428f);
        else if (displaySeat == 2)
            transRect.anchoredPosition = new Vector2(-780.35f, 428f);
        else if (displaySeat == 3)
            transRect.anchoredPosition = new Vector2(-780.35f, -443f);
    }

    // -----------------------------
    // Choose path + Spread phase
    // -----------------------------

    public void AugurCardManager()
    {
        foreach (Transform sc in spreadArea)
        {
            int card = sc.GetComponent<CardView>().thisCardID;
            sc.GetComponent<Image>().color = new Color(249f / 255f, 242f / 255f, 163f / 255f);
            sc.GetComponent<Button>().onClick.RemoveAllListeners();
            sc.GetComponent<Button>().onClick.AddListener(() => Rpc_SelectSpreadCard(card));
        }
    }

    //Khi người chơi chọn augur card trong spread
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SelectSpreadCard(int card)
    {
        SelectSpreadCard(card);
    }

    public void SelectSpreadCard(int card)
    {
        if (!Object.HasStateAuthority) return;
        
        addCardToHand(card, Place.inSpread);

        GameManager gm = GameManager.Instance;
        
        if (gm.stage == Stage.ChoosePath)
        {
            lastCardID = -1;

            for (int i = 0 ; i < gm.SpreadCardIds.Length; i++)
            {
                if (gm.SpreadCardIds[i] == card)
                    gm.SpreadCardIds.Set(i, -1);
                else if (gm.SpreadCardIds[i] > -1)
                {
                    //với stage choose path đầu game, card sẽ add ngược lại deck
                    gm.AddCardToDeck(gm.SpreadCardIds[i], Place.inSpread);
                    gm.SpreadCardIds.Set(i, -1);
                }
                gm.deck.Shuffle();
            }
        }
        else if (gm.stage == Stage.Spread)
        {
            lastCardID = card;

            for (int i = 0 ; i < gm.SpreadCardIds.Length; i++)
            {
                if (gm.SpreadCardIds[i] == card)
                    gm.SpreadCardIds.Set(i, -1);
                else if (gm.SpreadCardIds[i] > -1)
                {
                    //với stage spread thông thường, card không được chọn sẽ bị loại bỏ
                    gm.Discard(gm.SpreadCardIds[i], Place.inSpread);
                    gm.SpreadCardIds.Set(i, -1);
                }
            }

        }

        gm.pendingEnter = true;
    }

    // -----------------------------
    // Abandon phase
    // -----------------------------
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_AbandonPhaseSetUp()
    {
        foreach (Transform hc in handArea)
        {
            if (hc.GetComponent<CardView>().thisCardID != -1 && 
                GameManager.Instance.CardPlaces[hc.GetComponent<CardView>().thisCardID] != Place.inCall)
                hc.GetComponent<Image>().color = new Color(249f / 255f, 242f / 255f, 163f / 255f);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_DiscardInAbandonPhase(int card)
    {
        DiscardInAbandonPhase(card);
    }
    public void DiscardInAbandonPhase(int card)
    {
        for (int i = 0; i < CardIds.Length; i++)
        {
            if (CardIds[i] == card)
            {
                CardIds.Set(i, -1);
                GameManager.Instance.Discard(card, Place.inHand);

                foreach (Transform t in handArea)
                {
                    t.gameObject.GetComponent<Image>().color = new Color(1, 1, 1);
                    t.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
                }

                GameManager.Instance.pendingEnter = true;

                break;
            }
        }

        ResetPlayerState();
    }
    
    // -----------------------------
    // Decide phase
    // -----------------------------


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_GiveDecision(string button)
    {
        GameManager gm = GameManager.Instance;

        if (button == "CallButton")
        {
            if (lastCardID == -1)
                return;

            Rpc_CallToOpenChooseComboCall(true);
            isCalling = true;

            //Add last card vào call trước
            for (int i = 0; i < CallCardIds.Length; i++)
            {
                if (CallCardIds[i] == -1)
                {
                    CallCardIds.Set(i, lastCardID);

                    //Xóa last card khỏi hand
                    for (int j = 0; j < CardIds.Length; j++)
                    {
                        if (CardIds[j] == lastCardID)
                        {
                            CardIds.Set(j, -1);
                            break;
                        }
                    }
                    break;
                }
            }
        }
        else if (button == "SnapButton")
        {
            OnSnapButton();
        }
        else if (button == "PassButton")
        {
            gm.pendingEnter = true;

            ResetPlayerState();
        }
    }
    
    // Call
    public void HasCombo()
    {
        if (!Object.HasStateAuthority) return;

        if (lastCardID == -1) return;

        //Check Call Link và nếu đã call rồi thì không call nữa
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] != -1)
            {
                CardData checkCard = CardDatabase.Instance.GetCardById(CallCardIds[i]);

                if (GameManager.Instance.CardSet[CallCardIds[i]] == Set.Triplet && 
                CardDatabase.Instance.valueIsEqual(CardDatabase.Instance.GetCardById(lastCardID).value, checkCard.value))
                {
                    canCall = true;
                    return;
                }
                else 
                    return;
            }
        }

        List<int> cardIDsTemp = new List<int>();
        for (int i = 0; i < CardIds.Length; i++)
        {
            if (CardIds[i] == -1)
                continue;

            int cardTemp = CardIds[i];
            cardIDsTemp.Add(cardTemp);
        }

        if (CardDatabase.Instance.have1Triplet(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have1Line(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have1Quartet(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have1Straight(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have1Line4(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have4Aces(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have1RoyalQuartet(cardIDsTemp, true, lastCardID) ||
            CardDatabase.Instance.have1RoyalStraight(cardIDsTemp, true, lastCardID))
            {
                canCall = true;
            }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_CallToOpenChooseComboCall(bool open)
    {
        foreach (Transform hc in handArea)
        {
            if (hc.GetComponent<CardView>().thisCardID != -1 && 
                GameManager.Instance.CardPlaces[hc.GetComponent<CardView>().thisCardID] != Place.inCall)
                hc.GetComponent<Image>().color = open ? new Color(249f / 255f, 242f / 255f, 163f / 255f) : new Color(1, 1, 1);
        }

        UIManager.Instance.displayChooseComboScene(open);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestToggleCallCard(int cardID)
    {
        if (!Object.HasStateAuthority) return;
        if (GameManager.Instance.stage != Stage.Decide) return;
        
        if (cardID == lastCardID) return; //Ngăn không đưa last card ra ngoài

        if (GameManager.Instance.CardPlaces[cardID] == Place.inCall) return;  //Ngăn không cho đưa card đã call ra ngoài

        //nếu thẻ đã ở trong callcardid thì đưa nó lên lại hand
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == cardID && CallCardIds[i] != -1)
            {
                CallCardIds.Set(i, -1);
                addCardToHand(cardID, Place.inCall);
                return;
            }
        }
        //thêm vào call combo
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == -1)
            {
                CallCardIds.Set(i, cardID);

                //Xóa cardid khỏi hand
                for (int j = 0; j < CardIds.Length; j++)
                {
                    if (CardIds[j] == cardID)
                    {
                        CardIds.Set(j, -1);
                        break;
                    }
                }
                return;
            }
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_OnCallButton()
    {
        CardDatabase cd = CardDatabase.Instance;
        Set inCombo = Set.NoneSet;

        List<int> callCardIDsTemp = new List<int>();
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == -1)
                continue;

            int cardTemp = CallCardIds[i];
            callCardIDsTemp.Add(cardTemp);
        }

        //prepare call link
        bool isLink = false;
        int tripletCardID = -1;
        for (int i = 0; i < callCardIDsTemp.Count; i++)
        {
            if (GameManager.Instance.CardSet[callCardIDsTemp[i]] == Set.Triplet)
            {
                tripletCardID = callCardIDsTemp[i];
                continue;
            }
            if (tripletCardID != -1 && GameManager.Instance.CardSet[callCardIDsTemp[i]] == Set.NoneSet)
            {
                if (cd.valueIsEqual(cd.GetCardById(callCardIDsTemp[i]).value, cd.GetCardById(tripletCardID).value))
                    isLink = true;
            }
        }
        //start check
        if (callCardIDsTemp.Count == 4 && isLink)
        {
            PointIncreasing(15);
            inCombo = Set.Quartet;
            
            for (int i = 0; i < CallCardIds.Length; i++)
            {
                if (CallCardIds[i] == -1)
                {
                    Debug.Log("Special Error while checking Link Call!!");
                    continue;
                }
                
                GameManager.Instance.CardSet.Set(CallCardIds[i], Set.Quartet);
            }

            Rpc_AppearSuccessMsg("Link");
        }
        else if (cd.have4Aces(callCardIDsTemp, false, lastCardID) || cd.have1RoyalQuartet(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(50);
            inCombo = Set.League;

            Rpc_AppearSuccessMsg("Apocalypt");
        }
        else if (cd.have1RoyalStraight(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(50);
            inCombo = Set.RoyalStraight;

            Rpc_AppearSuccessMsg("Royal Ascend");
        }
        else if (cd.have1Straight(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(45);
            inCombo = Set.Straight;

            Rpc_AppearSuccessMsg("Ascend");
        }
        else if (cd.have1Line4(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(40);
            inCombo = Set.Line4;

            Rpc_AppearSuccessMsg("Arise");
        }
        else if (cd.have1Quartet(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(35);
            inCombo = Set.Quartet;

            Rpc_AppearSuccessMsg("Quadruple");
        }
        else if (cd.have1Line(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(30);
            inCombo = Set.Line;

            Rpc_AppearSuccessMsg("Raise");
        }
        else if (cd.have1Triplet(callCardIDsTemp, false, lastCardID))
        {
            PointIncreasing(25);
            inCombo = Set.Triplet;

            Rpc_AppearSuccessMsg("Tripled");
        }

        if (inCombo != Set.NoneSet)
        {
            for (int i = 0; i < CallCardIds.Length; i++)
            {
                if (CallCardIds[i] == -1) continue;

                //Lọc những card không trong combo trả về tay
                if (GameManager.Instance.CardSet[CallCardIds[i]] == Set.NoneSet)
                {
                    addCardToHand(CallCardIds[i], Place.inCall);
                    CallCardIds.Set(i, -1);
                    continue;
                }

                GameManager.Instance.CardPlaces.Set(CallCardIds[i], Place.inCall);
            }
            
            Rpc_CallToOpenChooseComboCall(false);
            isCalling = false;
            
            ResetPlayerState();

            GameManager.Instance.pendingEnter = true;

            return;
        }
        else
        {
            Rpc_DisplayInfoPanel(InfoKind.Not, "Don't find any combo!");
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_OnCancelCallButton()
    {
        isCalling = false;

        //Add lại card trong call vào hand
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == -1) continue;

            if (GameManager.Instance.CardPlaces[CallCardIds[i]] != Place.inCall)
            {
                addCardToHand(CallCardIds[i], Place.inCall);
                CallCardIds.Set(i, -1);
            }
        }

        Rpc_CallToOpenChooseComboCall(false);
    }

    // Snap
    public void CanSnap()
    {
        if (!Object.HasStateAuthority) return;

        CardDatabase cd = CardDatabase.Instance;

        List<int> cardIDsTemp = new List<int>();
        for (int i = 0; i < CardIds.Length; i++)
        {
            if (CardIds[i] == -1)
                continue;

            int cardTemp = CardIds[i];
            cardIDsTemp.Add(cardTemp);
        }

        List<int> callCardIDsTemp = new List<int>();
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == -1)
                continue;

            int cardTemp = CallCardIds[i];
            callCardIDsTemp.Add(cardTemp);
        }

        Set hadCombo = Set.NoneSet;
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] != -1 && GameManager.Instance.CardSet[CallCardIds[i]] != Set.NoneSet)
            {
                hadCombo = GameManager.Instance.CardSet[CallCardIds[i]];
                break;
            }
        }

        if (cd.SimpleSnap(cardIDsTemp, hadCombo) ||
        cd.MatchPairSnap(cardIDsTemp, hadCombo) ||
        cd.LinePairSnap(cardIDsTemp, hadCombo) ||
        cd.LineSuitSnap(cardIDsTemp, callCardIDsTemp) ||
        cd.QuadDuoSnap(cardIDsTemp, hadCombo) ||
        cd.SuitedQuadDuoSnap(cardIDsTemp, callCardIDsTemp) ||
        cd.FullHouseQuadDuo(cardIDsTemp, callCardIDsTemp) ||
        cd.QuartetQuadDuo(cardIDsTemp, callCardIDsTemp) ||
        cd.QuartetDuoSnap(cardIDsTemp, hadCombo) ||
        cd.QuartetLineSnap(cardIDsTemp, hadCombo) ||
        cd.LeagueQuartetDuoSnap(cardIDsTemp, hadCombo) ||
        cd.LeagueQuartetLineSnap(cardIDsTemp, hadCombo) ||
        cd.RoyalAceStraight(cardIDsTemp, hadCombo) ||
        cd.AdvanceRoyal(cardIDsTemp, hadCombo) ||
        cd.RoyalLine(cardIDsTemp, hadCombo) ||
        cd.SuitedRoyalLine(cardIDsTemp, callCardIDsTemp) ||
        cd.GodSeries(cardIDsTemp, callCardIDsTemp) ||
        cd.SuitedGodSeries(cardIDsTemp, callCardIDsTemp))
        {
            canSnap = true;
        }
    }

    public void OnSnapButton()
    {
        if (!Object.HasStateAuthority) return;

        CardDatabase cd = CardDatabase.Instance;

        List<int> cardIDsTemp = new List<int>();
        for (int i = 0; i < CardIds.Length; i++)
        {
            if (CardIds[i] == -1)
                continue;

            int cardTemp = CardIds[i];
            cardIDsTemp.Add(cardTemp);
        }

        List<int> callCardIDsTemp = new List<int>();
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == -1)
                continue;

            int cardTemp = CallCardIds[i];
            callCardIDsTemp.Add(cardTemp);
        }

        Set hadCombo = Set.NoneSet;
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] != -1 &&  GameManager.Instance.CardSet[CallCardIds[i]] != Set.NoneSet)
            {
                hadCombo = GameManager.Instance.CardSet[CallCardIds[i]];
                break;
            }
        }

        if (cd.RoyalAceStraight(cardIDsTemp, hadCombo))
        {
            PointIncreasing(1200);

            Rpc_AppearSuccessMsg("Royal Ace Straight Snap");
        }
        else if (cd.AdvanceRoyal(cardIDsTemp, hadCombo))
        {
            PointIncreasing(1100);

            Rpc_AppearSuccessMsg("Advance Royal Snap");
        }
        else if (cd.SuitedRoyalLine(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(1000);

            Rpc_AppearSuccessMsg("Suited Royal Line Snap");
        }
        else if (cd.QuartetQuadDuo(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(1000);

            Rpc_AppearSuccessMsg("Quartet Quad Duo Snap");
        }
        else if (cd.SuitedQuadDuoSnap(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(900);

            Rpc_AppearSuccessMsg("Suited Quad Duo Snap");
        }
        else if (cd.LeagueQuartetLineSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(850);

            Rpc_AppearSuccessMsg("League Quartet Line Snap");
        }
        else if (cd.RoyalLine(cardIDsTemp, hadCombo))
        {
            PointIncreasing(850);

            Rpc_AppearSuccessMsg("Royal Line Snap");
        }
        else if (cd.SuitedGodSeries(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(800);

            Rpc_AppearSuccessMsg("Suited God Series Snap");
        }
        else if (cd.LineSuitSnap(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(800);

            Rpc_AppearSuccessMsg("Line Suit Snap");
        }
        else if (cd.FullHouseQuadDuo(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(750);

            Rpc_AppearSuccessMsg("Full House Quad Duo Snap");
        }
        else if (cd.LeagueQuartetDuoSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(650);

            Rpc_AppearSuccessMsg("League Quartet Duo Snap");
        }
        else if (cd.QuartetLineSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(600);

            Rpc_AppearSuccessMsg("Quartet Line Snap");
        }
        else if (cd.LinePairSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(400);

            Rpc_AppearSuccessMsg("Line Pair Snap");
        }
        else if (cd.GodSeries(cardIDsTemp, callCardIDsTemp))
        {
            PointIncreasing(300);

            Rpc_AppearSuccessMsg("God Series Snap");
        }
        else if (cd.SimpleSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(300);

            Rpc_AppearSuccessMsg("Simple Snap");
        }
        else if (cd.MatchPairSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(250);

            Rpc_AppearSuccessMsg("Match Pair Snap");
        }
        else if (cd.QuartetDuoSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(200);

            Rpc_AppearSuccessMsg("Quartet Duo Snap");
        }
        else if (cd.QuadDuoSnap(cardIDsTemp, hadCombo))
        {
            PointIncreasing(100);

            Rpc_AppearSuccessMsg("Quad Duo Snap");
        }
        else
        {
            Rpc_DisplayInfoPanel(InfoKind.Not, "Don't find any Snap!");
            return;
        }
        
        //Đưa card lại vào tay từ call
        for (int i = 0 ; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == -1)
                continue;
            
            addCardToHand(CallCardIds[i], Place.inCall);
            CallCardIds.Set(i, -1);
        }

        //Xóa toàn bộ set call và thay nơi thành snap
        for (int i = 0 ; i < CardIds.Length; i++)
        {
            if (CardIds[i] == -1)
                continue;
            
            GameManager.Instance.CardSet.Set(CardIds[i], Set.NoneSet);
            GameManager.Instance.CardPlaces.Set(CardIds[i], Place.inSnap);
        }

        isSnaped = true;

        ResetPlayerState();

        GameManager.Instance.CheckEndGame();
    }
    //-----------------------------
    // GAINS
    //-----------------------------

    public void PlayerGains()
    {
        int numberPlayersRemaining = 0;
        for (int i = 0; i < GameManager.Instance.PlayerCount; i++)
        {
            if (!GameManager.Instance.GetPlayerAtSeat(i).isSnaped)
                numberPlayersRemaining++;
        }

        PointIncreasing(numberPlayersRemaining * 30);
    }
    

    //Kéo thả thẻ bài trên tay
    public Dictionary<RectTransform, float> CaptureBaseSlotX_HandOnly()
    {
        Dictionary<RectTransform, float> dict = new();

        foreach (Transform t in handArea)
        {
            CardView cv = t.GetComponent<CardView>();
            if (cv == null) continue;

            // ❌ bỏ qua CallCard
            if (isInCallCards(cv.thisCardID))
                continue;

            RectTransform rt = t as RectTransform;
            dict[rt] = rt.anchoredPosition.x;
        }

        return dict;
    }

    public int GetPreviewIndexByX(
        float draggedX,
        Dictionary<RectTransform, float> baseSlotX
    )
    {
        // ordered CardIds theo X gốc
        List<float> slots = baseSlotX
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Value)
            .ToList();

        for (int i = 0; i < slots.Count - 1; i++)
        {
            float mid = (slots[i] + slots[i + 1]) * 0.5f;
            if (draggedX < mid)
                return i;
        }

        return slots.Count - 1;
    }

    public void PreviewShiftCards(
        CardView draggingCard,
        int originIndex,
        int previewIndex,
        Dictionary<RectTransform, float> baseSlotX
    )
    {
        RectTransform dragRect = draggingCard.transform as RectTransform;
        float offset = dragRect.rect.width;

        // 🔑 1) LẤY LIST CARD & SORT THEO X GỐC
        List<RectTransform> ordered = baseSlotX
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        // 🔑 2) DUYỆT THEO INDEX LOGIC (KHÔNG PHẢI HIERARCHY)
        for (int i = 0; i < ordered.Count; i++)
        {
            RectTransform card = ordered[i];
            if (card == dragRect) continue;

            float baseX = baseSlotX[card];

            // ===============================
            // KÉO TRÁI → PHẢI
            // ===============================
            if (previewIndex > originIndex)
            {
                if (i > originIndex && i <= previewIndex)
                    card.anchoredPosition =
                        new Vector2(baseX - offset, card.anchoredPosition.y);
                else
                    card.anchoredPosition =
                        new Vector2(baseX, card.anchoredPosition.y);
            }
            // ===============================
            // KÉO PHẢI → TRÁI
            // ===============================
            else if (previewIndex < originIndex)
            {
                if (i >= previewIndex && i < originIndex)
                    card.anchoredPosition =
                        new Vector2(baseX + offset, card.anchoredPosition.y);
                else
                    card.anchoredPosition =
                        new Vector2(baseX, card.anchoredPosition.y);
            }
            else
            {
                card.anchoredPosition =
                    new Vector2(baseX, card.anchoredPosition.y);
            }
        }
    }


    public void ResetAllToBasePositions(
        Dictionary<RectTransform, float> baseSlotX
    )
    {
        foreach (var kv in baseSlotX)
        {
            kv.Key.anchoredPosition = new Vector2(
                kv.Value,
                kv.Key.anchoredPosition.y
            );
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestReorderCard(int from, int to)
    {
        if (!Object.HasStateAuthority) return;
        if (from < 0 || to < 0) return;

        int card = CardIds[from];
        if (card < 0) return;

        if (from < to)
        {
            for (int i = from; i < to; i++)
                CardIds.Set(i, CardIds[i + 1]);
        }
        else
        {
            for (int i = from; i > to; i--)
                CardIds.Set(i, CardIds[i - 1]);
        }

        CardIds.Set(to, card);
    }


    //-----------------------------
    // VISUAL CARD MOVING
    //-----------------------------
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_DrawCardVisual(int cardId, Place fromWhere, Place toWhere)
    {
        if (UIManager.Instance == null) return;

        Vector3 from = GetWorldPosByPlace(fromWhere, cardId);
        Vector3 to = GetWorldPosByPlace(toWhere, cardId);

        StartCoroutine(CardMoveAnim(cardId, fromWhere, toWhere, from, to));
    }
    
    IEnumerator CardMoveAnim(int cardId, Place fromWhere, Place toWhere, Vector3 from, Vector3 to)
    {
        GameObject flyCard = Instantiate(cardPrefab, from, Quaternion.identity, uiRoot);
        if (fromWhere == Place.inDeck && toWhere == Place.inHand)
            flyCard.GetComponent<CardView>().SetCardBack(cardId, Place.None, null);
        else
            flyCard.GetComponent<CardView>().SetCard(cardId, Place.None, null);

        if (toWhere == Place.inHand)
            flyCard.GetComponent<RectTransform>().sizeDelta = new Vector2(184f, 300f);
        else if (toWhere == Place.inDiscard)
            flyCard.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 178f);
        else if (toWhere == Place.inSpread)
            flyCard.GetComponent<RectTransform>().sizeDelta = new Vector2(126f, 200f);
        else if (toWhere == Place.inDeck)
            flyCard.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 178f);

        Vector3 start = from;
        Vector3 end = to;

        float t = 0f;
        float dur = 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = EaseOut(t);

            flyCard.transform.position =
                Vector3.Lerp(start, end, eased);

            flyCard.transform.localScale =
                Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, eased);

            yield return null;
        }

        Destroy(flyCard);
        RenderHand();
    }


    float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_HostCallShakeDeck()
    {
        StartCoroutine(ShakeDeck(deck));
    }

    public IEnumerator ShakeDeck(Transform deck)
    {
        Vector3 originalPos = deck.localPosition;

        float duration = 0.1f;
        float elapsed = 0f;
        float strength = 8f; // độ rung (UI pixel)

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float offsetX = UnityEngine.Random.Range(-1f, 1f) * strength;
            float offsetY = UnityEngine.Random.Range(-1f, 1f) * strength;

            deck.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        deck.localPosition = originalPos;
    }


    //-----------------------------
    // GENERAL
    //-----------------------------
    
    public void addCardToHand(int cardID, Place from)
    {
        if (!Object.HasStateAuthority) return;

        bool isAdded = false;

        for (int i = 0 ; i < CardIds.Length; i++)
        {
            if (CardIds[i] == -1)
            {
                CardIds.Set(i, cardID);
                GameManager.Instance.CardPlaces.Set(cardID, Place.inHand);
                isAdded = true;
                break;
            }
        }

        if (isAdded)
        {
            if (from != Place.inCall)
            {
                Rpc_DrawCardVisual(cardID, from, Place.inHand);
            }
            return;
        }
        else
        {
            Debug.Log("Handfull, cannot add card to hand.");
            //Discard nếu hand đã đầy
            GameManager.Instance.Discard(cardID, from);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void Rpc_DisplayInfoPanel(InfoKind kind, string text)
    {
        if (UIManager.Instance == null)
            return;

        switch (kind)
        {
            case InfoKind.Not:
                UIManager.Instance.ShowSystemMessage(UIManager.Label.Info, text, 1.82f);
                break;
        }
    }

    public void PointIncreasing(int pointPlus)
    {
        point += pointPlus;
        GameManager.Instance.playerPoints.Set(SeatIndex, point);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_CallClientToRender()
    {
        RenderHand();
        StartCoroutine(RenderAugurCard());
        StartCoroutine(RenderDiscardZone());
    }

    public void ResetPlayerState()
    {
        canCall = false;
        isCalling = false;

        canSnap = false;

        Rpc_CallClientToRender();
    }

    public Vector3 GetWorldPosByPlace(Place where, int cardID)
    {
        switch (where)
        {
            case Place.inDeck:
            {
                RectTransform deckRt = deck.GetComponent<RectTransform>();
                return deckRt.position; 
            }

            case Place.inDiscard:
            {
                RectTransform discardRt = discardArea.GetComponent<RectTransform>();
                return discardRt.position;
            }

            case Place.inSpread:
            {
                RectTransform spreadRt = spreadArea.GetComponent<RectTransform>();

                int count = GameManager.Instance.SpreadCardIds.Length;

                for (int i = 0; i < count; i++)
                {
                    if (GameManager.Instance.SpreadCardIds[i] == cardID)
                    {
                        float spacing = 260f;
                        float startX = -(count - 1) * spacing * 0.5f;

                        Vector2 localOffset =
                            new Vector2(startX + i * spacing, 0f);

                        // đổi local UI → world (để animation)
                        return spreadRt.TransformPoint(localOffset);
                    }
                }

                break;
            }

            case Place.inHand:
            {
                RectTransform handRt = UIManager.Instance.handAreas[displaySeat].GetComponent<RectTransform>();

                int indexTarget = -1;
                int totalHandCards = 0;
                int totalCallCards = 0;

                for (int i = 0; i < CardIds.Length; i++)
                {
                    if (CardIds[i] == -1) continue;

                    if (CardIds[i] == cardID)
                        indexTarget = totalHandCards;

                    totalHandCards++;
                }

                for (int i = 0; i < CallCardIds.Length; i++)
                {
                    if (CallCardIds[i] == -1) continue;

                    if (CallCardIds[i] == cardID)
                        indexTarget = totalCallCards;

                    totalCallCards++;
                }

                indexTarget += totalCallCards / 2;

                float xChange = (totalHandCards - 1) / 2f - indexTarget;
                float gainDistance = totalCallCards * 30f;
                if (totalCallCards == 4) gainDistance -= 96f;

                // 🔑 TÍNH LOCAL OFFSET — KHÔNG GÁN
                Vector2 localOffset =
                    new Vector2(-xChange * 170f + gainDistance, 10f);

                // 🔑 CHUYỂN SANG WORLD POS
                return handRt.TransformPoint(localOffset);
            }
        }

        return Vector3.zero;
    }

    public bool isInCallCards(int cardId)
    {
        for (int i = 0; i < CallCardIds.Length; i++)
        {
            if (CallCardIds[i] == cardId)
                return true;
        }

        return false;
    }

    public int GetIndexOfCardIds(int cardID)
    {
        for (int i = 0; i < CardIds.Length; i++)
        {
            if (CardIds[i] == cardID)
                return i;
        }
        return -1;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_CallToHostToSettingCardPlace(int cardID, Place place)
    {
        GameManager.Instance.CardPlaces.Set(cardID, place);
    }

    public void OpenAllSeeableCardNametag(bool open)
    {
        CardView[] allCards = FindObjectsOfType<CardView>();
        foreach (var card in allCards)
        {
            if (open)
                card.ShowCardName(false);
            else
                card.TurnOffShowCardName();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_AppearSuccessMsg(string msg)
    {
        UIManager.Instance.ShowSystemMessage(UIManager.Label.Success, msg, 2f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_TurnOffStartGameCoverPanel()
    {
        UIManager.Instance.EmptyBoard.SetActive(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_DisplayResultGame(int rank)
    {
        
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public enum Stage
{
    None,
    Startgame,
    ChoosePath,
    Abandon,
    Spread,
    Decide,
    Renew,
    Endgame
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    // =========================
    // NETWORKED STATE
    // =========================

    [Networked] public NetworkBool GameReady { get; set; }
    [Networked] private TickTimer DelayTick { get; set; }
    [Networked] public int DeckCount { get; set; }

    [Networked, Capacity(56), OnChangedRender(nameof(OnDiscardsChanged))]
    public NetworkArray<int> DiscardIds { get; }
    [Networked] public int lastDiscard { get; set; }

    [Networked, Capacity(3), OnChangedRender(nameof(OnSpreadChanged))]
    public NetworkArray<int> SpreadCardIds { get; }

    [Networked, Capacity(56)]
    public NetworkArray<Place> CardPlaces { get; }
    [Networked, Capacity(56)]
    public NetworkArray<Set> CardSet { get; }

    [Networked] public int MatchCount { get; set; }

    // ⭐ SOURCE OF TRUTH: PLAYERS
    [Networked, Capacity(4)]
    public NetworkArray<PlayerRef> players { get; }
    [Networked, Capacity(4)]
    public NetworkArray<int> playerPoints { get; }

    [Networked] public Stage stage { get; set; }
    [Networked] public int currentPlayer { get; set; }
    [Networked] public NetworkBool pendingEnter { get; set; }
    [Networked] public int PhaseEndTick { get; set; }

    // =========================
    // LOCAL REFERENCES
    // =========================

    public CardDatabase cardDatabase;
    public DeckManager deck;
    public List<Button> spreadCardButtons = new();

    public PlayerController thisPlayer;
    public bool startPhase = false;

    public new NetworkRunner Runner { get; private set; }

    // =========================
    // LIFECYCLE
    // =========================

    public override void Spawned()
    {
        Instance = this;
        Runner = Object.Runner;

        if (deck == null)
            deck = FindObjectOfType<DeckManager>();

        if (Object.HasStateAuthority)
        {
            GameReady = false;
            stage = Stage.None;
            currentPlayer = 0;

            if (deck != null)
                deck.InitializeDeck();

            for (int i = 0; i < DiscardIds.Length; i++)
                DiscardIds.Set(i, -1);

            for (int i = 0; i < SpreadCardIds.Length; i++)
                SpreadCardIds.Set(i, -1);

            for (int i = 0; i < CardPlaces.Length; i++)
                CardPlaces.Set(i, Place.inDeck);

            for (int i = 0; i < CardSet.Length; i++)
                CardSet.Set(i, Set.NoneSet);

            for (int i = 0; i < players.Length; i++)
                players.Set(i, PlayerRef.None);

            lastDiscard = -1;
        }
    }

    // =========================
    // PLAYER REGISTRATION
    // =========================

    public void RegisterPlayer(PlayerController pc)
    {
        if (!Runner.IsServer)
            return;

        int seat = pc.SeatIndex;

        if (seat < 0 || seat >= LobbyInfo.numberParticipants)
        {
            Debug.LogError($"[RegisterPlayer] Invalid seat {seat}");
            return;
        }

        players.Set(seat, pc.Object.InputAuthority);
    }

    public int AssignSeat(PlayerRef player)
    {
        if (!Runner.IsServer)
            return -1;

        // Nếu player đã có seat (tránh assign lại khi reload / respawn)
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == player)
                return i;
        }

        // Gom tất cả ghế trống
        List<int> freeSeats = new List<int>();
        for (int i = 0; i < LobbyInfo.numberParticipants; i++)
        {
            if (players[i] == PlayerRef.None)
                freeSeats.Add(i);
        }

        if (freeSeats.Count == 0)
        {
            Debug.LogError("[AssignSeat] No empty seat!");
            return -1;
        }

        // Random ghế trống
        int randomIndex = UnityEngine.Random.Range(0, freeSeats.Count);
        int seat = freeSeats[randomIndex];

        players.Set(seat, player);
        return seat;
    }

    // =========================
    // GAME START AND END
    // =========================

    public void GameStart()
    {
        if (!Runner.IsServer) return;

        Debug.Log("[GameManager] === GAME START ===");
        GameReady = true;

        stage = Stage.Startgame;
    }
    public void Host_OnPlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        string msg = $"Một người chơi đã rời phòng! Trở về sảnh...";

        // Gửi thông báo cho tất cả
        Rpc_ShowMessageForAll(msg);

        // Sau 2 giây → kết thúc room
        StartCoroutine(Host_ReturnToStartScene());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void Rpc_ShowMessageForAll(string message)
    {
        UIManager.Instance.ShowSystemMessage(UIManager.Label.Info, message, 2f);
    }

    IEnumerator Host_ReturnToStartScene()
    {
        yield return new WaitForSeconds(2f);

        // Tắt room → client tự disconnect
        Runner.Shutdown();
    }

    // =========================
    // PLAYER HELPERS
    // =========================

    public int PlayerCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < players.Length; i++)
                if (players[i] != PlayerRef.None)
                    count++;
            return count;
        }
    }

    public List<PlayerController> GetAllPlayers()
    {
        List<PlayerController> allPlayers = new List<PlayerController>();
        for (int i = 0; i < PlayerCount; i++)
        {
            PlayerController player = GetPlayerAtSeat(i);
            allPlayers.Add(player);
        }

        return allPlayers;
    }

    public PlayerController GetCurrentPlayer()
    {
        int seat = currentPlayer - 1;
        return GetPlayerAtSeat(seat);
    }


    public PlayerController GetPlayerAtSeat(int seatIndex)
    {
        // Seat không hợp lệ
        if (seatIndex < 0 || seatIndex >= LobbyInfo.numberParticipants)
            return null;

        // Ghế trống
        PlayerRef targetRef = players[seatIndex];
        if (targetRef == PlayerRef.None)
            return null;

        // LOGIC GIỐNG HỆT CODE CŨ:
        // tìm PlayerController local có InputAuthority trùng PlayerRef
        foreach (var pc in FindObjectsOfType<PlayerController>())
        {
            if (pc.Object != null &&
                pc.Object.InputAuthority == targetRef)
            {
                return pc;
            }
        }

        return null;
    }



    // =========================
    // NETWORK UPDATE
    // =========================

    public override void FixedUpdateNetwork()
    {
        if (!GameReady)
            return;

        if (Runner.IsServer)
        {
            DeckCount = deck != null ? deck.CountCards() : 0;
            HostRunGameLogic();
        }
    }

    // =========================
    // GAME LOOP (HOST ONLY)
    // =========================

    void HostRunGameLogic()
    {
        if (stage == Stage.Startgame)
        {
            if (MatchCount == 0)
            {
                for (int seat = 0; seat < PlayerCount; seat++)
                {
                    PlayerController pc = GetPlayerAtSeat(seat);
                    pc.Rpc_TurnOffStartGameCoverPanel();
                }
            }

            MatchCount += 1;

            Rpc_ShowMessageForAll("Match " + MatchCount.ToString());
            
            // Deal cards
            for (int seat = 0; seat < PlayerCount; seat++)
            {
                if (players[seat] == PlayerRef.None)
                    continue;

                PlayerController pc = GetPlayerAtSeat(seat);

                for (int i = 0; i < 7; i++)
                {
                    RefillDiscardToDeck();
                    int card = deck.DrawCard(Place.inHand);
                    pc.addCardToHand(card, Place.inDeck);
                    DeckDrawAnimation(card, Place.inHand);
                }
            }

            thisPlayer.Rpc_HostCallShakeDeck();

            stage = Stage.ChoosePath;
            currentPlayer = 0;
            startPhase = true;
        }

        else if (stage == Stage.ChoosePath)
        {
            if (startPhase)
            {
                if (DelayTicks(2))
                    return;

                // MillCard(100);
                // startPhase = false;
                // return;

                currentPlayer++;

                CountdownPhaseStart(5);

                PlayerController currentPC = GetCurrentPlayer();
                if (currentPC != null && currentPC.isSnaped)
                {
                    if (currentPlayer == PlayerCount)
                        currentPlayer = 0;

                    startPhase = true;
                    return;
                }

                for (int i = 0; i < SpreadCardIds.Length; i++)
                {
                    RefillDiscardToDeck();
                    int spreadCard = deck.DrawCard(Place.inSpread);
                    SpreadCardIds.Set(i, spreadCard);
                    CardPlaces.Set(spreadCard, Place.inSpread);
                    DeckDrawAnimation(spreadCard, Place.inSpread);
                }

                thisPlayer.Rpc_HostCallShakeDeck();

                startPhase = false;
            }

            CheckPhaseTimeout();

            if (pendingEnter)
            {
                pendingEnter = false;

                PhaseEndTick = 0;
                PlayerController currentPC = GetCurrentPlayer();
                currentPC.phaseCountdown = false;

                if (currentPlayer == PlayerCount)
                {
                    stage = Stage.Abandon;
                    currentPlayer = 0;
                }
                startPhase = true;
            }
        }

        else if (stage == Stage.Abandon)
        {
            if (startPhase)
            {
                if (DelayTicks(1))
                    return;

                currentPlayer++;

                CountdownPhaseStart(5);

                PlayerController currentPC = GetCurrentPlayer();
                if (currentPC != null && currentPC.isSnaped)
                {
                    currentPC.PlayerGains();

                    if (currentPlayer == PlayerCount)
                        currentPlayer = 0;

                    startPhase = true;
                    return;
                }

                currentPC?.Rpc_AbandonPhaseSetUp();
                startPhase = false;
            }

            CheckPhaseTimeout();

            if (pendingEnter)
            {
                pendingEnter = false;
                stage = Stage.Spread;
                startPhase = true;
            }
        }

        else if (stage == Stage.Spread)
        {
            if (startPhase)
            {
                // startPhase = false;
                // Text_EndGameNow();
                // return;
                
                if (DelayTicks(1))
                    return;

                CountdownPhaseStart(5);

                PlayerController currentPC = GetCurrentPlayer();
                currentPC?.ResetPlayerState();

                for (int i = 0; i < SpreadCardIds.Length; i++)
                {
                    RefillDiscardToDeck();
                    int spreadCard = deck.DrawCard(Place.inSpread);
                    SpreadCardIds.Set(i, spreadCard);
                    CardPlaces.Set(spreadCard, Place.inSpread);
                    DeckDrawAnimation(spreadCard, Place.inSpread);
                }

                thisPlayer.Rpc_HostCallShakeDeck();

                startPhase = false;
            }

            CheckPhaseTimeout();

            if (pendingEnter)
            {
                pendingEnter = false;
                stage = Stage.Decide;
                startPhase = true;
            }
        }

        else if (stage == Stage.Decide)
        {
            if (startPhase)
            {
                if (DelayTicks(1))
                    return;

                CountdownPhaseStart(5);

                PlayerController currentPC = GetCurrentPlayer();
                currentPC?.ResetPlayerState();
                startPhase = false;
            }

            CheckPhaseTimeout();
            
            if (pendingEnter)
            {
                pendingEnter = false;
                currentPlayer = (currentPlayer == PlayerCount) ? 0 : currentPlayer;
                stage = Stage.Abandon;
                startPhase = true;
            }
        }
    }

    // =========================
    // UTILITIES
    // =========================

    public void CheckEndGame()
    {
        List<PlayerController> allPlayers = GetAllPlayers();

        //Nếu toàn bộ người chơi chưa snap
        for (int i = 0; i < allPlayers.Count; i++)
        {
            //Chưa endgame
            if (!allPlayers[i].isSnaped)
            {
                pendingEnter = true;
                return;
            }
        }

        //---- !!!!!! -----
        //   -- TEMP --
        //---- !!!!!! -----
        MatchCount = 3;

        //Chuyển match hoặc end game
        if (MatchCount < 3)
        {
            for (int i = 0; i < allPlayers.Count; i++)
            {
                //reset player hand
                for (int handI = 0; handI < allPlayers[i].CardIds.Length; handI++)
                    allPlayers[i].CardIds.Set(handI, -1);

                //reset player call
                for (int callI = 0; callI < allPlayers[i].CardIds.Length; callI++)
                    allPlayers[i].CardIds.Set(callI, -1);

                allPlayers[i].lastCardID = -1;
                allPlayers[i].ResetPlayerState();

                //reset is snap
                allPlayers[i].isSnaped = false;

            }

            //reset spread
            for (int spreadI = 0; spreadI < SpreadCardIds.Length; spreadI++)
                SpreadCardIds.Set(spreadI, -1);

            //reset discard
            for (int discardI = 0; discardI < DiscardIds.Length; discardI++)
                DiscardIds.Set(discardI, -1);

            //reset card place
            for (int placeI = 0; placeI < CardPlaces.Length; placeI++)
                CardPlaces.Set(placeI, Place.inDeck);

            lastDiscard = -1;

            //reset deck
            deck = new DeckManager();
            deck.InitializeDeck();

            currentPlayer = 0;
            stage = Stage.Startgame;
        }
        //Khi đủ 3 trận
        else
        {
            allPlayers.Sort((a, b) => a.point.CompareTo(b.point));

            for (int i = PlayerCount - 1; i > 0; i--)
            {
                allPlayers[i].Rpc_DisplayResultGame(i);
            }

            currentPlayer = 0;
            stage = Stage.None;

            Rpc_ShowMessageForAll("Winner found. Room start to end.");

            StartCoroutine(Host_ReturnToStartScene());
        }
    }

    public void RefillDiscardToDeck()
    {
        if (deck.CountCards() > 0)
            return;

        for (int i = 0; i < DiscardIds.Length; i++)
        {
            if (DiscardIds[i] == -1)
                continue;

            AddCardToDeck(DiscardIds[i], Place.inDiscard);
            DiscardIds.Set(i, -1);
        }

        deck.Shuffle();
    }

    public void Discard(int cardID, Place from)
    {
        if (!Runner.IsServer) return;

        CardPlaces.Set(cardID, Place.inDiscard);

        bool isDiscarded = false;

        for (int i = 0; i < DiscardIds.Length; i++)
        {
            if (DiscardIds[i] == -1)
            {
                DiscardIds.Set(i, cardID);
                lastDiscard = cardID;
                isDiscarded = true;
                break;
            }
        }

        if (isDiscarded)
        {
            thisPlayer.Rpc_DrawCardVisual(cardID, from, Place.inDiscard);
        }
        else
            Debug.LogWarning("[Discard] Discard pile full");
    }

    public void AddCardToDeck(int cardID, Place from)
    {
        deck.AddCardToDeck(cardID);

        thisPlayer.Rpc_DrawCardVisual(cardID, from, Place.inDeck);
    }

    public void MillCard(int number)
    {
        if (!Runner.IsServer) return;

        if (number > DeckCount) number = DeckCount;

        for (int i = 0; i < number; i++)
        {
            Discard(deck.MillCard(), Place.inDeck);
        }
    }

    public void OnDiscardsChanged()
    {
        StartCoroutine(thisPlayer?.RenderDiscardZone());
    }

    public void OnSpreadChanged()
    {
        StartCoroutine(thisPlayer?.RenderAugurCard());
    }

    public bool DelayTicks(int ticks)
    {
        if (!DelayTick.IsRunning)
        {
            DelayTick = TickTimer.CreateFromTicks(Runner, ticks);
            return false;
        }

        if (!DelayTick.Expired(Runner))
            return false;

        DelayTick = TickTimer.None;
        return true;
    }

    public void DeckDrawAnimation(int cardID, Place to)
    {
        thisPlayer.Rpc_DrawCardVisual(cardID, Place.inDeck, to);
    }

    public void CountdownPhaseStart(int durationSeconds)
    {
        PhaseEndTick = Runner.Tick + Runner.TickRate * durationSeconds;
        PlayerController currentPlayer = GetCurrentPlayer();
        currentPlayer.phaseCountdown = true;
    }

    void CheckPhaseTimeout()
    {
        if (!Runner.IsServer) return;

        if (PhaseEndTick > 0 && Runner.Tick >= PhaseEndTick)
        {
            PhaseEndTick = 0;
            PlayerController currentPlayer = GetCurrentPlayer();
            currentPlayer.phaseCountdown = false;

            if (stage == Stage.Spread || stage == Stage.ChoosePath)
            {
                List<int> playerSpreadCardIds = new List<int>();
                foreach (Transform sc in currentPlayer.spreadArea)
                {
                    int cardID = sc.GetComponent<CardView>().thisCardID;
                    playerSpreadCardIds.Add(cardID);
                }

                int randomIndex = Random.Range(0, playerSpreadCardIds.Count);
                int chooseRandomCardID = playerSpreadCardIds[randomIndex];

                currentPlayer.SelectSpreadCard(chooseRandomCardID);
            }
            else if (stage == Stage.Abandon)
            {
                List<int> playerHandCardIds = new List<int>();
                for (int i = 0; i < currentPlayer.CardIds.Length; i++)
                {
                    if (currentPlayer.CardIds[i] == -1)
                        continue;
                    int cardID = currentPlayer.CardIds[i];
                    playerHandCardIds.Add(cardID);
                }

                int randomIndex = Random.Range(0, playerHandCardIds.Count);
                int chooseRandomCardID = playerHandCardIds[randomIndex];

                currentPlayer.DiscardInAbandonPhase(chooseRandomCardID);
            }

            currentPlayer.ResetPlayerState();
            pendingEnter = true;
        }
    }

    public int GetTimerCountdownTick()
    {
        int remain = (PhaseEndTick - Runner.Tick) / Runner.TickRate;

        remain = Mathf.Max(0, remain);

        return remain;
    }



    // =========================
    // Text game
    // =========================
    public void Text_EndGameNow()
    {
        List<PlayerController> allPlayers = GetAllPlayers();

        int virtualPoint = 0;
        foreach (var player in allPlayers)
        {
            virtualPoint += 100;
            player.PointIncreasing(virtualPoint);
            player.isSnaped = true;
        }

        CheckEndGame();
    }
    
}

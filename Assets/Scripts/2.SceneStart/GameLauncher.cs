using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;

public class GameLauncher : MonoBehaviour
{
    public NetworkRunner runnerPrefab;
    public TMP_InputField roomField;

    public Button QuickPlay;
    public Button CreateRoom;
    public Button JoinRoom;

    private bool isConnecting = false;
    const int MAX_RETRY = 3;
    const float RETRY_DELAY = 0.7f;


    private NetworkRunner runner;

    void Start()
    {
        QuickPlay.onClick.AddListener(OnQuickPlay);
        CreateRoom.onClick.AddListener(OnCreateRoom);
        JoinRoom.onClick.AddListener(OnJoinRoom);
    }

    // -----------------------------------
    // BUTTON: QUICK PLAY (random room)
    // -----------------------------------
    public void OnQuickPlay()
    {
        string room = Random.Range(100000, 999999).ToString();
        StartHost(room);
    }

    // -----------------------------------
    // BUTTON: CREATE ROOM
    // -----------------------------------
    public void OnCreateRoom()
    {
        string room = Random.Range(100000, 999999).ToString();
        StartHost(room);
    }

    // -----------------------------------
    // BUTTON: JOIN ROOM
    // -----------------------------------
    public void OnJoinRoom()
    {
        if (!JoinRoom.interactable)
            return;

        string room = roomField.text.Trim();
        if (string.IsNullOrEmpty(room)) return;

        JoinRoom.interactable = false;
        StartClient(room);
    }


    // -----------------------------------
    // HOST START
    // -----------------------------------
    async void StartHost(string room)
    {
        runner = Instantiate(runnerPrefab);
        runner.name = "Runner";
        DontDestroyOnLoad(runner.gameObject);

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = room,
            Scene = SceneRef.FromIndex(2)  // scene Lobby
        });
    }

    // -----------------------------------
    // CLIENT START
    // -----------------------------------
    async void StartClient(string room)
    {
        if (isConnecting)
            return;

        isConnecting = true;
        JoinRoom.interactable = false;

        int attempt = 0;

        while (attempt < MAX_RETRY)
        {
            attempt++;
            Debug.Log($"[Client] Join attempt {attempt}/{MAX_RETRY}");

            runner = Instantiate(runnerPrefab);
            runner.name = "Runner";
            DontDestroyOnLoad(runner.gameObject);

            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = room,
            });

            if (result.Ok)
            {
                Debug.Log("[Client] Join room success");
                return; // ✅ THÀNH CÔNG → thoát
            }

            Debug.LogWarning($"[Client] Join failed: {result.ShutdownReason}");

            // Dọn runner thất bại
            await runner.Shutdown();
            Destroy(runner.gameObject);
            runner = null;

            // ❌ Chỉ retry khi GameNotFound
            if (result.ShutdownReason != ShutdownReason.GameNotFound)
                break;

            // ⏱️ Delay trước retry
            await Task.Delay(Mathf.RoundToInt(RETRY_DELAY * 1000));
        }

        // ❌ Thất bại sau tất cả retry
        Debug.LogError("[Client] Join room failed after retries");

        isConnecting = false;
        JoinRoom.interactable = true;
    }


}

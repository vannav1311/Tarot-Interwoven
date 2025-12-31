using Fusion;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public static class LobbyInfo {
    public static int numberParticipants;
}

public class LobbyManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI playerCountText;
    public Button startGameButton;
    public Button exitRoomButton;
    public Button saveIcon;

    private NetworkRunner runner;
    public GameManager gameManagerPrefab;

    private void Awake()
    {
        // Chạy Initialize sau khi scene load để tìm runner từ scene trước
        StartCoroutine(Initialize());
    }

    private System.Collections.IEnumerator Initialize()
    {
        // Runner sinh từ scene trước thường mất 1–5 frame để tồn tại
        while (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>(); // Tìm cả trong DontDestroyOnLoad
            yield return null;
        }

        SetupUI();
    }

    // Chỉ chạy 1 lần sau khi runner đã sẵn sàng
    void SetupUI()
    {
        if (runner == null)
        {
            Debug.LogError("LobbyManager: Không tìm thấy NetworkRunner!");
            return;
        }

        // Host mới được dùng nút Start
        if (runner.IsServer)
        {
            startGameButton.onClick.AddListener(OnStartGame);
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }

        exitRoomButton.onClick.AddListener(OnExitRoom);
        saveIcon.onClick.AddListener(OnSaveCodeRoom);

        RefreshUI();
    }

    void Update()
    {
        if (runner != null && runner.SessionInfo != null)
            RefreshUI();
    }

    // ============================================================
    // REFRESH UI
    // ============================================================
    void RefreshUI()
    {
        if (runner == null || runner.SessionInfo == null)
            return;

        // 1. Mã phòng
        roomCodeText.text = "Mã phòng: " + runner.SessionInfo.Name;

        // 2. Số người
        int count = runner.ActivePlayers.Count();
        playerCountText.text = $"Người chơi: {count}/4";

        if (runner.IsServer)
            startGameButton.gameObject.SetActive(count >= 1 && count <= 4);
    }

    // ============================================================
    // START GAME (Host)
    // ============================================================
    async void OnStartGame()
    {
        if (!runner.IsServer)
            return;

        Debug.Log("Host bắt đầu game, chuyển sang Gameplay...");

        LobbyInfo.numberParticipants = runner.ActivePlayers.Count();

        await runner.LoadScene(SceneRef.FromIndex(3), LoadSceneMode.Single);

        runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
    }

    // ============================================================
    // THOÁT PHÒNG
    // ============================================================
    async void OnExitRoom()
    {
        Debug.Log("Đang thoát phòng...");

        if (runner != null)
        {
            await runner.Shutdown();
            Destroy(runner.gameObject);
        }

        SceneManager.LoadScene(1);
    }

    void OnSaveCodeRoom()
    {
        GUIUtility.systemCopyBuffer = runner.SessionInfo.Name.ToString();
    }
}

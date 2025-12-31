using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : NetworkRunnerCallbacks
{
    bool hasEnteredLobby = false;

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        // Chỉ xử lý khi đang Gameplay
        if (SceneManager.GetActiveScene().name != "4.Gameplay")
            return;

        GameManager.Instance?.Host_OnPlayerLeft(player);
    }

    public override void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"Runner shutdown: {reason}");

        // ❗ Nếu chưa vào lobby thì IGNORE
        if (!hasEnteredLobby)
        {
            Debug.Log("⛔ Shutdown during join → ignore");
            return;
        }

        SceneManager.LoadScene("2.StartScene");
    }


    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        if (SceneManager.GetActiveScene().name == "3.Lobby")
        {
            hasEnteredLobby = true;
            Debug.Log("✔ Entered Lobby scene");
        }
    }

}

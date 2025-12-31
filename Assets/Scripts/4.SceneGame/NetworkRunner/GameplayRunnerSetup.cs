using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class GameplayRunnerSetup : MonoBehaviour
{
    [Header("Prefabs & Positions")]
    
    public GameObject playerPrefab;                 // Network player prefab (NetworkObject)
    public Transform[] playerPositions;             // seats positions in scene (0..3)

    private List<NetworkObject> spawnedPlayers = new();
    private NetworkRunner runner;
    private bool spawned = false;
    
    float timeout = 120f;
    float t = 0f;

    private IEnumerator Start()
    {
        // 1) Wait until NetworkRunner exists (runner is created in previous scene and marked DontDestroy)
        while (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
            yield return null;
        }

        // 2) Wait until GameManager network instance has spawned (we rely on GameManager.Instance set in Spawned)
        while (GameManager.Instance == null || GameManager.Instance.Runner == null)
        {
            yield return null;
        }

        runner = FindObjectsOfType<NetworkRunner>()
                 .FirstOrDefault(r => r.IsRunning);


        // Now safe: GameManager exists and is initialized across host & client
        // Host should spawn players; client will just wait for replication and GameManager to mark GameReady
        if (runner.IsServer)
        {
            StartCoroutine(SpawnAfterOneTick());
        }
    }
    private IEnumerator SpawnAfterOneTick()
    {
        // Đợi đúng 1 tick Fusion cho an toàn
        yield return new WaitForFixedUpdate();

        if (spawned)
            yield break;

        spawned = true;

        Debug.Log("[GameplayRunnerSetup] SpawnExistingPlayers()");
        SpawnExistingPlayers(runner);
    }

    public void SpawnExistingPlayers(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        // Cache active players in join order
        var activeList = runner.ActivePlayers.ToList();
        Debug.Log($"[GameplayRunnerSetup] Active players count = {activeList.Count}");

        foreach (var p in activeList)
        {
            int seat = GameManager.Instance.AssignSeat(p);
            Vector3 pos = Vector3.zero;
            if (seat >= 0 && seat < playerPositions.Length)
                pos = playerPositions[seat].position;

            Debug.Log($"[GameplayRunnerSetup] Spawning player {p} at seat {seat}");

            var existingObj = runner.GetPlayerObject(p);
            if (existingObj != null)
            {
                Debug.Log($"[GameplayRunnerSetup] PlayerRef {p} already has PlayerObject, skipping spawn.");
                continue;
            }

            var playerObj = runner.Spawn(playerPrefab, pos, Quaternion.identity, p, (r, obj) =>
            {
                var pc = obj.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SeatIndex = seat;
                }
            });

            if (playerObj != null)
            {
                spawnedPlayers.Add(playerObj);

                // initialize NetworkArray or other defaults on host immediately if needed
                var pc = playerObj.GetBehaviour<PlayerController>();
                if (pc != null)
                {
                    for (int i = 0; i < pc.CardIds.Length; i++)
                    {
                        pc.CardIds.Set(i, -1);
                    }
                    for (int i = 0; i < pc.CallCardIds.Length; i++)
                    {
                        pc.CallCardIds.Set(i, -1);
                    }
                }

                Debug.Log($"[GameplayRunnerSetup] Spawned PlayerRef={p} seat={seat}");
            }
            else
            {
                Debug.LogError("[GameplayRunnerSetup] Spawn returned null for player prefab!");
            }
        }

        // Start delayed check to ensure spawned players are valid then build host-side list and start game
        StartCoroutine(DelayedBuildHost());
    }

    private IEnumerator DelayedBuildHost()
    {
        // Wait one FixedUpdate to allow Fusion to register spawned objects in simulation
        yield return new WaitForFixedUpdate();

        // Wait until GameManager and its Runner are available
        while (GameManager.Instance == null || GameManager.Instance.Runner == null)
            yield return null;

        // Wait until all spawned players are valid and have PlayerController attached
        foreach (var obj in spawnedPlayers)
        {
            NetworkObject netObj = obj;
            PlayerController pc = null;
            while (netObj == null || !netObj.IsValid || (pc = netObj.GetBehaviour<PlayerController>()) == null)
            {
                yield return null;
            }
        }

        // Ensure each player has SeatIndex assigned (DelayedBuild host ensures it)
        bool ready = false;
        while (!ready && t < timeout)
        {
            t += Time.deltaTime;
            ready = true;

            // 1. Đủ số người
            if (GameManager.Instance.PlayerCount < LobbyInfo.numberParticipants)
            {
                ready = false;
                yield return null;
                continue;
            }

            // 2. Check từng player
            for (int seat = 0; seat < LobbyInfo.numberParticipants; seat++)
            {
                var pc = GameManager.Instance.GetPlayerAtSeat(seat);
                if (pc == null) continue;

                if (pc.SeatIndex < 0)
                {
                    ready = false;
                    break;
                }

                if (pc.ReadyState != ClientReadyState.FullyReady)
                {
                    ready = false;
                    break;
                }
            }

            yield return null;
        }

        if (!ready)
        {
            Debug.LogError("[Host] Timeout waiting for players ready");
            yield break;
        }

        Debug.Log("[Host] All players ready → start game");
        GameManager.Instance.GameStart();
    }
}

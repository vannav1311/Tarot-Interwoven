using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class SceneReadyProxy : NetworkBehaviour
{
    private static HashSet<PlayerRef> readyPlayers = new();

    public override void Spawned()
    {
        // CLIENT báo host: tôi đã load xong Gameplay scene
        if (Object.HasInputAuthority)
        {
            Rpc_ClientSceneReady(Object.InputAuthority);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void Rpc_ClientSceneReady(PlayerRef player)
    {
        readyPlayers.Add(player);
        Debug.Log($"[SceneReadyProxy] Player {player} READY");
    }

    public static bool AreAllPlayersReady(int expected)
    {
        return readyPlayers.Count >= expected;
    }
}

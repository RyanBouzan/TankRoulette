using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class DespawnAfterX : NetworkBehaviour
{

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(Despawn(2.0f));
    }

    IEnumerator Despawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (base.IsServer)
            ServerDespawn();
        else
            Destroy(this.gameObject);
       
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerDespawn()
    {
        base.Despawn();
    }
}

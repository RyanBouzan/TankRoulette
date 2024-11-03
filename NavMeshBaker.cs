using FishNet.Object;
using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavMeshBaker : NetworkBehaviour
{
    public NavMeshPlus.Components.NavMeshSurface navMesh;
    public int counter = 0;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(!base.IsServer)
        {
            this.enabled = false;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }



    // Update is called once per frame
    void Update()
    {
        if(counter >= 100000)
        {
            Physics2D.SyncTransforms();
            Debug.LogWarning("baking some coog");
            navMesh.BuildNavMeshAsync();
            counter = 0;
            
        }
        counter++;
    }
}

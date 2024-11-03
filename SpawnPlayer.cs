using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System;

public class SpawnPlayer : NetworkBehaviour
{

    [SerializeField]
    private GameObject bulldogPrefab;

    [SerializeField]
    private Transform bulldogSpawnPosition;

    public GameObject bulldogClone;

    // Start is called before the first frame update
    void Start()
    {
        bulldogPrefab = Resources.Load("Prefabs/BULLDOG") as GameObject;
        bulldogSpawnPosition = GameObject.FindGameObjectWithTag("SPAWNPOS").transform;
    }

    public void OnServerInitialized()
    {
        ServerSpawnBulldog(bulldogPrefab, bulldogSpawnPosition, this);
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            ServerSpawnBulldog(bulldogPrefab, bulldogSpawnPosition, this);

        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void ServerSpawnBulldog(GameObject bulldogPrefab, Transform spawnPosition, SpawnPlayer script)
    {

        GameObject spawnedBulldog = Instantiate(bulldogPrefab, spawnPosition.transform.position, spawnPosition.transform.rotation);

        ServerManager.Spawn(spawnedBulldog);

        ObserverSpawnBulldog(bulldogPrefab, spawnPosition, script);

    }

    [ObserversRpc(ExcludeOwner = true)]
    private void ObserverSpawnBulldog(GameObject bulldogPrefab, Transform spawnPosition, SpawnPlayer script)
    {
        GameObject spawnedBulldog = Instantiate(bulldogPrefab, spawnPosition.transform.position, spawnPosition.transform.rotation);
        script.bulldogClone = spawnedBulldog;
    }


}

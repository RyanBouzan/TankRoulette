using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    public static PlayerSpawnPoint Instance;
    public Transform[] _spawns = new Transform[4];
    private void Awake()
    {
        Instance = this;
        Instance._spawns[0] = transform.GetChild(0);
        Instance._spawns[1] = transform.GetChild(1);
        Instance._spawns[2] = transform.GetChild(2);
        Instance._spawns[3] = transform.GetChild(3);
    }

   
}

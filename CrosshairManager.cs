using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

namespace TankRoulette
{
public class CrosshairManager : NetworkBehaviour
{

    // Update is called once per frame
    void Update()
    {
        transform.position = Input.mousePosition;
    }

void Awake()
{
    BootstrapNetworkManager.OnGameStart += OnGameStart;
}

void OnDestroy()
{
    BootstrapNetworkManager.OnGameStart -= OnGameStart;
}

void OnGameStart()
{
    if(base.IsOwner)
    {
       transform.SetParent(GameObject.FindGameObjectWithTag("LocalClient").transform.GetChild(0));
       GetComponent<Image>().enabled = true;
       transform.GetChild(0).GetComponent<Image>().enabled = true;
    }
    else
        Destroy(this.gameObject);
}
}
}
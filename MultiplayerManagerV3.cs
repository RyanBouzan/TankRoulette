using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


//acts as a bridge between fishnet's client manager/NetworkManager object
//and our game manager object. stores client and tank objects in public fields for
//other scripts to read and access, for example, the enemy tank class to check for players.
namespace TankRoulette
{
    public class MultiplayerManagerV3 : NetworkBehaviour
    {
        [SerializeField]
        private GameRoundManager GRM;

        [SyncVar]
        public List<PlayerObject> players = new();



        [SyncVar]
        public GameObject Player1Tank;
        [SyncVar]
        public GameObject Player1ClientOBJ;
        [SyncVar]
        public ClientTankManager Player1Client;

        [SyncVar]
        public GameObject Player2Tank;
        [SyncVar]
        public GameObject Player2ClientOBJ;
        [SyncVar]
        public ClientTankManager Player2Client;

        public int idCounter = -1;

        [SyncVar]
        public List<GameObject> allPlayerTanks = new();

        [SyncVar]
        public List<GameObject> allPlayerClients = new();

        [SerializeField]
        private bool checkForDeadPlayers;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (base.IsClientOnly)
            {
                GameObject.Find("NavMesh").SetActive(false);
            }

        }

        private void Start()
        {
            GRM = GetComponent<GameRoundManager>();

        }

        //responsible for assigning new conns to the appropriate fields
        [ServerRpc(RequireOwnership = false, RunLocally = true)]
        public void ServerAssign(NetworkConnection conn, GameObject client, GameObject tank, GameObject cam)
        {
            Assign(conn, client, tank, cam);
        }

        [ObserversRpc(RunLocally = true)]
        private void Assign(NetworkConnection conn, GameObject client, GameObject tank, GameObject cam)
        {

        }

    }
}
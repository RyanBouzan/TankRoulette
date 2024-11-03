using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TankRoulette
{
    public class ClientTankManager : NetworkBehaviour
    {

        [SyncVar]
        public bool tankIsActive = false;

        [Header("Assign In Inspector")]

        [SerializeField]
        private GameObject canvas;

         public ShopUI shop;

        public GameObject shopPanel;

        [SerializeField]
        private GameObject ProgressBar;


        public GameObject activeHUD;

        public GameObject RepairKitHUD;

        public GameObject respawnPanel;

        public Image DamageScreen;

        [SerializeField]
        private bool developmentMode;


        [Header("Assigned At Runtime OK if blank")]
        [SyncVar]
        public GameObject activeTank;

        public PlayerFireController pfc;

        public BootstrapManager bootstrap;
        [SerializeField]
        private GameObject playerCameraController;
       

        private bool beenInitialized = false;
        public int clientTankNum = 0;

        public PlayerObject playerObj;

        private void Start()
        {
            BootstrapNetworkManager.OnGameStart += OnGameStart;
            //Debug.LogWarning("subscribed");
            if(developmentMode)
            {
                activeHUD.SetActive(false);
                shop.shopPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            BootstrapNetworkManager.OnGameStart -= OnGameStart;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            //ShopPanel.SetActive(true);

            if (!base.IsOwner)
            {
                canvas.SetActive(false);
                respawnPanel.SetActive(false);
                activeHUD.SetActive(false);
                this.enabled = false;
            }

        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (base.IsServerOnly)
            {
                canvas.SetActive(false);
            }

        }

        [ServerRpc(RunLocally = true)]
        public void Respawn(ClientTankManager CTM)
        {

            if (CTM.tankIsActive && !beenInitialized)
                return;



            Debug.LogWarning("triggered respawn for Client ID: " + this.OwnerId);
            PlayerDurability pd = CTM.activeTank.GetComponent<PlayerDurability>();
            PlayerFireController pf = CTM.activeTank.GetComponent<PlayerFireController>();

            CTM.activeTank.transform.SetPositionAndRotation
                (Vector2.zero, Quaternion.identity);

            pd._Integrity = pd.defaultIntegrity;
            pd.UpdateEngine(pd, 1f);
            CTM.DamageScreen.gameObject.SetActive(false);
            pd.ERA_Front = pd.defaultERA;
            pd.ERA_Left = pd.defaultERA;
            pd.ERA_Right = pd.defaultERA;
            pd.ERA_Rear = pd.defaultERA - pd.defaultERA / 2;
            if (pd.trophyAvailible)
                pd.ServerUpdateTrophy(pd, true);

            pf.magazineCount = pf.maxMagazineCount;
            pf.reserveCount = 500;
            pf.fireState = PlayerFireController.FireState.Ready;
            CTM.activeTank.SetActive(true);
            CTM.respawnPanel.SetActive(false);
            CTM.tankIsActive = true;
            CTM.activeHUD.SetActive(true);

        }



        //subscribed event from bootstrap network manager, called immediately after game starts
        private void OnGameStart()
        {
            ProgressBar.SetActive(true);
            activeHUD.SetActive(true);
            RepairKitHUD.SetActive(true);
            beenInitialized = true;
        }


    }
}

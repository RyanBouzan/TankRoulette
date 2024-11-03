using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Unity.VisualScripting;
using FishNet.Object;

namespace TankRoulette
{

    public class ShopManager : NetworkBehaviour
    {

        public GameObject shopPanel;
        public ClientTankManager CTM;
        public ShopItem[] itemList = new ShopItem[4];

        public List<Shop> shopList = new List<Shop>();


        //is the player in the shop?
        //will be used by the UI script to display the shop
        public bool inShop;


        private void Awake()
        {


            itemList[0] = new ShopItem("AP", 20000);
            itemList[1] = new ShopItem("CAMO", 20000);
            itemList[2] = new ShopItem("ARMOR", 20000);
            itemList[3] = new ShopItem("MAG", 20000);

            //    ShopTextbox = ShopTextboxObject.GetComponent<TextMeshProUGUI>();
            //    NotifTextbox = NotifTextboxObject.GetComponent<TextMeshProUGUI>();

        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (base.IsOwner)
            {
                this.enabled = false;
            }
        }

        private void InitializeShopPanel()
        {
            try
            {
                //should be Client -> Canvas -> Shop -> ShopPanel (gameobject).
                GameObject localclient = LocalConnection.FirstObject.gameObject;
                //Debug.Log("Getting shopPanel...");
                //Debug.Log("looking in: " + localclient);

                try
                {
                    CTM = localclient.GetComponent<ClientTankManager>();
                    shopPanel = CTM.shopPanel;
                    
                    //  Debug.LogWarning("got shop panel: " + shopPanel);
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.Log("transform was out of bounds, according to unity. probably means the client didnt join yet");
                }
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning("shop panel null exception");
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (shopPanel == null)
            {
                //Debug.LogWarning("ShopPanel was null, attempting to initialize");
                InitializeShopPanel();
            }

            inShop = checkAllShops();

            if (shopPanel != null)
            {
                if (inShop)
                {
                    Cursor.visible = true;
                    shopPanel.SetActive(true);
                }
                else
                {
                    Cursor.visible = false;
                    shopPanel.SetActive(false);
                }
            }

        }

        //assuming shops do not overlap in game, the player can only be in one shop at a time.
        //this also forces all shops to open the same item set. this may be a problem in the future.

        private bool checkAllShops()
        {

            foreach (Shop shop in shopList)
            {
                bool temp = shop.playerInShop;
                if (temp)
                    return true;
            }


            return false;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FishNet.Object;
using System;
using FishNet.Connection;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using FishNet.Object.Synchronizing;


namespace TankRoulette
{
    //script responsible for managing the client's shop. 
    //For now, gets references to the player movement controller and fire controller to modify damage, health, etc.
    //for how this is done, see the OnSpawn method in ClientTankManager class
    public class ShopUI : NetworkBehaviour
    {
        public GameObject shopPanel;
        public GameObject playerTank;
        public PlayerFireController playerFireCont;
        public PlayerDurability playerDurability;
        public CameraController cameraController;
        [SerializeField]
        private TextMeshProUGUI ItemDisplayTitle;
        [SerializeField]
        private TextMeshProUGUI ItemDisplayText;
        [SerializeField]
        private GameObject PurchaseTextPrefab;
        //currently UNASSIGNED!
        public Light2D playerScoutVision;
        public Text hudText;
        public int coin;
        public bool bounty = false;
        public int tempBountyAmount;
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!base.IsOwner)
            {
                //transform.parent.gameObject.SetActive(false);
                this.enabled = false;
            }



            //LocalConnection.Objects holds all the gameobjects belonging to a connection.
            //Currently, we have 1: Client, 2: Tank 3: Camera



            // playerFireCont = playerTank.GetComponent<PlayerFireController>();
            // playerDurability = playerTank.GetComponent<PlayerDurability>();
            //// Debug.LogError(LocalConnection.Objects.ElementAt(1).name);
            // playerScoutVision = playerTank.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Light2D>();


        }


        private void FixedUpdate()
        {

            //constantly runs to check if there is a bounty that needs to be added
            if (bounty)
            {
                bounty = false;
                AddBountyServer(this, tempBountyAmount);
            }

            // string integrityText = "Integrity: " + playerDurability._Integrity.ToString("F1") + "\n";
            // string coinText = "Coin :" + coin + "\n";
            // string fireText = playerFireCont.fireState.ToString() + "\n";
            // string ammoTextLeft;
            // if (playerFireCont.fireState == PlayerFireController.FireState.Reloading)
            // {
            //     ammoTextLeft = "...";
            // }
            // else
            // {
            //     ammoTextLeft = playerFireCont.magazineCount.ToString();
            // }
            // string ammoTextRight = " / " + playerFireCont.maxMagazineCount + "\n";
            // string damageText = "damage: " + playerFireCont.damage.ToString() + "\n";

            // hudText.text = integrityText + coinText + fireText + ammoTextLeft + ammoTextRight + damageText;

        }

        public void AddBounty()
        {
            // Debug.LogError("weoh coin before: " + coin);
            bounty = true;
            //AddBountyServer(this, amount);

            // Debug.LogError("weoh coin after: " + coin);
        }

        [ServerRpc(RequireOwnership = false, RunLocally = true)]
        private void AddBountyServer(ShopUI shopRef, int amount)
        {
            shopRef.coin += amount;
        }
        public void Hover_Item_Exit()
        {
            ItemDisplayTitle.text = "Select an item";
            ItemDisplayText.text = "Items can be used to upgrade or add new features to your tank.";
        }

        public void Hover_Item_AP()
        {
            ItemDisplayTitle.text = "Armor Piercing Rounds";
            ItemDisplayText.text = "Superior armor piercing rounds. \nFeatures increased penetration and damage due to stronger materials and higher velocity.";
        }

        public void Hover_Item_Mag()
        {
            ItemDisplayTitle.text = "Increase Magazine Size";
            ItemDisplayText.text = "Larger size of internal magazine for more firepower down range.";
        }

        public void Hover_Item_Armor()
        {
            ItemDisplayTitle.text = "Enhanced Armor";
            ItemDisplayText.text = "Increases integrity of armor through superior materials.";
        }

        public void Hover_Item_Binos()
        {
            ItemDisplayTitle.text = "High Grade Optics";
            ItemDisplayText.text = "Repair and upgrade observatory devices to clearly identify targets and terrain.";
        }

        //apply this and other similar functions to buttons in the UI.
        //unfortunately, making coin a syncvar would be quite difficult. therefore,
        //all of the logic to purchase items happens on the client side.
        //the server is not currently aware of the client's coin
        public void PurchaseAP()
        {
            if (coin >= 40)
            {
            StartCoroutine(FadeOutPurchaseText("Purchased Ammo"));

                coin -= 40;
                playerFireCont.UpdateTankDamage(playerFireCont, 5f);
                Debug.LogWarning("bought ap");

            }
            else
            {
                Debug.LogWarning("you are lacking coin");
            }
        }

        private IEnumerator FadeOutPurchaseText(string text)
            {
                GameObject purchased = Instantiate(PurchaseTextPrefab, transform);
            purchased.GetComponent<RectTransform>().position = Input.mousePosition;
            purchased.GetComponent<TextMeshProUGUI>().text = text;
                float duration = 2f;
                float elapsedTime = 0f;
                Vector3 startPosition = purchased.transform.position + new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0f); // Add small randomness to start position
                Vector3 endPosition = startPosition + Vector3.up * 100f; // Move up by 50 units
                TextMeshProUGUI tmpro = purchased.GetComponent<TextMeshProUGUI>();
                Color startColor = tmpro.color;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / duration;

                    // Move up
                    purchased.transform.position = Vector3.Lerp(startPosition, endPosition, t);

                    // Fade out
                    tmpro.color = new Color(startColor.r, startColor.g, startColor.b, 1 - t);

                    yield return null;
                }

                Destroy(purchased);
            }
        public void PurchaseArmor()
        {
            StartCoroutine(FadeOutPurchaseText("Purchased Armor"));
            

            // Coroutine to fade out, move up, and destroy the purchase text
            
            if (coin >= 5)
            {
                coin -= 5;
                Debug.LogWarning("bought armor");

                //since the tank's integrity and ERA are syncvars, only the server can modify the values.
                //any updates will automatically be sent to the client
                playerDurability.UpdateTankIntegrity(playerDurability, 5f);

            }
            else
            {
                Debug.LogWarning("you are lacking coin");
            }
        }

        //run on server and client because of runlocally
        //TODO: implement these
       
        public void PurchaseMag()
        {
            StartCoroutine(FadeOutPurchaseText("Purchased Magazine"));

            if (coin >= 20)
            {
                Debug.LogWarning("bought mag");
                playerFireCont.UpdateTankMag(playerFireCont);
                coin -= 20;
            }
            else
            {
                Debug.LogError("you are lacking coin");
            }
        }

        public void PurchaseBINOS()
        {

            if (coin >= 50)
            {
            StartCoroutine(FadeOutPurchaseText("Purchased Optics"));

                coin -= 50;
                cameraController.UpdateBinos(cameraController, cameraController.visionTaperAmount - 0.01f);
            }
            else
            {
                Debug.LogError("you are lacking coin");
            }
            Debug.LogWarning("bought BINOS");

        }

    }
}
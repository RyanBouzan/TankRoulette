using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace TankRoulette
{
   
    public class PlayerDurability : NetworkBehaviour
    {
        
        [SerializeField]
        private PlayerAudioController pac;


        public GameObject nameTag;

        protected enum ArmorPlate
        {
            Front = 1,
            Left,
            Right,
            Rear
        }
        [SyncVar]
        public int _RepairKitCount;

        private bool repairing;

        [Header("Armor/ERA Segments")]

        public float defaultIntegrity;

        public float defaultERA;

        public int defaultTrophy;

        [Header("Trophy Components")]

        public bool trophyAvailible;
        public bool trophyEnabled;

        [SerializeField]
        private CircleCollider2D trophyCollider;

        [SyncVar]
        public int trophyCharges;

        //stored as independent variables instead of array because of Synchronization Var

        [SyncVar]
        public float ERA_Front;

        [SyncVar]
        public float ERA_Left;

        [SyncVar]
        public float ERA_Right;

        [SyncVar]
        public float ERA_Rear;

        //represents health
        [SyncVar]
        public float _Integrity;





        //assigned in inspector
        [SerializeField]
        private BoxCollider2D FrontPlate, SideLeft, SideRight, RearPlate;

        [SyncVar(OnChange = nameof(on_damage))]
        public bool showDamage;

        //reference to client and server tank managers
        public ClientTankManager CTM;
        public MultiplayerManagerV3 MM3;
        [SerializeField]
        private GameObject explosionPrefab;

        [SerializeField]
        private GameObject RepairKitImage;

        [SerializeField]
        private Text RepairKitText;

        private void Awake()
        {
            UnityEngine.Random.InitState(115);
        }

        

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner)
            {
                this.enabled = false;
            }
            CTM = LocalConnection.FirstObject.GetComponent<ClientTankManager>();

            if (base.IsServer)
            {
                ERA_Front = defaultERA;
                ERA_Left = defaultERA;
                ERA_Right = defaultERA;
                ERA_Rear = defaultERA - defaultERA / 2;
            }
            trophyEnabled = trophyAvailible;
            RepairKitImage = CTM.RepairKitHUD.transform.GetChild(0).gameObject;

            RepairKitText = CTM.RepairKitHUD.transform.GetChild(1).GetComponent<Text>();
            RepairKitText.text = _RepairKitCount.ToString();
        }

        [ServerRpc(RunLocally = true)]
        public void UpdateTankIntegrity(PlayerDurability pd, float amount)
        {
            pd._Integrity += amount;
        }

        //uses syncvar onchange functionality to call this method to show the damage screen
        //for the correct client
        private void on_damage(bool oldValue, bool newValue, bool asServer)
        {
            if (!base.IsOwner)
                return;

            if (!newValue)
            {
                // Debug.LogError("damage screen here");
                // Debug.LogError("showing damage screen using CTM: " + CTM.name);

                CTM.DamageScreen.gameObject.SetActive(true);

                StartCoroutine(FadeOutAndDisable(CTM.DamageScreen));


            }
        }


        // Update is called once per frame
        void Update()
        {
            if (_Integrity <= 0)
            {
                ServerDespawnTank(LocalConnection, explosionPrefab);
            }
            if (trophyAvailible)
            {

                if (Input.GetKeyDown(KeyCode.F))
                {
                    trophyEnabled = !trophyEnabled;
                    trophyCollider.enabled = trophyEnabled;
                }
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                UpdateEngine(this, 0.1f);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                //cleaner guarded clauses technique
                if (_RepairKitCount <= 0 || repairing)
                {
                    Debug.Log("player is out of repair kits or is currently repairing!");
                }
                else if (GetComponent<Rigidbody2D>().velocity.magnitude > 2)
                {
                    Debug.Log("Player's tank is moving, cannot repair. implement a UI message here.");
                }
                else if (GetComponent<TankMotor>().engine > 0.95f)
                {
                    Debug.Log("Player's engine is full durability, ignoring");
                }
                else
                {
                    repairing = true;
                    GetComponent<TankMotor>().repairing = true;
                    RepairKitImage.GetComponent<Animator>().enabled = true;
                    UseRepairKit(base.LocalConnection, this);
                }
            }

        }
        [TargetRpc]
        private void UseRepairKitTarget(NetworkConnection conn, PlayerDurability pd)
        {
            pd.GetComponent<TankMotor>().engine = 0f;
            pd._RepairKitCount--;
            pd.RepairKitText.text = _RepairKitCount.ToString();
            Debug.Log("implement flashing repair kit symbol here");
            StartCoroutine(RepairWithDelay(5));
        }
        [ServerRpc]
        private void UseRepairKit(NetworkConnection conn, PlayerDurability pd)
        {
            UseRepairKitTarget(conn, pd);
        }

        private IEnumerator RepairWithDelay(int time)
        {
            while (time > 0)
            {
                Debug.Log("(replace with UI): repairing! time left: " + time + " seconds.");
                yield return new WaitForSeconds(1f);
                time--;
            }
            Debug.Log("repair finished, implement UI.");
            UpdateEngine(this, 1f);
            repairing = false;
            GetComponent<TankMotor>().repairing = false;
            RepairKitImage.GetComponent<Animator>().enabled = false;
            RepairKitImage.GetComponent<Image>().color = Color.white;


        }
        //multipurpose method to reset the engine durability, and also to damage it
        [ServerRpc(RequireOwnership = true, RunLocally = true)]
        public void UpdateEngine(PlayerDurability pd, float amount, int randomChance = -1)
        {

            if (randomChance == -1 || randomChance == 1)
            {
                pd.GetComponent<TankMotor>().engine = amount;
                UpdateEngineOB(pd, amount);
            }
            else
            {
                int rand = UnityEngine.Random.Range(0, randomChance);
                if (rand == 0)
                {
                    pd.GetComponent<TankMotor>().engine = amount;
                    UpdateEngineOB(pd, amount);
                }
            }


            Debug.Log("updating engine server");

        }

        [ObserversRpc(ExcludeServer = true)]
        private void UpdateEngineOB(PlayerDurability pd, float amount)
        {
            pd.GetComponent<TankMotor>().engine = amount;
        }




        public void ProjectileImpact(PlayerDurability pd, Collider2D hitPlate, float damage)
        {
            if (pd.IsServer)
            {
                //excellent use of server resources right here RIGHT HERE
                pd.showDamage = true;
                pd.showDamage = false;
            }
            else
            {
                ShowDamageScreen();
            }
            if (trophyCollider.enabled && trophyAvailible)
            {
                if ((hitPlate as CircleCollider2D).Equals(trophyCollider))
                {
                    ServerUpdateTrophy(this, false);
                }
            }
            else
            {
                Debug.LogError("called projectile impact");
                pac.PlayClip("projectile_impact");
                Debug.LogWarning("incoming damage constant is: " + damage);
                //front
                if ((hitPlate as BoxCollider2D).Equals(FrontPlate))
                {
                    if (ERA_Front >= damage)
                    {
                        ServerUpdateERA(this, ArmorPlate.Front , damage);
                        damage = 0;
                    }
                    else if (ERA_Front > 0)
                    {
                        damage -= ERA_Front;
                        ServerUpdateERA(this, ArmorPlate.Front, 10000);

                    }

                    Debug.LogError("took: " + damage * 0.2f + " damage");
                    ServerUpdateIntegrity(this, damage * 0.2f);
                }
                //side left
                else if ((hitPlate as BoxCollider2D).Equals(SideLeft))
                {
                    if (ERA_Left >= damage)
                    {
                        ServerUpdateERA(this, ArmorPlate.Left, damage);
                        damage = 0;
                    }
                    else if (ERA_Left > 0)
                    {
                        damage -= ERA_Left;
                        ServerUpdateERA(this, ArmorPlate.Left, 10000);

                    }

                    if (damage != 0)
                    {
                        UpdateEngine(pd, GetComponent<TankMotor>().engine - 0.1f, 1);
                    }

                    //                Debug.LogError("took: " + damage * 0.5f + " damage");
                    ServerUpdateIntegrity(this, damage * 0.5f);

                }
                else if ((hitPlate as BoxCollider2D).Equals(SideRight))
                {
                    if (ERA_Right >= damage)
                    {
                        ServerUpdateERA(this, ArmorPlate.Right, damage);
                        damage = 0;
                    }
                    else if (ERA_Right > 0)
                    {
                        damage -= ERA_Right;
                        ServerUpdateERA(this, ArmorPlate.Right, 10000);

                    }

                    if (damage != 0)
                    {

                        UpdateEngine(pd, GetComponent<TankMotor>().engine - 0.1f, 1);

                    }

                    //     Debug.LogError("took: " + damage * 0.5f + " damage");
                    ServerUpdateIntegrity(this, damage * 0.5f);

                }
                else if ((hitPlate as BoxCollider2D).Equals(RearPlate))
                {
                    if (ERA_Rear >= damage)
                    {
                        ServerUpdateERA(this, ArmorPlate.Rear, damage);
                        damage = 0;
                    }
                    else if (ERA_Rear > 0)
                    {
                        damage -= ERA_Rear;
                        ServerUpdateERA(this, ArmorPlate.Rear, 10000);

                    }

                    if (damage != 0)
                    {
                        UpdateEngine(pd, GetComponent<TankMotor>().engine - 0.3f, 1);
                    }

                    //      Debug.LogError("took: " + damage * 1f + " damage");
                    ServerUpdateIntegrity(this, damage * 1f);
                }
                else
                {
                    Debug.LogError("this statement should not have printed");
                }
            }
        }

        [ServerRpc]
        private void ShowDamageScreen()
        {
            base.GetComponent<PlayerDurability>().showDamage = true;
        }

        [ServerRpc(RunLocally = true)]
        public void ServerUpdateTrophy(PlayerDurability pd, bool reset)
        {
            if (reset)
            {
                pd.trophyCharges = pd.defaultTrophy;
                pd.trophyCollider.enabled = true;
            }
            else
            {
                pd.trophyCharges--;

                if (pd.trophyCharges <= 0)
                {
                    pd.trophyCollider.enabled = false;
                    pd.trophyAvailible = false;
                    pd.trophyEnabled = false;
                }
            }
        }

        //updates the syncvar integrity
        [ServerRpc(RunLocally = true)]
        private void ServerUpdateIntegrity(PlayerDurability pd, float damage)
        {
            pd._Integrity -= damage;
        }

        //updates ERA using a switch case because there are 4 of them
        [ServerRpc(RunLocally = true)]
        private void ServerUpdateERA(PlayerDurability pd, ArmorPlate plate, float damage)
        {
            switch (plate)
            {
                case ArmorPlate.Front:
                    pd.ERA_Front -= damage;
                    break;
                case ArmorPlate.Left:
                    pd.ERA_Left -= damage;
                    break;
                case ArmorPlate.Right:
                    pd.ERA_Right -= damage;
                    break;
                case ArmorPlate.Rear:
                    pd.ERA_Rear -= damage;
                    break;
                default:
                    break;
            }



        }

        private IEnumerator FadeOutAndDisable(Image damageScreen)
        {
            // Get the initial color of the object
            Color startColor = damageScreen.color;
            float elapsedTime = 0f;

            while (elapsedTime < 1f)
            {
                // Calculate the new color with alpha fading out
                float alpha = 1 - (elapsedTime / 1f);
                Color newColor = new Color(startColor.r, startColor.g, startColor.b, alpha);

                // Apply the new color to the object's material
                damageScreen.color = newColor;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // After fading, set the alpha to 0
            // Color finalColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            //damageScreen.material.color = finalColor;

            // Disable the object
            damageScreen.gameObject.SetActive(false);

        }




        //it is an IMB move to destroy and resapwn the tank. instead, disable the controller, sprite and other
        //jackson

        //previously i made the attempt to send the script as a reference to the server.
        //according to fishnet, this is IMB. it doesnt get jackson and its null.
        //Instead, pass the connection to the method, then use the knowledge that the client is the first object
        //in the connection. from there, anything is possib.
        [ServerRpc(RequireOwnership = false)]
        private void ServerDespawnTank(NetworkConnection conn, GameObject explosion)
        {
            //get the client from the connection. the conn.Objects will have 1: Client, 2: Tank
            ClientTankManager CTM = conn.FirstObject.GetComponent<ClientTankManager>();


            //should have play on awake sound effect
            GameObject deathExplosion = Instantiate(explosion, transform.position, transform.rotation);
            ServerManager.Spawn(deathExplosion);

            TargetDespawnTank(conn);

            // CTM.tankIsActive = false;
            // CTM.activeTank.SetActive(false);
            // CTM.activeHUD.SetActive(false);

            //repeat on client connection only

            //ServerManager.Despawn(player);
            // MM3 = GameObject.FindGameObjectWithTag("GameController").GetComponent<MultiplayerManagerV3>();
            // MM3.allPlayerTanks.Remove(CTM.activeTank);
        }

        [TargetRpc]
        private void TargetDespawnTank(NetworkConnection conn)
        {
            //grab CTM again
            ClientTankManager CTM = conn.FirstObject.GetComponent<ClientTankManager>();

            CTM.tankIsActive = false;
            CTM.activeTank.SetActive(false);
            CTM.respawnPanel.SetActive(true);
        }


    }
}
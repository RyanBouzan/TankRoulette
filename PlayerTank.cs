using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System;
using UnityEngine;


namespace TankRoulette
{
    public class PlayerTank : Tank
    {
        public GameObject GameController;


        [SyncVar]
        public int health;

        public Transform turretBasket;
        public Transform turretCenter;
        public float desiredAngle;

        public float turretRotationSpeed;
        public float turretDesiredAngle;

        //[SerializeField]
        //  private float _moveSpeed = 50f;
        //    private float _velocity;

        //no need for syncvar, since we can pass these values as arguments to a serverRPC instead
        //element 0 = AP, 1 = CAMO, 2 = ARMOR, 3 = MAGAZINE
        public bool[] purchasedItems = new bool[4];



        //points used to PURGE LOZEN. local variable only, can be changed to implement point sharing system
        public int points;

        [Header("Stats")]
        public int kills;

        [SyncVar]
        public int shotsRemaining;

        public ShopManager shop;
        //public int tankHealth;







        // Start is called before the first frame update
        void Start()
        {
            // PlayerTankObject = Resources.Load("Prefabs/PlayerTank") as GameObject;
            GameController = GameObject.FindGameObjectWithTag("GameController");

        }



        public override void OnStartClient()
        {


            base.OnStartClient();
            if (!IsOwner)
            {
                this.enabled = false;
            }
            //turretBasket = transform.GetChild(0).transform.GetChild(0);

            //shop = GameController.GetComponent<ShopManager>();





        }



        /*

            [ServerRpc(RequireOwnership = false)]
            private void TryBuyShopItem(PlayerTank player, int itemIndex)
            {
                //check if the guy who called this method has enough points to buy the item
                if (player.points > player.shop.itemList[itemIndex].getprice())
                {
                    //since the points is a syncvar, it has to be updated here
                    player.points -= player.shop.itemList[itemIndex].getprice();
                    player.purchasedItems[itemIndex] = true;
                }

                //if it cant purge then do shiterbag
            }

            */

        [ServerRpc(RequireOwnership = false)]
        private void TODAYSTHEDAY(PlayerTank playerTank)
        {
            ServerManager.Despawn(playerTank.gameObject);
        }



        void Update()
        {

            if (!base.IsOwner)
                return;

            if (health <= 0)
            {
                TODAYSTHEDAY(this);
            }

            //camera will follow local tank
            Camera.main.transform.position = new Vector3(turretBasket.position.x, turretBasket.position.y, -7.5f);

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                GivePoints(50000);
            }

            //  RotateTankTurret();

            //        RotateTank(desiredAngle);

        }

        private void RotateTankTurret()
        {
            if (turretCenter == null)
            {
                turretCenter = transform.GetChild(0).GetChild(0);
            }
            else
            {
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = Camera.main.nearClipPlane;
                Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

                Vector2 targetDirection = (Vector3)mouseWorldPosition - turretCenter.transform.position;

                turretDesiredAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                float speed = turretRotationSpeed / 10 * Time.deltaTime;
                turretCenter.transform.rotation = Quaternion.RotateTowards(turretCenter.transform.rotation, Quaternion.Euler(0, 0, turretDesiredAngle - 90), speed);

                if (!base.IsHost)
                {
                    //    Debug.Log(turretDesiredAngle);
                    RotateTankTurretServer(turretCenter.gameObject, turretDesiredAngle, speed);
                }

                if (base.IsHost)
                {
                    RotateTankTurretObserver(turretCenter.gameObject, turretDesiredAngle, speed);
                }
            }
        }

        [ObserversRpc(ExcludeOwner = true, RunLocally = false)]
        private void RotateTankTurretObserver(GameObject turretCenter, float turretDesiredAngle, float speed)
        {
            //  Debug.LogWarning("recieved on client, angle is: " + turretDesiredAngle);
            turretCenter.transform.rotation = Quaternion.RotateTowards(turretCenter.transform.rotation, Quaternion.Euler(0, 0, turretDesiredAngle - 90), speed);

        }

        [ServerRpc(RequireOwnership = false)]
        private void RotateTankTurretServer(GameObject turretCenter, float turretDesiredAngle, float turretRotationSpeed)
        {

            //  Debug.LogWarning("weoh");

            // turretCenter.transform.rotation = Quaternion.identity;

            turretCenter.transform.rotation = Quaternion.RotateTowards(turretCenter.transform.rotation, Quaternion.Euler(0, 0, turretDesiredAngle - 90), turretRotationSpeed);
        }




        // FixedUpdate is called once per physics frame
        void FixedUpdate()
        {
            //Debug.Log("Currently looking at: " + locations[sightValue]);


            /*

                    if (Input.GetKey(KeyCode.W))
                    {
                        MoveForward(moveSpeed);
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        MoveBackward(moveSpeed);
                    }
                    if (Input.GetKey(KeyCode.D))
                    {
                        RotateRight();
                    }

                    if (Input.GetKey(KeyCode.A))
                    {
                        RotateLeft();
                    }
            */
        }

        private void MoveBackward(float moveSpeed)
        {
            MoveForward(moveSpeed * -1);
        }

        public void MoveForward(float speed)
        {
            if (!base.IsHost)
                tankRB.AddForce(transform.up * speed, ForceMode2D.Force);

            MoveForwardServer(this, speed);
        }



        [ServerRpc]
        private void MoveForwardServer(PlayerTank playerTank, float speed)
        {
            playerTank.GetComponent<Rigidbody2D>().AddForce(transform.up * speed, ForceMode2D.Force);
        }
        private void RotateRight()
        {
            desiredAngle -= tankRotationSpeed;
        }

        private void RotateLeft()
        {
            desiredAngle += tankRotationSpeed;
        }

        public void RotateTank(float desiredAngle)
        {
            /*
            if(!base.IsHost)
            {
               //tankRB.MoveRotation(desiredAngle);
                 transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, desiredAngle), 70f * Time.fixedDeltaTime);
            }
            */
            RotateTankServer(this, desiredAngle);
        }


        //[ServerRpc]
        public void RotateTankServer(PlayerTank playerTank, float desiredAngle)
        {
            //playerTank.tankRB.MoveRotation(desiredAngle);

            playerTank.transform.rotation = Quaternion.RotateTowards(playerTank.transform.rotation, Quaternion.Euler(0, 0, desiredAngle), 70f * Time.fixedDeltaTime);
        }


        private void GivePoints(int amount)
        {
            this.points += amount;
        }






    }
}
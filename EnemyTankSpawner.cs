using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace TankRoulette
{

    public class EnemyTankSpawner : NetworkBehaviour
    {

        public int maxTanksPerSpawner;
        public float spawnRandomizeAmount = 2.5f;

        private GameObject GameManager;

        public List<TankNode> NodeTraversalOrder = new();

        [SerializeField]
        public Transform searchModeReferencePoint;

        [SyncVar]
        public GameObject[] tankList;
        public GameObject navBuffer;

        public GameObject SpawnedEnemyTank;
        //  public GameObject EnemyTankPrefab;

        private void Start()
        {
            GameManager = GameObject.FindGameObjectWithTag("GameController");
            maxTanksPerSpawner = GameManager.GetComponent<EnemyLocationManager>().maxTanksPerSpawn;
            tankList = new GameObject[maxTanksPerSpawner];
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (base.IsClientOnly)
                this.enabled = false;
        }



        private void Update()
        {
            /*
            if (Input.GetKeyDown(KeyCode.L))
            {
                int counter = 0;
                foreach (GameObject tank in tankList)
                {
                    counter++;
                    Debug.Log("tank number: " + counter + " : " + tank);
                }

            }
            */

            if (Input.GetKeyDown(KeyCode.G))
            {
                //       SpawnEnemyTank(EnemyTankPrefab);
                //  Debug.Log("1877 732 2848 +8001 \"anna franko\"");
            }
        }


        public int CheckIfFull()
        {

            //simple loop to check if there is at least one open spot
            //remember that the tanks may not be destroyed in the order that
            //they were spawned in, so there may be gaps in the middle
            //this method will return -1 if there was no open spot, else it will
            //return the index of the open spot

            int openIndex = -1;

            for (int i = 0; i < tankList.Length; i++)
            {
                if (tankList[i] == null)
                {
                    openIndex = i;
                    break;
                }
            }

            return openIndex;
        }


        public void StartEnemyTankSpawn(GameObject TankPrefab, int level)
        {
            //first, check if the list has at least one open spot, if it does then spawn a sucker in.
            //I dont know how else to keep track of the amount of tanks at a specific location, thats why using list

            int spawnIndex = CheckIfFull();
            if (spawnIndex != -1)
            {

                //get random num double of the random amount then subtract half
                float offsetX = Random.Range(0, spawnRandomizeAmount * 2);
                float offsetY = Random.Range(0, spawnRandomizeAmount * 2);

                offsetX -= spawnRandomizeAmount;
                offsetY -= spawnRandomizeAmount;


                //hard code the "random" seed value
                // spawnLocation = this.transform;
                //      GameObject SpawnedEnemyTank;

                //  if (!base.IsHost)
                //  SpawnedEnemyTank = Instantiate(TankPrefab, new Vector2(transform.position.x + offsetX, transform.position.y + offsetY), transform.rotation);


                //give prefab, script, offsets, and where to spawn in the tank
                ServerInitEnemyTank(TankPrefab, this, offsetX, offsetY, spawnIndex, level);

            }
        }

        private void ServerInitEnemyTank(GameObject EnemyTankPrefab, EnemyTankSpawner EnemySpawner, float offsetX, float offsetY, int spawnIndex, int level)
        {


            //get reference to local array
            GameObject[] TankLocations = EnemySpawner.tankList;


            //  GameObject spawnedNavBuffer = Instantiate(EnemySpawner.navBuffer);
            GameObject SpawnedEnemyTank = Instantiate(EnemyTankPrefab,
            new Vector2(transform.position.x + offsetX, transform.position.y + offsetY), EnemySpawner.transform.rotation);
            // spawnedNavBuffer.GetComponent<EnemyBuffer>().enemy = SpawnedEnemyTank.transform;
            SpawnedEnemyTank.GetComponent<EnemyTank>().originalSpawnPos = this;

            SpawnedEnemyTank.GetComponent<EnemyTank>().InitializeStats(level);
            // EnemyTank enemy = SpawnedEnemyTank.GetComponent<EnemyTank>();
            //depricated stoob
            //enemy.tankLocation = enemyLocation;

            //update the list that holds the tanks
            TankLocations[spawnIndex] = SpawnedEnemyTank;

            //spawn it on the bigulend
            ServerManager.Spawn(SpawnedEnemyTank);


            //  SetActiveTankObservers(TankLocations);



            //  Debug.Log("finished spawning in a tank at index: " + spawnIndex);

            //SpawnEnemyTankObserver(SpawnedEnemyTankServer, this);

        }

        //exclude lozen that already pob
        [ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]
        public void SpawnEnemyTankObserver(GameObject LocalSpawnedTank, EnemyTankSpawner EnemySpawner)
        {
            EnemySpawner.SpawnedEnemyTank = LocalSpawnedTank;
        }




        // Update is called once per frame

    }
}
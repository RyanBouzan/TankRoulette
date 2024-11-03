using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;


//handles the spawning and logic for enemies.
//this class along with EnemyTankSpawner should
//NOT be running on the client
//else it will be major stoob

namespace TankRoulette
{

    public class EnemyLocationManager : NetworkBehaviour
    {
        
        List<int> maxBig = new List<int>();


        //  public List<TankNode> TankNodes = new();

        //keeps track of if there is a tank at a given location
        [SyncVar]
        public EnemyTankSpawner[] EnemySpawnLocations = new EnemyTankSpawner[4];

        public GameObject EnemyTankPrefab;

        public int maxTanksPerSpawn;

        //reference to object that actually spawns tanks in
        //public EnemyTankSpawner EnemySpawner;

        public override void OnStartServer()
        {
            base.OnStartServer();


            //straight up disable if its a client
            if (base.IsClientOnly)
                this.enabled = false;

            //        NetworkConnection conn = ServerManager.Clients.ElementAt(1).Value;
        }


        // Start is called before the first frame update
        void Start()
        {
            //  EnemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemyTankSpawner>();
            // EnemyTankPrefab = Resources.Load("Prefabs/EnemyTankBasic") as GameObject;

        }

        // Update is called once per frame
        void Update()
        {

            //auto assign spawn locations to all have the same one
            if (Input.GetKeyDown(KeyCode.G))
            {

                SpawnEnemyTank(10);
            }

            //replace this code for when to spawn in a tank because it starts here



            if (Input.GetKeyDown(KeyCode.L))
            {
                printClient();
            }



        }

        [ServerRpc(RequireOwnership = false)]
        public void printClient()
        {
            foreach (KeyValuePair<int, FishNet.Connection.NetworkConnection> item in ServerManager.Clients)
            {
                Debug.LogWarning(item.Key + "\t" + item.Value);
                if (!item.Value.IsActive)
                {
                    //   item.Value.ClientId
                    ServerManager.Clients.Remove(item.Key);
                }
                //    List<NetworkObject> nob = item.Value.Objects.ToList<FishNet.Connection.NetworkConnection>();
            }

        }


        protected IEnumerator RetrySpawnDelay(float time)
        {
            yield return new WaitForSeconds(time);
        }





        //Bigulend method for spawning in a tank. will return true if the tank was succesfully spawned
        //will return false if there is no room to spawn in a tank
        public bool SpawnEnemyTank(int level)
        {


            int location = -1;
            int potentialOpenLocation = -1;
            bool lookingForOpenSpawn = true;


            //this code is functional, but far from the most efficient method to do this shid
            while (lookingForOpenSpawn)
            {

                //choose a random spot
                location = UnityEngine.Random.Range(0, 4);

                //check if random spot is full or not
                potentialOpenLocation = EnemySpawnLocations[location].CheckIfFull();


                //if the spot is full:
                if (potentialOpenLocation == -1)
                {
                    //add it to the list of full spots. it is possible for this to happen multiple times
                    maxBig.Add(location);
                }
                else
                {
                    //found an open spot at (open spot). remove it's record from the "full" list
                    while (maxBig.Contains(location))
                        maxBig.Remove(location);

                    //stop the loop and order the spawn
                    lookingForOpenSpawn = false;

                }

                //all juicers are completely full
                int unique = maxBig.Distinct().Count();
                if (unique == EnemySpawnLocations.Length)
                {
                    return false;
                }
            }


            //found a boil with open,order the server to spawn it in at the spod
            AssignTank(EnemySpawnLocations[location], EnemyTankPrefab, level);


            //    Debug.LogWarning("holy shid im crowning");
            return true;
        }


        private void AssignTank(EnemyTankSpawner ChosenSpawnLandmark, GameObject EnemyTankPrefab, int level)
        {
            int RandomLandmark = UnityEngine.Random.Range(0, 4);

            ChosenSpawnLandmark.StartEnemyTankSpawn(EnemyTankPrefab, level);
        }


        [ObserversRpc]
        public void SetActiveTankObservers(GameObject[] TankLocations)
        {
            //this.tankLocations = TankLocations;
        }


    }

}
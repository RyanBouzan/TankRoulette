using System.Collections;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;


namespace TankRoulette
{
    //class responsible for managing the in game rounds
    //contains logic for spawning enemies, and when to spawn objectives
    //hard coded for two players
    public class GameRoundManager : NetworkBehaviour
    {
        public int round;

        [SerializeField]
        private bool roundEnded;

        public int enemiesRemaining;
        public int enemiesToSpawn;

        private bool canSpawn;

        public GameObject Player1Tank;

        public GameObject Player2Tank;



        [SerializeField]
        private EnemyLocationManager Spawner;

        //[SyncVar]
        public bool ObjectiveIsActive = false;

        [SerializeField]
        private GameObject ObjectivePrefab;



        //reference to current spawned objective, which means that there should NOT be more than one
        //at a time!
        private ObjectiveObject SpawnedObjective;

        [SerializeField]
        private List<Transform> objectiveSpawns = new();

        [SerializeField]
        private bool GameStarted = false;

        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            //get components
            Spawner = GetComponent<EnemyLocationManager>();
        }

        private void Start()
        {
            BootstrapNetworkManager.OnGameStart += OnGameStart;

        }

        private void OnGameStart()
        {
            Debug.Log("started game grm");
            GameStarted = true;
            round = 0;
            SetNextRound(round);
            canSpawn = true;
            enemiesToSpawn = 2;
        }

        private void OnDestroy()
        {
            BootstrapNetworkManager.OnGameStart -= OnGameStart;
        }



        // Update is called once per frame
        void Update()
        {
            //outright return if player is a client only, this logic should not be running on the client
            //however, the clients should be aware of other fields in this class,
            //such as enemies remaining, the current round, and whether the round has ended, etc...
            if (!base.IsServer)
            {
                return;
            }

            ////temporary test code to start game
            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    if (!GameStarted)
            //    {
            //        GameStarted = true;
            //        round = 0;
            //        SetNextRound(round);
            //        canSpawn = true;
            //        enemiesToSpawn = 3;
            //    }
            //}



            if (GameStarted)
            {
                //spawn enemies every two seconds while there are enemies to spawn.
                if (enemiesToSpawn > 0 && canSpawn)
                {
                    //enemy location manager API, call the spawn enemy tank method, where the round
                    //is an argument that is passed onto the "InitializeStats" method of the enemy tank class
                    Spawner.SpawnEnemyTank(round);
                    enemiesToSpawn--;
                    enemiesRemaining++;
                    canSpawn = false;
                    //spawn delay
                    StartCoroutine(SpawnDelay(2f));
                }



                if (enemiesRemaining > 0)
                {
                    //do something here? maybe add music or UI context clues
                }

                //no enemies remaning to spawn or be killed, and the objective has been captured.
                else if (enemiesRemaining <= 0 && enemiesToSpawn == 0 && !roundEnded && SpawnedObjective.captured)
                {
                    roundEnded = true;
                    StartCoroutine(NextRoundDelay(5f));
                }

                //when the objective is captured it waits ten seconds + 1 to despawn
                if (SpawnedObjective.finished)
                {
                    UpdateObjectiveStatus(false);
                }
            }
        }


        //broadcast objetive status to all clients
        [ObserversRpc(RunLocally = true)]
        public void UpdateObjectiveStatus(bool val)
        {
            ObjectiveIsActive = val;
        }


        IEnumerator SpawnDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            canSpawn = true;
            yield return 0;
        }

        IEnumerator NextRoundDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetNextRound(round);
            yield return 0;
        }

        private void SetNextRound(int roundNum)
        {
            //increment round and calculate how many enemies to spawn
            round = roundNum + 1;
            enemiesToSpawn = roundNum * 2;
            roundEnded = false;

            //for the first round and every 5th round, spawn an objective with the following
            //modifiers
            if (round == 1 || round % 5 == 0)
            {
                int index = Random.Range(0, objectiveSpawns.Count - 1);

                GameObject SpawnedObjective = Instantiate(ObjectivePrefab, objectiveSpawns[index].position, objectiveSpawns[index].rotation);
                ObjectiveObject temp = SpawnedObjective.GetComponent<ObjectiveObject>();
                temp.FullCapture = 45 + (round * 10);
                temp.GRM = this;
                temp.award = 45 + (round * 10);
                ServerManager.Spawn(SpawnedObjective);
                UpdateObjectiveStatus(true);
                this.SpawnedObjective = temp;
            }
            //debug message
            Debug.Log("Starting next round... ( " + round + ") \n"
                + " Enemies to spawn: " + enemiesToSpawn + "\n"
                + "Objective spawned: " + ObjectiveIsActive
                );

        }
    }
}
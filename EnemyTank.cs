using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus;
using UnityEngine.EventSystems;

namespace TankRoulette {
	public class EnemyTank : NetworkBehaviour
	{


		public enum EnemyState
		{
			Searching, Decision, Attacking, Flanking, Sniping, Resetting, Rushing_Front, Cheating, Idle, None
		}

		


		public NavMeshPlus.Components.NavMeshSurface test;

		[Header("Current State")]
		public EnemyState state = EnemyState.None;

		[Header("Required Components")]
		private GameObject GameManager;
		//nav mesh agent component reference
		private NavMeshAgent navMeshAgent;
		private BootstrapNetworkManager BNM;
		public EnemyTankSpawner originalSpawnPos;
		public bool beenSpawned = false;
		public int bounty = 10;
		public int level;
		[SerializeField]
		private EnemyTankDurability durability;

		[SerializeField]
		private GameObject deathExplosionPrefab;

		[SyncVar]
		public float health;

		[Header("Attack/Movement Fields")]

		[SerializeField]
		private float damage;

		[SerializeField]
		private float projSpeed;

		[SerializeField]
		private float ricochetAngle;

		[SerializeField]
		public bool canFire = true;

		[SerializeField]
		private bool chooseNewSideDirection = false;

		[SerializeField]
		private int initialDirection = 0;

		public float fireDelay;

		//raycast layer to target
		[SerializeField]
		public LayerMask targetLayerMask;

		[SerializeField]
		private GameObject projectileContainerPrefab;

		//[SerializeField]
		//private GameObject projectileRealPrefab;

		//[SerializeField]
		//private GameObject projectileDecorationPrefab;
		public GameObject explosionTestPrefab;
		[SerializeField]
		public GameObject gunExplosionTestPrefab;

		[SerializeField]
		private float gunExplosionOffset = 2f;

		public float explosionDecalScale = 2f;

		[SerializeField]
		private Transform turretBasket;
		[SerializeField]
		private Transform bulletSpawn;
		public Transform indicator_debug;
		public Transform indicatorIdle_debug;
		public Transform tankBody;
		[SerializeField]
		private Rigidbody2D rb;

		[SerializeField]
		private float TurretTurnSpeed;

		[SerializeField]
		private float MoveSpeed;

		[SerializeField]
		private float TurnSpeed;



		[Header("AI Variables")]

		public Transform Target;
		public Transform prevTarget;

		private Vector2 flankOriginalDirection;

		//raycast layer to ingore
		[SerializeField]
		private LayerMask ignoreLayerMask;


		//distance from player to stop
		public float minimumEngagementDistance = 15f;
		[SerializeField]
		private bool turning = false;

		[SerializeField]
		private int TurningStamina;
		[SerializeField]
		private int TurningStaminaMax;
		[SerializeField]
		private bool TurningReserve;

		//should the enemy tank gain GPS directly to the nearest player?
		//    private bool overrideObstacle = false;

		//cyclic variable that sets the delay for when an enemy can make a decision
		[SerializeField]
		public int reactionTimeCounter = 0;

		public int attackTimeCounter = 0;
		public int attackTimeCutoff = 1000;
		public bool reloactedTarget = false;

		//max interval for reaction time
		public int reactionTime_Max;
		[SerializeField]
		int reactionTime;

		[SerializeField]
		public bool target = true;

		[SerializeField]
		private bool findNewTarget = false;

		[Header("Unused?")]

		public float nodeTerminateDistance = 5f;


		//generates a circle around the player tank and checks if there are any nodes within it.
		public float searchRadius = 200f;

		public Vector2 searchPoint = Vector2.zero;

		[SerializeField]
		private float offsetDistance = 60f;

		[SerializeField]
		private Transform obstacle_indicator;

		[SerializeField]
		private bool stuck = false;


		private bool newSearch = true;
        private bool dead = false;
        public GameObject explosionParticlePrefab;

        //	int nodeIndex = 0;
        //int nodeCount;












        public void Awake()
		{
			GameManager = GameObject.FindGameObjectWithTag("GameController");
			//		ELM = GameManager.GetComponent<EnemyLocationManager>();
			BNM = GameObject.Find("BootstrapNetworkManager").GetComponent<BootstrapNetworkManager>();
			turretBasket = transform.GetChild(0);
			bulletSpawn = transform.GetChild(0).GetChild(0);
			indicator_debug = transform.GetChild(1);
			indicatorIdle_debug = transform.GetChild(2);
			tankBody = transform;
			indicator_debug.transform.parent = null;
			obstacle_indicator.transform.parent = null;
			rb = GetComponent<Rigidbody2D>();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			//tell the navmesh controller to not update the rotation,
			//this is because it is 2D
			if (base.IsClientOnly)
				return;
			navMeshAgent.updateRotation = false;
			navMeshAgent.updateUpAxis = false;

		}

		public override void OnSpawnServer(NetworkConnection connection)
		{
			base.OnSpawnServer(connection);
			//Stats argument controls the "level" of the tank

			navMeshAgent = GetComponent<NavMeshAgent>();
			//do this again for double check
			navMeshAgent.updateRotation = false;
			navMeshAgent.updateUpAxis = false;

			//events are pretty cool, subscribe to the game end event posted by bootstrap network manager
			BootstrapNetworkManager.OnGameEnd += OnGameEnd;
		}

		private void OnGameEnd()
		{
			//conviently unsubscribe in the same method, 
			BootstrapNetworkManager.OnGameEnd -= OnGameEnd;
			base.Despawn();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			//InitializeStats(2, this);


		}


		//method to intialize most stats for the tank
		public void InitializeStats(int level)
		{
			navMeshAgent = GetComponent<NavMeshAgent>();


			//adjust formulas for enemy tank stats

			//idle state
			state = EnemyState.Idle;
			this.level = level;

			for (int i = 0; i < 3; i++)
			{
				durability.ERA_Segments[i] = 3 + level;
			}
			durability.ERA_Segments[3] = level;
			durability._Integrity = level * 1.5f;

			//enemy.magazineCount = 1 + level;

			//  enemy.moveSpeed = 60 + (level * 5);
			//  enemy.tankRotationSpeed = 60 + (level * 3);



			reactionTime_Max = Mathf.Clamp(600 - (30 * level), 300, 600);
			attackTimeCutoff = 500 + (50 * level);
			reactionTime = 30 + (10 * level);

			fireDelay = 12 - (level * 0.25f);

			damage = 7 + (level * 0.33f);

			beenSpawned = true;

			MoveSpeed = 30f + (level);
			TurnSpeed = 30f + (level);//Mathf.Clamp(30f + (2 * level), 30f, 55f);
			TurningStaminaMax = 40 + (200 * level);
			TurningStamina = TurningStaminaMax * 10;
			TurningReserve = true;
			TurretTurnSpeed = 20f + (2 * level);
			bounty = 10 + (level * 2);
		}


		//method overload
		//same method as line of sight but allows for single tank to be targeted manually from method argument.
		private Transform LineOfSight(EnemyTank enemy, GameObject player, bool overrideObstacle)
		{
			List<PlayerObject> tempList = new()
		{
			new PlayerObject(null, player, null)
		};

			return LineOfSight(enemy, tempList, overrideObstacle);
		}

		//method overload
		//same method as line of sight but allows for single tank to be targeted manually from method argument.
		private Transform LineOfSight(EnemyTank enemy, PlayerObject player, bool overrideObstacle)
		{
			List<PlayerObject> tempList = new()
		{
			player
		};

			return LineOfSight(enemy, tempList, overrideObstacle);
		}

		//from a list of all the player tanks, return the closest one within "line of sight"
		//return null if cant find one, OR if override obstacle is true then return closest player ignoring obstacles.
		private Transform LineOfSight(EnemyTank enemy, List<PlayerObject> allPlayers, bool overrideObstacle)
		{
			float minDistance = Mathf.Infinity;
			Transform closestPlayer = null;

			for (int i = 0; i < allPlayers.Count; i++)
			{
				if (allPlayers[i] == null)
					continue;

				GameObject playerTank = allPlayers[i].GetTank();

				Debug.DrawLine(enemy.transform.position, playerTank.transform.position, Color.green, 0.05f);

				RaycastHit2D sight = Physics2D.Raycast(enemy.transform.position, playerTank.transform.position - enemy.transform.position, 2000f, ~enemy.ignoreLayerMask);
				//Debug.LogError(sight.transform.name);
				if (sight.distance < minDistance)
				{
					minDistance = sight.distance;
					closestPlayer = playerTank.transform;
					Debug.LogWarning("closest player is: " + closestPlayer.name);
				}

				if (sight.collider.gameObject.Equals(playerTank))
				{
					Debug.DrawRay(enemy.transform.position, playerTank.transform.position - enemy.transform.position, Color.blue, 0.05f);

					Debug.Log("got a visual");
					enemy.prevTarget = playerTank.transform;
					return playerTank.transform;
				}
				else
				{
					//Debug.Log("blocked by " + sight.transform.name);
				}

			}
			if (overrideObstacle)
			{
				Debug.DrawRay(enemy.transform.position, closestPlayer.transform.position - enemy.transform.position, Color.green, 0.05f);

				return closestPlayer;
			}
			return null;
		}


		//contains logic for moving the tank towards a target. also contains code to help ensure
		//the tank does not strafe, by making sure the tank is pointed towards the target before moving
		private void GoToTarget(EnemyTank enemy, Vector2 target, float minimumEngagementDistance)
		{
			enemy.navMeshAgent.SetDestination(target);

			if (Vector2.Distance(transform.position, target) > minimumEngagementDistance)
			{

				RaycastHit2D frontCheck = Physics2D.Raycast(transform.position, transform.up, 7.5f, targetLayerMask);

				if (frontCheck.collider == null)
				{

					Debug.LogWarning($"going to target with speed {MoveSpeed}");
					Vector2 force = transform.up * MoveSpeed;
					rb.AddForce(force);

					// Limit the velocity to prevent drifting
					float limitedSpeed = Mathf.Clamp(rb.velocity.magnitude, 0f, MoveSpeed);
					rb.velocity = rb.velocity.normalized * limitedSpeed;
				}
				else
				{
					TurningStamina = TurningStaminaMax;
					TurningReserve = true;
					Debug.LogWarning(frontCheck.collider.name + " is in the way");
				}

			}
		}


		//rotate tank body towards target

		private void LookAtTarget(EnemyTank enemy, Vector2 target)
		{
			float speed = TurnSpeed * Time.deltaTime;

			Vector2 targetDirection = target - (Vector2)enemy.tankBody.transform.position;



			float tankDesiredAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
			if (Mathf.Abs(Mathf.DeltaAngle(enemy.tankBody.transform.eulerAngles.z, tankDesiredAngle - 90)) >= 0 && TurningStamina > 0 && TurningReserve)
			{
				enemy.tankBody.rotation = Quaternion.RotateTowards(enemy.tankBody.rotation, Quaternion.Euler(0, 0, tankDesiredAngle - 90), speed);

				TurningStamina--;

				if (TurningStamina == 0)
				{
					TurningReserve = false;
				}
			}
			else if (TurningStamina < TurningStaminaMax)
			{
				if (TurningStamina > TurningStaminaMax - 15)
				{
					TurningReserve = true;
				}
				TurningStamina++;
			}

			//Debug.LogWarning("DesiredAngle fixed: " + (tankDesiredAngle-90));
			//Debug.LogWarning("Tank angle: " + );




		}


		//controls turret to aim gun at target. the "implicated" parameter decides if shooting is allowed
		private void AimAtTarget(EnemyTank enemy, Transform target, bool implicated, float spreadFactor, float damage, float fireDelay)
		{

			float speed = TurretTurnSpeed * Time.deltaTime;

			Vector2 targetDirection = target.transform.position - enemy.turretBasket.transform.position;

			float turretDesiredAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
			enemy.turretBasket.rotation =
			Quaternion.RotateTowards(enemy.turretBasket.rotation, Quaternion.Euler(0, 0, turretDesiredAngle - 90), speed);

			//Debug.Log("target angle: " + turretDesiredAngle);

			//Debug.LogError("targeting: " + target);

			if ((Mathf.Abs(Mathf.DeltaAngle(enemy.turretBasket.transform.eulerAngles.z, (turretDesiredAngle - 90))) <= 1)
				&& implicated)
			{
				//Debug.LogError("called the big one");
				EnemyFire(enemy, spreadFactor, damage, fireDelay);
			}
			// Debug.LogError("Difference is: " + Mathf.DeltaAngle(enemy.turretBasket.transform.eulerAngles.z, (turretDesiredAngle - 90)));

		}


		//enemy fire controller
		private void EnemyFire(EnemyTank enemy, float spreadFactor, float damage, float fireDelay)
		{
			if (!enemy.canFire)
				return;

			enemy.canFire = false;
			StartCoroutine(FireDelay(fireDelay));


			if (enemy.navMeshAgent.isStopped)
			{
				//spreadFactor /= 3f;
			}


			Vector3 direction = enemy.bulletSpawn.up;

			//direction.x += UnityEngine.Random.Range(-spreadFactor, spreadFactor);
			//direction.y += UnityEngine.Random.Range(-spreadFactor, spreadFactor);
			// what was i thinking with this? direction.z += UnityEngine.Random.Range(-spreadFactor, spreadFactor);
			//Debug.Log("got a shooter here");

			//DEPRECATED
			//Debug.DrawRay(enemy.bulletSpawn.position, direction, Color.yellow, 1f);

			//fire raycast!
			//RaycastHit2D hitObject = Physics2D.Raycast(enemy.bulletSpawn.position, direction, 100f, ~enemy.ignoreLayer);


 
			//assign values
			PlayerFireController.LiveFireData lfd = new()
			{
				_damage = damage,
				_direction = direction,
				_explosionDecalScale = explosionDecalScale,
				_bulletSpawnPosition = bulletSpawn.position,
				_bulletSpawnRotation = bulletSpawn.rotation,
				_maxDistance = 250f,
				_projSpeed = projSpeed,
				_ricochetAngle = ricochetAngle
			};

			GameObject spawnedProjectile = Instantiate(projectileContainerPrefab, lfd._bulletSpawnPosition, lfd._bulletSpawnRotation);
            ProjectileRealCollision prc = spawnedProjectile.GetComponent<ProjectileRealCollision>();
            prc.Initialize(lfd, this);
            prc.ready = true;
            ServerManager.Spawn(spawnedProjectile);
			EnemyObserverFire(enemy, spawnedProjectile.gameObject);

			

		}
		[ObserversRpc(RunLocally = true)]
		private void EnemyObserverFire(EnemyTank enemy, GameObject containerObject)
		{
			enemy.GetComponent<AudioSource>().Play();
			Vector2 temp =(Vector2)bulletSpawn.position + (Vector2)(bulletSpawn.up * gunExplosionOffset);
			GameObject firedGunDecoration = Instantiate(gunExplosionTestPrefab, temp, bulletSpawn.rotation);
			ServerManager.Spawn(firedGunDecoration);
		}

		void FixedUpdate()
		{
			//another sanity check
			if (base.IsServer)
			{

				//have the tanks even been spawned yet?
				if (BNM.players.Count == 0)
				{
					return;
				}

				/*
				//is there at least one active player tank?
				if (!MM3.Player1Client.tankIsActive && !MM3.Player2Client.tankIsActive)
				{
					return;
				}
				*/

				//have i been spawned?
				if (!beenSpawned)
				{
					return;
				}


				//check if health is empty.
				if (durability._Integrity <= 0 && !dead)
				{
					dead = true;
					GameObject deathExplosion = Instantiate(deathExplosionPrefab, transform.position, transform.rotation);
					ServerManager.Spawn(deathExplosion);

					//can call destroy and not despawn because it was never spawned over the server
					GameManager.GetComponent<GameRoundManager>().enemiesRemaining -= 1;
					Destroy(indicator_debug.gameObject);
					Destroy(obstacle_indicator.gameObject);
					base.Despawn(gameObject);
				}

				indicator_debug.transform.position = navMeshAgent.steeringTarget;




				//true when the current tank is lost or destroyed
				if (findNewTarget)
				{
					findNewTarget = false;
					state = EnemyState.Resetting;
					navMeshAgent.isStopped = true;
					StopAllCoroutines();
					StartCoroutine(ReTargetDelay(7f));
				}

				if (Input.GetKeyDown(KeyCode.M))
				{
					int area = NavMesh.GetAreaFromName("Walkable");
					Debug.LogWarning("Mesh check:" + NavMesh.SamplePosition(indicatorIdle_debug.position, out NavMeshHit hit, 10f, area));
					Debug.LogWarning(hit.mask);
				}


				//switch case for the enemy state. runs logic accordingly based on the current state
				switch (state)
				{

					//if the state is still idle by this time, set it to searching
					case EnemyState.Idle:
						state = EnemyState.Searching;
						break;





					//state where the enemy is theoretically searching for a target.
					//it cannot fire, move, rotate, or aim while searching, UNLESS already had a previous target.
					case EnemyState.Searching:
						//do nothing until count to reaction Time max.
						reactionTimeCounter++;
						if (reactionTimeCounter >= reactionTime_Max)

						{

							state = EnemyState.Decision;

						}
						else
						{

							if (newSearch)
							{
								newSearch = false;

								int num = UnityEngine.Random.Range(0, 10);

								//50% chance for enemies to flank, and their level must be greater than 5.
								if (num > 5 && level > 5)
								{
									state = EnemyState.Flanking;
									canFire = false;
									StartCoroutine(FireDelay(fireDelay));

									reactionTimeCounter = 0;
									attackTimeCounter = 0;
									break;
								}


								Debug.Log("implement search mode here");

								prevTarget = LineOfSight(this, BNM.players, true);

								searchPoint = (Vector2)prevTarget.position + UnityEngine.Random.insideUnitCircle * searchRadius;
								obstacle_indicator.position = searchPoint;
								RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, 1f, LayerMask.NameToLayer("EnemyNav"));

								while (true)
								{
									if (Vector2.Distance(searchPoint, prevTarget.position) <= 40)
									{
										if (hit.collider != null)
										{
											if (hit.collider.OverlapPoint(searchPoint))
											{
												Debug.LogWarning("search point was inside an obstacle, retrying");
												searchPoint = (Vector2)prevTarget.position + UnityEngine.Random.insideUnitCircle * searchRadius;
												hit = Physics2D.Raycast(transform.position, transform.up, 1f, LayerMask.NameToLayer("EnemyNav"));
												continue;
											}
										}
										searchPoint = (Vector2)prevTarget.position + UnityEngine.Random.insideUnitCircle * searchRadius;
										hit = Physics2D.Raycast(transform.position, transform.up, 1f, LayerMask.NameToLayer("EnemyNav"));
										continue;
									}
									else
									{
										break;
									}
								}

							}
							obstacle_indicator.position = searchPoint;
							LookAtTarget(this, indicator_debug.position);

							GoToTarget(this, searchPoint, 0f);

							if (Vector2.Distance(transform.position, obstacle_indicator.position) <= 10f)
							{
								newSearch = true;
							}

						}
						break;

					case EnemyState.Decision:

						if (reactionTimeCounter > 1500)
						{
							reactionTimeCounter = 0;
							ResetTankAI(EnemyState.Searching);
						}


						reactionTimeCounter++;

						//at this point we send out the raycast for the nearest player

						Target = LineOfSight(this, BNM.players, false);

						if (Target == null)
						{
							if ((reactionTimeCounter - reactionTime_Max) > reactionTime)
							{
								reactionTimeCounter = 0;
								state = EnemyState.Searching;
							}

						}
						//found a valid target!
						else
						{

							if (prevTarget == null)
							{
								prevTarget = Target;
							}

							/*
							else if(prevTarget != Target)
							{
								if(reactionTimeCounter > 6000)
								Debug.LogError("\"lost\" the target, going to the new one target");
							}
							*/



							int num = UnityEngine.Random.Range(0, 10);



							if (Vector2.Distance(Target.position, transform.position) > 75 && num > 7)
							{
								state = EnemyState.Sniping;
								canFire = false;
								StartCoroutine(FireDelay(fireDelay * 2));
							}
							else if (num > 4)
							{
								state = EnemyState.Attacking;
								canFire = false;
								StartCoroutine(FireDelay(fireDelay));
								GoToTarget(this, Target.position, minimumEngagementDistance);
							}

						}

						break;

					case EnemyState.Rushing_Front:
						break;
					case EnemyState.Flanking:

						//calculate side destination
						reactionTimeCounter++;


						Transform cheatTarget = LineOfSight(this, BNM.players, true);

						if (Vector2.Distance(cheatTarget.position, transform.position) <= 80f || stuck || reactionTimeCounter >= 4500) //|| attackTimeCounter >= 1000)
						{
							state = EnemyState.Attacking;
							GoToTarget(this, cheatTarget.position, minimumEngagementDistance);
							reactionTimeCounter = 501;
							attackTimeCounter = 0;
							//capital T target
							Target = cheatTarget;
							prevTarget = cheatTarget;
						}


						Vector2 sideDirection;


						Vector2 directionToTarget;
						directionToTarget = (cheatTarget.position - transform.position).normalized;

						sideDirection = Vector2.Perpendicular(directionToTarget) * offsetDistance;

						if (chooseNewSideDirection)
						{

							LayerMask obstacleLayerMask = LayerMask.GetMask("EnemyNav");

							chooseNewSideDirection = false;




							flankOriginalDirection = Vector2.Perpendicular(directionToTarget).normalized * offsetDistance;


							if (initialDirection == 0)
							{
								float randomValue = UnityEngine.Random.Range(0f, 1f);

								if (randomValue < 0.5f)
								{
									initialDirection = -1;
								}
								else
								{
									initialDirection = 1;
								}

							}

							//flankOriginalDirection *= initialDirection;

							Debug.DrawRay(transform.position, sideDirection, Color.yellow, 2f);

							//front raycast to check for obstacles

							RaycastHit2D obstacleCheck = Physics2D.Raycast(transform.position, sideDirection, offsetDistance, obstacleLayerMask);

							obstacle_indicator.position = transform.position;

							obstacle_indicator.position = (Vector2)transform.position + (flankOriginalDirection);

							if (obstacleCheck.collider != null && obstacleCheck.collider.isTrigger)
							{
								Debug.LogWarning("obstacle check hit collider: " + obstacleCheck.collider.gameObject.name);
								if (obstacleCheck.collider.OverlapPoint(obstacle_indicator.position))
								{
									if (offsetDistance >= 100)
									{
										stuck = true;
										break;
									}
									Debug.LogWarning("enemy tank destination is invalid, increasing distance: ");
									offsetDistance += 1;
									chooseNewSideDirection = true;



								}
								else
								{
									offsetDistance = 60f;
									indicator_debug.position = obstacle_indicator.position;
									//GoToTarget(this,indicator_debug.position, 0);
									//LookAtTarget(this, indicatorIdle_debug.position);
								}
							}
							else
							{
								indicator_debug.position = (Vector2)transform.position + Vector2.Perpendicular(directionToTarget).normalized * offsetDistance;
								//GoToTarget(this, indicator_debug.position, 0);
								//LookAtTarget(this, indicatorIdle_debug.position);


							}
						}

						GoToTarget(this, indicator_debug.position, 0);

						if (Vector2.Angle(sideDirection, flankOriginalDirection) > 30f || Vector2.Distance(transform.position, indicator_debug.position) <= 7.5f)
						{
							chooseNewSideDirection = true;
							Debug.LogWarning("implement flank recalculation here.");
						}

						Debug.DrawRay(transform.position, directionToTarget, Color.yellow);

						Debug.DrawRay(transform.position, flankOriginalDirection * 10f, Color.magenta);
						Debug.DrawRay(transform.position, sideDirection, Color.cyan);
						LookAtTarget(this, indicator_debug.position);

						if (LineOfSight(this, cheatTarget.gameObject, false) != null)
						{
							//Debug.LogWarning("implicated!");
							AimAtTarget(this, cheatTarget, true, 1f, damage, fireDelay);
						}
						else
						{
							AimAtTarget(this, indicatorIdle_debug, false, 1f, damage, fireDelay);
							attackTimeCounter++;
						}


						break;
					case EnemyState.Cheating:


						break;

					case EnemyState.Sniping:

						//potential source of jackson if a tank's target is null for some reason.
						try
						{
							if (!Target.gameObject.activeInHierarchy && !prevTarget.gameObject.activeInHierarchy)
							{

								Debug.LogWarning("detected player kill");
								findNewTarget = true;
								break;
							}
						}
						catch (NullReferenceException)
						{
							Debug.LogWarning("the enemy's target was null while trying to determine if the target was dead");
						}

						reactionTimeCounter++;
						Target = LineOfSight(this, prevTarget.gameObject, false);

						minimumEngagementDistance = 150;


						if (Target == null)
						{
							attackTimeCounter++;

							if (attackTimeCounter >= attackTimeCutoff * 4)
							{
								findNewTarget = true;
							}
						}
						else
						{
							//dont move or face target after about 3 seconds and ensure that it is
							if (reactionTimeCounter < 300 || Vector2.Distance(transform.position, Target.position) > minimumEngagementDistance)
								LookAtTarget(this, Target.position);

							GoToTarget(this, prevTarget.position, minimumEngagementDistance);

							attackTimeCounter = 0;
							AimAtTarget(this, Target, Vector2.Distance(transform.position, prevTarget.transform.position) <= 150f, 4f, damage, fireDelay * 1.5f);
						}

						break;

					case EnemyState.Attacking:

						//potential source of jackson if a tank's target is null for some reason.
						try
						{
							if (!Target.gameObject.activeInHierarchy && !prevTarget.gameObject.activeInHierarchy)
							{

								Debug.LogWarning("detected player kill");
								findNewTarget = true;
								break;
							}
						}
						catch (NullReferenceException)
						{
							Debug.LogWarning("the enemy's target was null while trying to determine if the target was dead");
						}

						reactionTimeCounter++;


						//if the reactiontime counter is less than 500, then the enemy is "implicated"
						//and gains wall hack. else it must rely on line of sight ONLY.
						if (reactionTimeCounter <= 500)
						{
							minimumEngagementDistance = 5;
							Target = LineOfSight(this, prevTarget.gameObject, true);
							//Debug.LogError("expected target is: " + prevTarget.gameObject.name);
							AimAtTarget(this, prevTarget, Vector2.Distance(transform.position, prevTarget.transform.position) <= 50f, 15f, damage, fireDelay);


							Transform tempCheck = LineOfSight(this, prevTarget.gameObject, false);
							if (tempCheck != null)
							{
								if (tempCheck.gameObject.Equals(prevTarget.gameObject))
								{
									minimumEngagementDistance = 30;
								}
							}

						}
						//500 "units" have passed and the enemy loses the hack but will go to last known position
						else
						{
							//try to locate target by line of sight ONLY
							Target = LineOfSight(this, prevTarget.gameObject, false);



							//for every tick the target is lost, add one to this counter
							//aim at last known position

							if (Target == null)
							{
								reloactedTarget = true;
								attackTimeCounter++;
								AimAtTarget(this, indicator_debug.transform, false, -1f, -1f, -1f);
								minimumEngagementDistance = 0;


								if (attackTimeCounter >= attackTimeCutoff)
								{
									reactionTimeCounter = 0;
									attackTimeCounter = 0;
									state = EnemyState.Searching;
									prevTarget = null;
									searchRadius = 50f;
								}
							}
							//the target was reacquired, so reset the counter
							//aim at the tank
							else
							{
								if (reloactedTarget)
								{
									reloactedTarget = false;
									if (canFire)
									{
										Debug.LogError("lost the target, activating half firedelay");
										canFire = false;
										StartCoroutine(FireDelay(fireDelay / 2));
									}
								}
								minimumEngagementDistance = 30;
								attackTimeCounter = 0;
								AimAtTarget(this, prevTarget.transform, Vector2.Distance(transform.position, prevTarget.transform.position) <= 50f, 12f, damage, fireDelay);

							}
						}

						//while it is less than the cutoff
						if (attackTimeCounter <= attackTimeCutoff)
						{
							if (Vector2.Distance(transform.position, prevTarget.position) > minimumEngagementDistance)
								LookAtTarget(this, indicator_debug.transform.position);
							GoToTarget(this, prevTarget.position, minimumEngagementDistance);
						}
						//completely lost the target, reset to search mode.
						else
						{
							reactionTimeCounter = 0;
							attackTimeCounter = 0;
							state = EnemyState.Searching;
							prevTarget = null;
						}


						break;

				}





			}
		}


		private void ResetTankAI(EnemyState newState)
		{
			Debug.LogWarning("resetting vals");
			StopAllCoroutines();
			reactionTimeCounter = 0;
			attackTimeCounter = 0;
			prevTarget = null;
			Target = null;
			state = newState;
			navMeshAgent.isStopped = false;
			indicator_debug.transform.position = indicatorIdle_debug.transform.position;
			navMeshAgent.destination = indicator_debug.transform.position;
		}

		[TargetRpc]
		public void AwardBounty(NetworkConnection conn, int bounty)
		{

			//extremely janky solution
			ShopUI tempShopRef;
			tempShopRef = conn.FirstObject.GetComponent<ClientTankManager>().shop;
			Debug.LogError("sent target rpc");
			tempShopRef.bounty = true;
			tempShopRef.tempBountyAmount = bounty;
			//tempShopRef.AddBounty(5);
		}


		IEnumerator ReTargetDelay(float delay)
		{
			yield return new WaitForSeconds(delay);

			ResetTankAI(EnemyState.Searching);

		}

		IEnumerator FireDelay(float delay)
		{
			yield return new WaitForSeconds(delay);

			canFire = true;

		}


	}
}
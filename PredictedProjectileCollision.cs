using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;

namespace TankRoulette
{
    public class PredictedProjectileCollision : ProjectileBase
    {
        public struct ProjReplicateData : IReplicateData
        {
            public bool _impacted;

            public Vector2 _position;

            public Quaternion _rotation;

            public float _remainingDistance;

            public bool _lastSegment;

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;

            public ProjReplicateData(bool impacted, Vector2 position, Quaternion rotation, float remainingDistance, bool lastSegment)
            {
                _impacted = impacted;
                _position = position;
                _rotation = rotation;
                _remainingDistance = remainingDistance;
                _lastSegment = lastSegment;
                _tick = 0;
            }
        }

        public struct ProjReconcileData : IReconcileData
        {
            public bool _impacted;

            public Vector2 _position;

            public Quaternion _rotation;

            public float _remainingDistance;

            public bool _lastSegment;

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;

            public ProjReconcileData(bool impacted, Vector2 position, Quaternion rotation, float remainingDistance, bool lastSegment)
            {
                _impacted = impacted;
                _position = position;
                _rotation = rotation;
                _remainingDistance = remainingDistance;
                _lastSegment = lastSegment;
                _tick = 0;
            }
        }

        
        [SerializeField] private ProjectileAudioControler pac;

        [SerializeField] private CircleCollider2D nearMissColider;
        //damage
        public float damage;

        public float ricochetAngle;

        public float ricochetCount = 0;

        //controls the probability for damaging modules (engine, tracks)
        //not implemented yet
        public int moduleDamageChance;

        //Max range of projectile
        public float maxDistance;

        //stores projectiles initial position
        public Vector2 initialPosition;

        //Vector for direction to travel

        //holds collision layermask
        public LayerMask targetLayerMask;

        public string targetLayerName;

        public Transform decoration;


        //has the projectile impacted?
        public bool impacted = false;

        //explosion decal to spawn in on impact
        public GameObject explosionDecalPrefab;

        public GameObject explosionParticlePrefab;
        //scale of explosion decal
        public float explosionDecalScale;

        //rotation of explosion, set when the tank fires the projectile
        public float explosionDecalRotation;

        //reference to decoration partner

        //debug color
        private Color[] debugColor = { Color.green, new Color(1f, 0.64f, 0f), Color.blue };
        private int i = 0;


        //very important. The player tank that fires the projectile will set reference to this.
        //the enemy tank takes this and on death, awards coin to the player's shop script
        public ShopUI owningShopRef;

        //owning connection of the object who fired this
        public NetworkConnection owningConn;
        public Vector2 endpoint;


        //private variables
        [SerializeField]
        private float remainingDistance;
        private bool lastSegment = false;


       private void Awake()
    {
        //instead of FixedUpdate, use Fishnet's timemanager to update each frame
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }

        private void OnDestroy()
        {
            if (InstanceFinder.TimeManager != null)
            {
                InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
                InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
            }
        }


        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
        }

        private void InitializeInternal(PlayerFireController.LiveFireData lfd)
        {
            direction = lfd._direction;
            damage = lfd._damage;
            projSpeed = lfd._projSpeed;
            ricochetAngle = lfd._ricochetAngle;
            explosionDecalScale = lfd._explosionDecalScale;
            gameObject.SetActive(true);
            explosionDecalRotation = lfd._bulletSpawnRotation.eulerAngles.z - 90;
            explosionDecalScale = lfd._explosionDecalScale;
            transform.SetPositionAndRotation(lfd._bulletSpawnPosition, lfd._bulletSpawnRotation);
            maxDistance = lfd._maxDistance;
            //initial pos is the current position
            initialPosition = transform.position;
            //take the current position and add the direction vector. multiply by max distance to get endpoint
            endpoint = initialPosition + (lfd._direction * lfd._maxDistance);
        }
        public override void Initialize(PlayerFireController.LiveFireData lfd, EnemyTank et)
        {
            InitializeInternal(lfd);
            team = false;
            explosionDecalScale = et.explosionDecalScale;
            targetLayerMask = et.targetLayerMask;
            explosionDecalPrefab = et.explosionTestPrefab;
            explosionParticlePrefab = et.explosionParticlePrefab;
            targetLayerName = "PlayerLayer";
            targetLayerMask = et.targetLayerMask;
        }

        public override void Initialize(PlayerFireController.LiveFireData lfd, PlayerFireController pfc)
        {
            InitializeInternal(lfd);
            team = true;
            targetLayerName = "EnemyLayer";
            targetLayerMask = pfc.targetLayerMask;
            explosionDecalScale = pfc.explosionDecalScale;
            explosionDecalPrefab = pfc.explosionTestPrefab;
            explosionParticlePrefab = pfc.explosionParticlePrefab;
            owningShopRef = pfc.shopRef;
            owningConn = pfc.LocalConnection;
        }
        [Reconcile]
        private void ProjReconcile(ProjReconcileData prd, bool asServer, Channel channel = Channel.Unreliable)
        {
            Debug.LogError("proj reconicle");
            impacted = prd._impacted;
            transform.position = prd._position;
            transform.rotation = prd._rotation;
            remainingDistance = prd._remainingDistance;
            lastSegment = prd._lastSegment;
        }
        private void TimeManager_OnTick()
        {
            if (base.IsOwner)
            {
                Debug.LogError("clean");
                //reconcilliate first
                ProjReconcile(default, false);
                //gather user input and put it into movedata "structure"
                //move player using the data stored in movedata
                Traverse(default, false);
            }
            if (base.IsServer)
            {
                //server is master, therefore it does not need to gather inputs
                Traverse(default, true);
            }
        }

        private void TimeManager_OnPostTick()
        {
            //EXTREMELY IMPORTANT to send anything that might affect the transform of an object!
            //this also includes any colliders attatched to the object
            //see video for full explanation

            /* Reconcile is sent during PostTick because we
                 * want to send the rb data AFTER the simulation. */
            if (base.IsServer)
            {
                //all the stuff included in this is explained in "ReconcileData"
                ProjReconcileData frd = new(impacted, transform.position, transform.rotation, remainingDistance, lastSegment); 
                ProjReconcile(frd, true);
            }

            //in general, the "runningAsServer" parameter
            //should be set to true when: SERVER has (DATA) to send to => CLIENT
            //should be set to false when: CLIENT has (DATA) to send to => SERVER
        }
        [Replicate]
        private void Traverse(ProjReplicateData prd, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
        {
            Debug.LogError("calling traverse");
            //true when hit object
            if (impacted)
            {
                Debug.LogError("Master impacted!");
                Destroy(this.gameObject);
            }

            //projectile decoration must be ready at the same time, set from the fire controller
            if (!ready)
                return;



            if (Vector2.Distance(initialPosition, transform.position) > maxDistance)
            {
                Debug.LogWarning("reached MAX distance!");
                impacted = true;
                return;
            }

            //calculate remaning distance each frame
            remainingDistance = Mathf.Abs(Vector2.Distance(transform.position, endpoint));

            if (remainingDistance <= projSpeed)
            {
                projSpeed = remainingDistance;
                lastSegment = true;
            }


            if (i == 2)
                i = 0;
            else
                i++;

            Debug.DrawRay(transform.position, direction * projSpeed, debugColor[i], 1.0f);

            //fire raycast with distance "distanceBetweenFrames", from the position of the object, forward (up)
            RaycastHit2D hitObject = Physics2D.Raycast(transform.position, direction, projSpeed, targetLayerMask);

            if (hitObject.transform == null)
            {
                //do nothing
            }
            else
            {
                //some black magic binary masking. checks if the hit object is in the same LAYER as target layer
                if ((targetLayerMask & 1 << hitObject.transform.gameObject.layer) == 1 << hitObject.transform.gameObject.layer)
                {
                    // Move the position to hitObject.point, but move it behind a couple of units
                    
                    //check for ricochet
                    float angle = Vector2.Angle(direction, hitObject.normal);
                    Debug.LogError(angle-90f);
                    GameObject explosionParticle = Instantiate(explosionParticlePrefab, hitObject.point, Quaternion.LookRotation(hitObject.normal));
                    ServerManager.Spawn(explosionParticle);
                    // Check if the angle is less than 30 degrees
                    if (angle-90f < ricochetAngle)
                    {
                        pac.PlayClip("projectile_ricochet_1");
                        transform.position = hitObject.point;
                        impacted = (ricochetCount > 0) ? true : impacted;
                        if (impacted) return;
                        ricochetCount++;
                        // Calculate the ricochet direction using reflection
                        Vector2 ricochetDirection = Vector2.Reflect(direction, hitObject.normal);
                        // Set the new direction to the ricochet direction
                        
                        direction = ricochetDirection;
                        Vector2 newPosition = hitObject.point + (direction.normalized * 0.15f);
                        transform.position = newPosition;
                        
                        return;
                    }
                    //Debug.LogWarning(Vector2.Angle(hitObject.point, hitObject.normal));

                    // Vector2 ricochetDirection = Quaternion.AngleAxis(Vector2.Angle(hitObject.normal, hitObject.transform.position) * Mathf.Cos(Vector2.Angle(hitObject.normal, hitObject.transform.position)), Vector2.up) * direction;

                    // direction = ricochetDirection;
                    // decoration.direction = ricochetDirection; 

                    //update the decoration's position
                    ready = false;
                    impacted = true;
                    //Debug.LogError("spawning explosion decal");

                    //instantiate and spawn in the explosion decal over the server at the right spot
                    GameObject explosionDecal = Instantiate(explosionDecalPrefab, hitObject.point, Quaternion.Euler(0, 0, explosionDecalRotation));
                    explosionDecal.transform.localScale = new Vector2(explosionDecal.transform.localScale.x * explosionDecalScale
                        , explosionDecal.transform.localScale.y * explosionDecalScale);
                    ServerManager.Spawn(explosionDecal);
                      
                    //obstacle layer num (will break if previous layers are removed)
                    if (hitObject.transform.gameObject.layer != 10)
                    {

                        //basic method to mark if the projectile was fired by player or enemy
                        if (team)
                        {
                            //get references through the "hitOjbect" object
                            EnemyTank enemy = hitObject.transform.GetComponent<EnemyTank>();
                            EnemyTankDurability ed = hitObject.transform.GetComponent<EnemyTankDurability>();


                            //call the projectile impact method, this will order the cooresponding armor plate to take
                            //the amount of damage this projectile is programmed with
                            ed.ProjectileImpact(hitObject.collider, damage);


                            //if dead
                            if (ed._Integrity <= 0)
                            {
                                //award bounty to owning connection. bounty is stored in the enemytank script
                                enemy.AwardBounty(owningConn, enemy.bounty);

                            }
                        }
                        else
                        {
                            //not on team, therefore enemy calls projectile impact on player
                            //works similarly, except there is need for bounty
                            hitObject.transform.GetComponent<PlayerDurability>().
                                ProjectileImpact(hitObject.transform.GetComponent<PlayerDurability>(), hitObject.collider, damage);
                        }
                    }

                }
                else
                {
                    //decor.impacted = true;
                    //Debug.LogError("hit object! : " + hitObject.transform.name);
                    //ready = false;
                }
            }
            //remaining distance is less than the length of a segment.
            //therefore, no need to continue to the next frame.
            if (lastSegment)
            {
              
                impacted = true;
                return;
            }

            //this runs last after all the logic calculations
            //teleport to next transform position, and get ready to fire another raycast
            //the distance between frames float will control how "fast" the projectile travels
            transform.position += (Vector3)direction * projSpeed;
        }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("PlayerLayer"))
        {
            TargetAudio(collision.gameObject.GetComponent<NetworkObject>().LocalConnection);
        }
    }
    [TargetRpc]
    private void TargetAudio(NetworkConnection conn)
    {
        pac.PlayClip("near_miss_1");
    }
    }

}
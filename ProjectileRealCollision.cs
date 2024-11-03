using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace TankRoulette
{
    public class ProjectileRealCollision : ProjectileBase
    {
        [SerializeField] private ProjectileAudioControler pac;

        [SerializeField] private PolygonCollider2D nearMissRadius;

        [SerializeField] private List<GameObject> nearMissed;
        //reference to decoration trail
        public DecorationProjectile decoration;

        //damage
        public float damage;


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



        //has the projectile impacted?
        public bool impacted = false;

        public float ricochetAngle;


        public float ricochetCount = 0;


        //explosion decal to spawn in on impact
        public GameObject explosionDecalPrefab;

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

        [SerializeField]
        private GameObject explosionParticlePrefab;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!base.IsOwner)
            {
                // this.enabled = false;
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
            owningShopRef = pfc.shopRef;
            owningConn = pfc.LocalConnection;
           // distanceBetweenFrames = 25 * .35f;
        }


        [ObserversRpc(RunLocally = true)]
        public void Fire()
        {
            transform.parent = null;
            ready = true;
        }


        private void FixedUpdate()
        {
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

                    float angle = Vector2.Angle(direction, hitObject.normal);
                     GameObject explosionParticle = Instantiate(explosionParticlePrefab, hitObject.point, Quaternion.LookRotation(hitObject.normal));
                    ServerManager.Spawn(explosionParticle);
                    //check for ricochet
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
                    decoration.transform.position = hitObject.point;
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
                if(decoration != null)
                decoration.transform.position = transform.position;
                impacted = true;
                return;
            }

            //this runs last after all the logic calculations
            //teleport to next transform position, and get ready to fire another raycast
            //the distance between frames float will control how "fast" the projectile travels
            transform.position += (Vector3)direction * projSpeed;
        }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer(targetLayerName) && nearMissed.Contains(other.gameObject) == false)
        {
            TargetAudio(other.GetComponent<NetworkObject>().LocalConnection);
            nearMissed.Add(other.gameObject);
        }
    }
    [TargetRpc]
    private void TargetAudio(NetworkConnection conn)
    {
        Debug.LogError("PLAYING NEAR MISS AUDIO");
        pac.PlayClip("projectile_near_miss_1");
    }

    }
}
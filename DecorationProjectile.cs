using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

namespace TankRoulette
{

    public class DecorationProjectile : ProjectileBase
    {
        //see ProjectileRealCollision for more information

        //reference to the real raycasting projectile
        [SerializeField]
        private ProjectileRealCollision master;

        //used to synchronize with the decoration projectile to ensure that they are timed correctly.

        //this is really clean, it is set to true when the real projectile impacts.
        //this means that the decoration never checks for collision, and relies on the raycast instead

        [SerializeField]
        private TrailRenderer trail;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (base.IsClientOnly)
            {
                this.enabled = false;
            }
        }

        private void OnDestroy()
        {
            Debug.LogError("wtfff");
        }

        // [ObserversRpc(RunLocally = true)]
        // private void Fire()
        // {
        //     transform.parent = null;
        //     ready = true;
        // }

        private void InitalizeInternal(PlayerFireController.LiveFireData lfd)
        {
            direction = lfd._direction;
            transform.SetPositionAndRotation(lfd._bulletSpawnPosition, lfd._bulletSpawnRotation);
            projSpeed = 750 * .7f;

            gameObject.SetActive(true);
            if(master == null)
            {
                Debug.LogError("Decoration projectile missing MASTER!");
            }
        }

        public override void Initialize(PlayerFireController.LiveFireData lfd, EnemyTank et)
        {
            InitalizeInternal(lfd);
        }

        public override void Initialize(PlayerFireController.LiveFireData lfd, PlayerFireController pfc)
        {
            InitalizeInternal(lfd);
        }

        private void Update()
        {
            if (!ready)
                return;
            
            if (master.impacted)
            {
                trail.Clear();
                Debug.LogError("IMPACTED!");
                Destroy(this.gameObject);
            }
            if (ready && master.ready == false)
            {
                trail.Clear();
                Debug.LogError("all G todays the day");
            }
            
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (!ready)
            {
                return;
            }

            //sanity check
            if (direction != null)
                transform.position += projSpeed * Time.fixedDeltaTime * (Vector3)direction;

            //instead of teleporting to each position, this movement is based on time.delta time (change in time between frames)

        }

        
    }
}
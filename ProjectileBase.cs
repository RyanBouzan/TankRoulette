using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace TankRoulette {

    public abstract class ProjectileBase : NetworkBehaviour
    {

        public ProjectileContainer container;

        //make it vector2 to avoid casting
        public Vector2 direction;

        //distance for raycast AND distance that the projectile teleports to each frame
        public float projSpeed;
        public bool team;

        //used to synchronize with the decoration projectile to ensure that they are timed correctly.

        public bool ready;

        public abstract void Initialize(PlayerFireController.LiveFireData lfd, PlayerFireController pfc);

        public abstract void Initialize(PlayerFireController.LiveFireData lfd, EnemyTank et);


    }
}


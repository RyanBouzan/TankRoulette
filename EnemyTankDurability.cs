using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

//script to manage enemy tank durability.
//TODO: add modules
namespace TankRoulette
{
    public class EnemyTankDurability : NetworkBehaviour
    {
        [SerializeField]
        private EnemyTank enemy;

        public float _Integrity;

        //assigned in inspector
        [SerializeField]
        private BoxCollider2D FrontPlate, SideLeft, SideRight, RearPlate;



        //front, left, right, rear

        public float[] ERA_Segments = new float[4];


        //first attempt at modules

        public float engine = 1.0f;

        public float tracks = 1.0f;

        public float gun = 1.0f;




        public void ProjectileImpact(Collider2D ArmorPlate, float damage)
        {

            //experiment: immediately jump the enemy react counter to max - 25.
            //this likely quickens the response, but also gives the player a 25 tick grace period
            //if the counter was above the threshold.
            enemy.reactionTimeCounter = enemy.reactionTime_Max - 25;

            //Debug.LogWarning("incoming damage constant is: " + damage);
            //front
            if ((ArmorPlate as BoxCollider2D).Equals(FrontPlate))
            {


                if (ERA_Segments[0] >= damage)
                {
                    ERA_Segments[0] -= damage;
                    //Debug.Log("damaged blocked by ERA");
                }
                else if (ERA_Segments[0] > 0)
                {
                    damage -= ERA_Segments[0];
                    ERA_Segments[0] = -1;
                    //Debug.Log("damage after era: " + damage);
                    UpdateIntegrity(damage * 0.2f);
                }
                else
                {
                    //Debug.LogError("took: " + damage * 0.2f + " damage");
                    UpdateIntegrity(damage * 0.2f);
                }
            }
            //side left
            else if ((ArmorPlate as BoxCollider2D).Equals(SideLeft))
            {
                if (ERA_Segments[1] >= damage)
                {
                    ERA_Segments[1] -= damage;
                    //Debug.Log("damaged blocked by ERA");

                }
                else if (ERA_Segments[1] > 0)
                {
                    damage -= ERA_Segments[1];
                    ERA_Segments[1] = -1;
                    UpdateIntegrity(damage * .5f);
                }
                else
                {
                    //Debug.LogError("took: " + damage * 0.5f + " damage");
                    UpdateIntegrity(damage * 0.5f);
                }


            }
            else if ((ArmorPlate as BoxCollider2D).Equals(SideRight))
            {
                if (ERA_Segments[2] >= damage)
                {
                    ERA_Segments[2] -= damage;
                    //Debug.Log("damaged blocked by ERA");

                }
                else if (ERA_Segments[2] > 0)
                {
                    damage -= ERA_Segments[2];
                    ERA_Segments[2] = -1;
                    UpdateIntegrity(damage * .5f);
                }
                else
                {
                    //Debug.LogError("took: " + damage * 0.5f + " damage");
                    UpdateIntegrity(damage * 0.5f);
                }

            }
            else if ((ArmorPlate as BoxCollider2D).Equals(RearPlate))
            {
                if (ERA_Segments[3] >= damage)
                {
                    ERA_Segments[3] -= damage;
                    //Debug.Log("damaged blocked by ERA");

                }
                else if (ERA_Segments[3] > 0)
                {
                    damage -= ERA_Segments[3];
                    ERA_Segments[3] = -1;
                    UpdateIntegrity(damage * 1f);
                }
                else
                {
                    //Debug.LogError("took: " + damage * 1f + " damage");
                    UpdateIntegrity(damage * 1f);
                }

            }
            else
            {
                Debug.LogError("this statement should not have printed");
            }
        }

        private void UpdateIntegrity(float damage)
        {
            _Integrity -= damage;
        }

    }
}

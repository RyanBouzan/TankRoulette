using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using TMPro;
using FishNet.Object.Synchronizing;

namespace TankRoulette
{
    public class ObjectiveObject : NetworkBehaviour
    {
        [SerializeField]
        private MultiplayerManagerV3 MM3;

        public GameRoundManager GRM;

        public int CaptureProgress = 0;

        public int FullCapture = 75;

        public bool captured = false;

        public bool finished = false;

        [SerializeField]
        private int boost;

        [SyncVar]
        private int counter = 0;

        [SerializeField]
        private TextMeshPro text;

        [SerializeField]
        private List<GameObject> tanksInsideObjective = new();


        public int award;

        public override void OnStartServer()
        {
            base.OnStartServer();
            MM3 = GameObject.FindGameObjectWithTag("GameController").GetComponent<MultiplayerManagerV3>();
        }

        // Update is called once per frame
        private void FixedUpdate()
        {

            if (CaptureProgress >= FullCapture)
            {
                if (!captured)
                {
                    captured = true;
                    AwardCapture(award);

                    StartCoroutine(SelfDestruct(10));
                }
                return;
            }

            if (base.IsServer)
            {
                if (GRM.enemiesRemaining == 0)
                {
                    boost = 5;
                }
                else
                {
                    boost = 1;
                }
                counter += tanksInsideObjective.Count * boost;
                CaptureProgress = counter / 20;
                ShowandUpdateProgress(this, CaptureProgress);
            }


        }

        [ObserversRpc(RunLocally = true)]
        private void AwardCapture(int award)
        {
            Debug.LogWarning("awarding capture bounty");

            LocalConnection.FirstObject.GetComponent<ClientTankManager>().shop.coin += award;
        }

        IEnumerator SelfDestruct(int delay)
        {
            yield return new WaitForSeconds(delay);
            finished = true;
            yield return new WaitForSeconds(1f);
            ServerManager.Despawn(this.gameObject);
        }

        [ObserversRpc(RunLocally = true)]
        public void ShowandUpdateProgress(ObjectiveObject obj, int progress)
        {
            if (progress <= obj.FullCapture)
                text.text = "Progress: " + (progress).ToString();
            else
                text.text = "Captured!";
            if (base.IsClientOnly)
            {
                obj.CaptureProgress = progress;
            }
        }

        //check if the list of tanks inside the objective does not already contain the new tank,
        //and that the collision is actually a player tank.
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (base.IsServer)
            {
                if (!tanksInsideObjective.Contains(collision.gameObject)
                    && collision.gameObject.layer == LayerMask.NameToLayer("PlayerLayer"))
                    tanksInsideObjective.Add(collision.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (base.IsServer)
            {
                if (tanksInsideObjective.Contains(collision.gameObject))
                    tanksInsideObjective.Remove(collision.gameObject);
            }
        }
    }
}
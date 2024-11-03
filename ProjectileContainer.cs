using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using Unity.VisualScripting;

namespace TankRoulette
{
    public class ProjectileContainer : NetworkBehaviour
    {
        public ProjectileRealCollision proj;
        public DecorationProjectile decor;
        public bool activated;
        
        /// <summary>
        /// This method detaches the projectile and decoration from their parent objects and then destroys the current game object.
        /// </summary>
        public void Shed(bool asServer){
            proj.GetComponent<NetworkObject>().SetParent((NetworkObject)null);
            proj.GetComponent<NetworkObject>().SetParent((NetworkObject)null);
            proj.transform.parent = null;
            decor.transform.parent = null;
            if(!asServer)
            ServerShed(gameObject);
            Destroy(gameObject);
        }
        [ServerRpc(RequireOwnership = false)]
        private void ServerShed(GameObject go)
        {
            ServerManager.Despawn(go);
        }
       
        // [ServerRpc(RunLocally = false)]
        // private void ServerFire(PlayerFireController pfc, PlayerFireController.LiveFireData lfd)
        // {
        //     proj.direction = lfd._direction;
        //     proj.damage = lfd._damage;
        //     proj.explosionDecalScale = lfd._explosionDecalScale;
        //     proj.gameObject.SetActive(true);
        //     proj.explosionDecalRotation = lfd._bulletSpawn.rotation.eulerAngles.z - 90;
        //     proj.transform.SetPositionAndRotation(lfd._bulletSpawn.position, lfd._bulletSpawn.rotation);
            
        //     //initial pos is the current position
        //     proj.initialPosition = transform.position;
        //     //take the current position and add the direction vector. multiply by max distance to get endpoint
        //     proj.endpoint = proj.initialPosition + (lfd._direction * lfd._maxDistance);
        //     // Release the decoration projectile
        //     decor.direction = lfd._direction;
        //     decor.gameObject.SetActive(true);

        //     activated = true;

        //     transform.parent = null;
        //     GetComponent<NetworkObject>().SetParent(NetworkObject.AsUnityNull());
            
        //     Destroy(gameObject);
        // }

        // public void Fire(PlayerFireController pfc, PlayerFireController.LiveFireData lfd)
        // {
        //     Debug.LogWarning("Firing");

        //     ServerFire(pfc, lfd);
        // }
        // [ObserversRpc(RunLocally = true)]
        // public void Fire(EnemyTank eft, EnemyTank.LiveFireData lfd)
        // {

        //     proj.direction = lfd._direction;
        //     proj.damage = lfd._damage;
        //     proj.explosionDecalScale = lfd._explosionDecalScale;
        //     proj.gameObject.SetActive(true);
        //     proj.explosionDecalRotation = lfd._bulletSpawn.rotation.eulerAngles.z - 90;
        //     proj.transform.SetPositionAndRotation(lfd._bulletSpawn.position, lfd._bulletSpawn.rotation);
        //     proj.team = false;
        //     //initial pos is the current position
        //     proj.initialPosition = transform.position;
        //     //take the current position and add the direction vector. multiply by max distance to get endpoint
        //     proj.endpoint = proj.initialPosition + (lfd._direction * lfd._maxDistance);
        //     // Release the decoration projectile
        //     decor.team = false;
        //     decor.direction = lfd._direction;
        //     decor.gameObject.SetActive(true);

        //     proj.ready = true;
        //     decor.ready = true;
        //     activated = true;
        // }

    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace TankRoulette
{
    public class EnemyStateDebug : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro textMeshPro;

        [SerializeField]
        private Transform parent;

        private EnemyTank info;

        // Start is called before the first frame update
        void Start()
        {
            textMeshPro = GetComponent<TextMeshPro>();
            info = transform.parent.GetComponent<EnemyTank>();
        }

        // Update is called once per frame
        void Update()
        {
            string fire = "canfire: " + info.canFire.ToString();
            string counter = "react: " + info.reactionTimeCounter.ToString();
            string attack = "attack: " + info.attackTimeCounter.ToString();
            string state = info.state.ToString();
            string target;
            string distanceToTarget = "";

            if (info.Target == null)
            {
                target = "No target";
            }
            else
            {
                target = info.Target.gameObject.name;
                distanceToTarget = Vector2.Distance(transform.position, info.Target.transform.position).ToString();
            }
            textMeshPro.text = fire + "\n" + counter + "\n" + attack + "\n" + state + "\n Target: " + target + "\n " + distanceToTarget;
            //textMeshPro.text = parent.GetComponent<EnemyTank>().state.ToString();
            transform.rotation = Quaternion.identity;

        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBuffer : MonoBehaviour
{
    public Transform enemy;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if(enemy != null)
        {
            transform.position = enemy.transform.position;
        }
    }
}

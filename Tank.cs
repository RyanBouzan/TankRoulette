using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class Tank : NetworkBehaviour
{


    public Rigidbody2D tankRB;



    //health represented as int
  

    public float moveSpeed;

    public float tankRotationSpeed;


    // Start is called before the first frame update
    void Start()
    {
        tankRB = gameObject.GetComponent<Rigidbody2D>();
    }


   


}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script is attatched to all shop game objects, not the shop manager!


public class Shop : MonoBehaviour
{
    public bool playerInShop = false;

    public BoxCollider2D shopCollider;

    public void OnTriggerStay2D(Collider2D collision)
    {


        //using this judiciously. simple fix to prevent trophy colliders activating the shop
        if(collision.isTrigger)
        {
            return;
        }


                                //MUST update player tank tag.
                                //it has to be the local one or else
                                //the other players tank will cause the shop
                                //to open for you
        if (collision.CompareTag("LocalPlayerTank"))
        {
            playerInShop = true;
        }

    }

    public void OnTriggerExit2D(Collider2D collision)
    {

        if (collision.isTrigger)
        {
            return;
        }


        if (collision.CompareTag("LocalPlayerTank"))
        {
            playerInShop = false;
        }
    }


}

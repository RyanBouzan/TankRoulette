using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PlayerPurcahsedUI : NetworkBehaviour
{
    public int position = 0;
    [SerializeField] List<GameObject> possibpositions = new();
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
        {
            this.enabled = false;
        }
    }



    public void AddSprite(string spritename)
    {
        
        Texture2D sprite = null;
        if (spritename.Equals("AP_Sprite"))
        {
            Debug.LogWarning(spritename);
            sprite = Resources.Load("Sprites/AP Shell UI") as Texture2D;
        }


        possibpositions[position].GetComponent<Image>().color = Color.white;

        //possibpositions[position].GetComponent<Image>().sprite;
        position++;
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopItem
{
    string itemName;
    int price;
    public bool beenBought=false;
    
    public ShopItem(string itemName, int price)
    {
        this.itemName = itemName;
        this.price = price;
    }
    public string getname()
    {
        return itemName;
    }
    public int getprice() 
    { 
        return price; 
    }
    public bool buyItem(int points)
    {
        if (beenBought) return true;
        if (points > price)
        {
            beenBought = true;
            return true;
        }
        return false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileState : MonoBehaviour {
    private bool watered;
    private System.DateTime wateredLast;
    private System.TimeSpan waterBound = new System.TimeSpan(24, 0, 0);

	// Use this for initialization
	void Start () {
        IsWatered = false;
	}
	
	// Update is called once per frame
	void Update () {
        if (LastWatered() > waterBound && IsWatered)
        {
            IsWatered = false;
            //swap to withered tile
        }
        else if (LastWatered() < waterBound && !IsWatered)
        {
            IsWatered = true;
            // swap to watered tile
        }
	}

    //Property that checks if tile is watered
    public bool IsWatered
    {
        get { return watered; }
        set { watered = value; }
    }

    //method to change tile to watered state
    public void Water()
    {
        wateredLast = System.DateTime.Now.ToUniversalTime();
        if (!IsWatered)
        {
            IsWatered = true;
            //swap watered tile
        }

    }

    private System.TimeSpan LastWatered()
    {
        return wateredLast.Subtract(System.DateTime.Now.ToUniversalTime());
    }

}

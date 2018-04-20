using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileState : MonoBehaviour
{
    private int plantStage, plantType;
    private string currentPlantModel;
    private bool watered, hasPlant;
    private System.DateTime wateredLast, plantedLast;
    private static System.TimeSpan waterBound = new System.TimeSpan(0, 0, 15);
    private static System.TimeSpan sproutDuration = new System.TimeSpan(0, 0, 30);
    private static System.TimeSpan midlingDuration = new System.TimeSpan(0, 0, 45);

    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (HasPlant)
        {
            if (DeltaLastWatered().CompareTo(waterBound) >= 0 && IsWatered)
            {
                this.IsWatered = false;
                _ShowAndroidToastMessage("Plant just withered");
            }
            else if (DeltaLastWatered().CompareTo(waterBound) < 0 && !IsWatered)
            {
                IsWatered = true;
            }
            if (!plantType.Equals("Withered"))
            {
                UpdatePlantStage();
            }
            
        }
    }

    public TileState(string currentPlantModel, int plantType, bool watered, bool hasPlant, System.DateTime wateredLast, System.DateTime plantedLast)
    {
        this.currentPlantModel = currentPlantModel;
        this.plantType = plantType;
        this.watered = watered;
        this.hasPlant = hasPlant;
        this.wateredLast = wateredLast;
        this.plantedLast = plantedLast;
    }

    //Property that checks if tile is watered
    public bool IsWatered
    {
        get { return watered; }
        set { watered = value; }
    }

    //property to check if a tile is holding a plant
    public bool HasPlant
    {
        get { return hasPlant; }
        set { hasPlant = value; }
    }

    //property that contains the name of the model of the plant stored in the tile
    public string PlantModel
    {
        get { return currentPlantModel; }
        set { currentPlantModel = value; }
    }

    //plant model != plant type all the time
    //this is because of the sproutling and midling phases
    //plant type is the type of seed planted
    public int PlantType
    {
        get { return plantType; }
        set { plantType = value; }
    }

    //property indicating plant stage
    // 0->seed/sprout; 1->midling; 2->full-grown plant
    public int PlantStage
    {
        get { return plantStage; }
        set { plantStage = value; }
    }

    //Just needed for saving time progress
    public System.DateTime WateredLast
    {
        get { return wateredLast;  }
        set { wateredLast = value;  }
    }

    //Also needed for saving time progress
    public System.DateTime PlantedLast
    {
        get { return plantedLast; }
        set { plantedLast = value; }
    }

    //removes plant from the tile
    public void Harvest(string currentPlantModel)
    {
        this.HasPlant = false;
        this.IsWatered = false;
        this.PlantStage = 0;
    }

    //method to plant something
    public void PutPlant(int typeToPlant, string modelInTile)
    {
        plantedLast = System.DateTime.Now.ToUniversalTime();
        this.currentPlantModel = modelInTile;
        this.plantType = typeToPlant;
        this.HasPlant = true;
        plantStage = 0;
        this.Water();
    }

    //method to change tile to watered state
    public void Water()
    {
        wateredLast = System.DateTime.Now.ToUniversalTime();
        if (!this.IsWatered)
        {
            this.IsWatered = true;
        }

    }

    private void UpdatePlantStage()
    {
        System.TimeSpan plantDuration = System.DateTime.Now.ToUniversalTime().Subtract(plantedLast);
        if (plantDuration.CompareTo(sproutDuration) >= 0)
        {
            plantStage = 1;
        }
        else if (plantDuration.CompareTo(midlingDuration) >= 0)
        {
            plantStage = 2;
        }
    }

    //returns the difference between the time last watered and the current time
    private System.TimeSpan DeltaLastWatered()
    {
        return System.DateTime.Now.ToUniversalTime().Subtract(wateredLast);
    }

    /// <summary>
    /// Show an Android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    private void _ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                    message, 0);
                toastObject.Call("show");
            }));
        }
    }

}

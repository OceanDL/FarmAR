using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileState : MonoBehaviour
{
    private bool watered, empty;
    private System.DateTime wateredLast;
    private System.TimeSpan waterBound = new System.TimeSpan(0, 0, 5);

    // Use this for initialization
    void Start()
    {
        empty = false;
        watered = true;
        Water();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsEmpty)
        {
            //_ShowAndroidToastMessage("Plant just withered");
            if (LastWatered().CompareTo(waterBound) > 0 && IsWatered)
            {
                //_ShowAndroidToastMessage("Plant just withered");
                IsWatered = false;
            }
            else if (LastWatered().CompareTo(waterBound) < 0 && !IsWatered)
            {
                
                IsWatered = true;
            }
        }
    }

    //Property that checks if tile is watered
    public bool IsWatered
    {
        get { return watered; }
        set { watered = value; }
    }

    //Property that checks if tile is watered
    public bool IsEmpty
    {
        get { return empty; }
        set { empty = value; }
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
    //was getting weird error with .subtract()
    private System.TimeSpan LastWatered()
    {
        return System.DateTime.Now.ToUniversalTime().TimeOfDay;
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

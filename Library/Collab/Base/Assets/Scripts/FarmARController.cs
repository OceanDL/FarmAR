//Moddification of HelloAR default script to be used in FarmAR.'
//Script is currently main controller for interation with the AR scene.
//Last Update 3/1/18


namespace GoogleARCore.FarmAR
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.UI;

#if UNITY_EDITOR
    using Input = InstantPreviewInput;
#endif

    /// <summary>
    /// Controls the FarmAR scene.
    /// </summary>
    public class FarmARController : MonoBehaviour
    {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera image (i.e. AR background).
        /// </summary>
        public Camera FirstPersonCamera;

        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject TrackedPlanePrefab;

        /// <summary>
        /// A model to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject FarmPrefab;

        /// <summary>
        /// A gameobject parenting UI for displaying the "searching for planes" snackbar.
        /// </summary>
        public GameObject SearchingForPlaneUI;

        public GameObject PlaceFarmUI;

        public GameObject TileUI;

        /// <summary>
        /// A list to hold new planes ARCore began tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();

        /// <summary>
        /// A list to hold all planes ARCore is tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<TrackedPlane> m_AllPlanes = new List<TrackedPlane>();

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error, otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        /// <summary>
        /// Dictionary of key: names, values: game model paths
        /// </summary>
        private Dictionary<string, string> tileModels = new Dictionary<string, string>();


        private bool farmCreated = false;

        private GameObject selectedTile = null;

        Material mSelected = null;

        Material mOriginal = null;

        GameObject farmObject = null;

        private float initialDistance;
        private float newDistance;
        private const string modelPath = "FarmAR Assets/Prefabs/";

        /// <summary>
        /// Swaps prefabs for game object
        /// Copies components and properties
        /// </summary>
        public void Swap(GameObject tile, string swapToTile)
        {
            string value = "";
            if (tileModels.TryGetValue(swapToTile, out value))
            {
                GameObject newTile = (GameObject)Instantiate(Resources.Load(value));
                var components = tile.GetComponents<Component>();
                foreach(Component comp in components)
                {
                    Type t = comp.GetType();
                    var properties = t.GetProperties();
                    newTile.AddComponent(t);
                    var newComp = newTile.GetComponent(t);
                    GetCopyOf(comp, t, newComp);
                }
                newTile.transform.parent = tile.transform.parent;
                newTile.transform.localPosition = tile.transform.localPosition;
                newTile.transform.localRotation = tile.transform.localRotation;
                newTile.transform.localScale = tile.transform.localScale;
                DestroyImmediate(tile);
                tile = newTile;
            }

        }

        public void GetCopyOf(Component comp, Type other, Component newComp)
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            newComp = comp;
        }

        public void Start()
        {
            tileModels.Add("Seeds", modelPath + "model");
            tileModels.Add("Flowers", modelPath + "model 1");
            tileModels.Add("Grass", modelPath + "model 2");
            tileModels.Add("Empty", modelPath + "model 3");
            tileModels.Add("Pineapple", modelPath + "model 4");
            tileModels.Add("Carrots", modelPath + "model 5");

        }
        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            _QuitOnConnectionErrors();

            // Check that motion tracking is tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                if (!m_IsQuitting && Session.Status.IsValid())
                {
                    SearchingForPlaneUI.SetActive(true);
                }

                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
            Session.GetTrackables<TrackedPlane>(m_NewPlanes, TrackableQueryFilter.New);
            for (int i = 0; i < m_NewPlanes.Count; i++)
            {
                // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                // coordinates.
                GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                    transform);
                planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(m_NewPlanes[i]);
            }

            // Disable the snackbar UI when no planes are valid.
            Session.GetTrackables<TrackedPlane>(m_AllPlanes);
            bool showSearchingUI = true;
            bool showPlaceFarmUI = false;
            bool showTileUI = false;
            if (selectedTile != null)
            {
                showTileUI = true;
            }
            for (int i = 0; i < m_AllPlanes.Count; i++)
            {
                if (m_AllPlanes[i].TrackingState == TrackingState.Tracking)
                {
                    showSearchingUI = false;
                    showPlaceFarmUI = true;
                    break;
                }
            }
            if (farmCreated)
            {
                showPlaceFarmUI = false;
            }

            PlaceFarmUI.SetActive(showPlaceFarmUI);
            SearchingForPlaneUI.SetActive(showSearchingUI);
            TileUI.SetActive(showTileUI);

            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                TrackableHitFlags.FeaturePointWithSurfaceNormal;

            if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit) && !farmCreated)
            {


                farmObject = Instantiate(FarmPrefab, hit.Pose.position, hit.Pose.rotation);

                // Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
                // world evolves.
                var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                //Pretty sure we don't need this. Probably. 
                //// Andy should look at the camera but still be flush with the plane.
                //if ((hit.Flags & TrackableHitFlags.PlaneWithinPolygon) != TrackableHitFlags.None)
                //{
                //    // Get the camera position and match the y-component with the hit position.
                //    Vector3 cameraPositionSameY = FirstPersonCamera.transform.position;
                //    cameraPositionSameY.y = hit.Pose.position.y;

                //    // Have Andy look toward the camera respecting his "up" perspective, which may be from ceiling.
                //    farmObject.transform.LookAt(cameraPositionSameY, farmObject.transform.up);
                //}

                // Make Farm model a child of the anchor.
                farmObject.transform.parent = anchor.transform;

                farmCreated = true;
            }

            //Create ray and raycasthit for tile selection
            Ray ray = Camera.current.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit tileHit = new RaycastHit();

            //Selects tile and changes material, currently uses color.yellow as temporary highlighter.
            if (farmCreated && Physics.Raycast(ray, out tileHit))
            {
                if (selectedTile != tileHit.collider.gameObject && selectedTile != null)
                {
                    mSelected.color = mOriginal.color;
                }
                selectedTile = tileHit.collider.gameObject;
                mSelected = selectedTile.GetComponent<Renderer>().material;
                if (mSelected.color != Color.yellow)
                {
                    //_ShowAndroidToastMessage("Original Material Saved");
                    mOriginal = new Material(mSelected);
                }

                mSelected.color = Color.yellow;
            }

            if (TileUI.activeSelf)
            {
                Button[] buttonList = TileUI.GetComponentsInChildren<Button>();
                Button harvesttButton = buttonList[0];
                Button waterButton = buttonList[1];
                Button plantButton = buttonList[2];
                String prefabToSwap = "Flowers";

                plantButton.onClick.AddListener(() => { Swap(selectedTile, prefabToSwap); });


            }



            //    //Scale Farm by pinching
            //    if (Input.touchCount == 2 && farmCreated)
            //    {
            //        if (Input.touchCount >= 2 && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved))
            //        {
            //            Vector2 touch1 = Input.GetTouch(0).position;
            //            Vector2 touch2 = Input.GetTouch(1).position;

            //            newDistance = (touch1 - touch2).sqrMagnitude;
            //            float changeInDistance = newDistance - initialDistance;
            //            float percentageChange = changeInDistance / initialDistance;


            //            Vector3 newScale = farmObject.transform.localScale;
            //            newScale += percentageChange * newScale;

            //            farmObject.transform.localScale = newScale;

            //        }

            //    }
        }




        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
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
}

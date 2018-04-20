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

        public GameObject EscapeUI;

        public GameObject InventoryUI;

        public GameObject CurrencyUI;

        public GameObject CurrencyImage;


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
        /// A list of the active tiles
        /// </summary>
        private GameObject[] activeTiles;

        /// <summary>
        /// Dictionary of key: names, values: game model paths
        /// 0-> grass, 1-> flowers, 2-> carrots, 3-> pineapple, 4-> seeds, 5-> empty, 6-> wither, 7-> midling
        /// </summary>
        private Dictionary<int, string> tileModels;

        private Dictionary<int, int> plantValues;

        private Dictionary<int, int> seedValues;


        private bool farmCreated = false;

        private GameObject selectedTile = null;

        Material mSelected = null;

        Material mOriginal = null;

        GameObject farmObject = null;

        private float initialDistance;
        private float newDistance;
        private const string modelPath = "FarmAR Assets/Prefabs/";
        private int currency;



        public void Start()
        {
            /// <summary>
            /// Dictionary of key: names, values: game model paths
            /// 0-> grass, 1-> flowers, 2-> carrots, 3-> pineapple, 4-> seeds, 5-> empty, 6-> wither, 7-> midling
            /// </summary>
            tileModels = new Dictionary<int, string>();
            tileModels.Add(4, modelPath + "model");
            tileModels.Add(1, modelPath + "model 1");
            tileModels.Add(0, modelPath + "model 2");
            tileModels.Add(5, modelPath + "model 3");
            tileModels.Add(3, modelPath + "model 4");
            tileModels.Add(2, modelPath + "model 5");
            tileModels.Add(6, modelPath + "model 6");
            tileModels.Add(7, modelPath + "model 7");

            plantValues = new Dictionary<int, int>();
            plantValues.Add(0, 2);
            plantValues.Add(1, 4);
            plantValues.Add(3, 10);
            plantValues.Add(2, 6);
            plantValues.Add(6, 0);

            seedValues = new Dictionary<int, int>();
            seedValues.Add(0, 1);
            seedValues.Add(1, 2);
            seedValues.Add(2, 3);
            seedValues.Add(3, 4);

            activeTiles = GameObject.FindGameObjectsWithTag("Tile");


            if (PlayerPrefs.GetInt("Currency", -1) == -1)
            {
                _ShowAndroidToastMessage("Initializing currency");
            }

            currency = PlayerPrefs.GetInt("Currency", 500);
            PlayerPrefs.SetInt("Currency", currency);

            //currency = 500000;

            Text currencyValue = CurrencyImage.GetComponentInChildren<Text>();
            currencyValue.text = currency.ToString();
            _ShowAndroidToastMessage(currency.ToString());
            bool farmSaved = false;
            if (PlayerPrefs.GetInt("farmSaved") == 1)
            {
                farmSaved = true;
            }
            ReloadFarm(farmSaved);


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
            bool showEscapeUI = false;
            bool showCurrencyUI = false;
            if (selectedTile != null)
            {
                showEscapeUI = true;
                showTileUI = true;
                showCurrencyUI = true;
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
            EscapeUI.SetActive(showEscapeUI);
            CurrencyUI.SetActive(showCurrencyUI);



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

            try
            {
                CheckWaterLevel();
                _ShowAndroidToastMessage("Check Water OK!");
            }
            catch (Exception e)
            {
                //_ShowAndroidToastMessage("Water code crashed");
            }
            try
            {
                CheckPlantStage();
                _ShowAndroidToastMessage("Check Plant OK!");
            }
            catch (Exception e)
            {
                //_ShowAndroidToastMessage("Check plant crashed");
            }

            if (TileUI.activeSelf)
            {

                Button[] buttonList = TileUI.GetComponentsInChildren<Button>();
                Button harvestButton = buttonList[0];
                Button waterButton = buttonList[1];
                Button plantButton = buttonList[2];

                StorageInventory storage = plantButton.GetComponent<StorageInventory>();



                plantButton.onClick.AddListener(() =>
                {

                    //                    Plant(selectedTile, -1);
                    storage.OpenInventory();


                });
                waterButton.onClick.AddListener(() =>
                {
                    TileState oldState = selectedTile.GetComponent<TileState>();
                    TileState copy = null;
                    GetCopyOf(oldState, copy, oldState.GetType());
                    DestroyImmediate(oldState);
                    TileState newState = selectedTile.AddComponent<TileState>();
                    GetCopyOf(copy, newState, copy.GetType());
                    newState.Water();
                });
                harvestButton.onClick.AddListener(() => { Harvest(selectedTile); });


                if (InventoryUI.activeSelf)
                {
                    Button[] seedButtons = InventoryUI.GetComponentsInChildren<Button>();
                    int i = 0;


                    seedButtons[0].onClick.AddListener(() =>
                     {
                         try
                         {
                             _ShowAndroidToastMessage("Selected item at " + 0);
                             Plant(selectedTile, 0);
                             storage.OpenInventory();
                         }
                         catch (Exception e)
                         {
                             _ShowAndroidToastMessage("Inventory Button listener crashed");
                         }
                     });

                    seedButtons[1].onClick.AddListener(() =>
                    {
                        try
                        {
                            _ShowAndroidToastMessage("Selected item at " + 1);
                            Plant(selectedTile, 1);
                            storage.OpenInventory();
                        }
                        catch (Exception e)
                        {
                            _ShowAndroidToastMessage("Inventory Button listener crashed");
                        }
                    });

                    seedButtons[2].onClick.AddListener(() =>
                    {
                        try
                        {
                            _ShowAndroidToastMessage("Selected item at " + 2);
                            Plant(selectedTile, 2);
                            storage.OpenInventory();
                        }
                        catch (Exception e)
                        {
                            _ShowAndroidToastMessage("Inventory Button listener crashed");
                        }
                    });

                    seedButtons[3].onClick.AddListener(() =>
                    {
                        try
                        {
                            _ShowAndroidToastMessage("Selected item at " + 3);
                            Plant(selectedTile, 3);
                            storage.OpenInventory();
                        }
                        catch (Exception e)
                        {
                            _ShowAndroidToastMessage("Inventory Button listener crashed");
                        }
                    });
                }

            }



            if (EscapeUI.activeSelf)
            {
                Button[] buttonList = EscapeUI.GetComponentsInChildren<Button>();
                Button escapeButton = buttonList[0];

                escapeButton.onClick.AddListener(() =>
                {
                    mSelected.color = mOriginal.color;
                    selectedTile = null;
                });
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

        //puts a new plant in the passed in tile
        //displays toast message if there is already a plant
        //
        //still needs inventory UI implementation
        public void Plant(GameObject tile, int plantID)
        {
            currency = 999;
            if (!tile.GetComponent<TileState>().HasPlant)
            {
                if (currency >= seedValues[plantID])
                {
                    currency -= seedValues[plantID];
                    Swap(tile, 4, plantID, 1);
                }
                else
                {
                    _ShowAndroidToastMessage("Not enough currency yo. Have " + currency + ", need " + seedValues[plantID]);
                }

            }
        }

        //removes plant from passed in tile
        //displays toast message if there is no plant in the tile
        public void Harvest(GameObject tile)
        {
            if (tile.GetComponent<TileState>().HasPlant)
            {
                Swap(tile, 5, -1, 2);
                currency += plantValues[tile.GetComponent<TileState>().PlantType];
            }
        }

        /// <summary>
        /// Swaps prefabs for game object
        ///
        /// action to take: 1-> put in new plant
        ///                 2-> harvest plant
        ///                 3-> grow new plant
        ///                 4-> wither plant
        /// </summary>
        public void Swap(GameObject tile, int plantModel, int plantType, int action)
        {
            string value;

            if (tileModels.TryGetValue(plantModel, out value))
            {
                //make new tile, copy components of old tile
                GameObject newTile = (GameObject)Instantiate(Resources.Load(value));

                //copy box collider
                newTile.AddComponent<BoxCollider>();
                GetCopyOf(tile.GetComponent<BoxCollider>(), newTile.GetComponent<BoxCollider>(), tile.GetComponent<BoxCollider>().GetType());
                newTile.GetComponent<BoxCollider>().size = tile.GetComponent<BoxCollider>().size;
                newTile.GetComponent<BoxCollider>().center = tile.GetComponent<BoxCollider>().center;

                //copy mesh renderer and destory the old one
                MeshRenderer m = newTile.AddComponent<MeshRenderer>() as MeshRenderer;
                GetCopyOf(tile.GetComponent<MeshRenderer>(), m, tile.GetComponent<MeshRenderer>().GetType());
                DestroyImmediate(tile.GetComponent<MeshRenderer>());

                TileState newTileState = newTile.AddComponent<TileState>();

                switch (action)
                {
                    //put new plant
                    case 1:
                        newTileState.PutPlant(plantType, value);
                        break;
                    //harvest plant
                    case 2:
                        newTileState.Harvest(value);
                        break;
                    //growth
                    case 3:
                        GetCopyOf(tile.GetComponent<TileState>(), newTileState, newTileState.GetType());
                        newTileState.PlantModel = value;
                        break;
                    //wither
                    case 4:
                        newTileState.PlantModel = value;
                        newTileState.IsWatered = false;
                        newTileState.WateredLast = tile.GetComponent<TileState>().WateredLast;
                        break;
                    default:
                        break;
                }

                newTile.transform.parent = tile.transform.parent;
                newTile.transform.localPosition = tile.transform.localPosition;
                newTile.transform.localRotation = tile.transform.localRotation;
                newTile.transform.localScale = tile.transform.localScale;
                DestroyImmediate(tile);
                tile = newTile;



            }

        }

        private static void GetCopyOf(Component comp, Component newComp, Type other)
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
                    catch
                    {
                        _ShowAndroidToastMessage("bad copy, yo");
                    } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            newComp = comp;
        }

        private void CheckWaterLevel()
        {
            //search for objects that are watered but still have withered prefab
            _ShowAndroidToastMessage("Entered CheckWaterLevel()");
            //activeTiles = GameObject.FindGameObjectsWithTag("Tile");
            _ShowAndroidToastMessage("activeTiles size = " + activeTiles.Length);
            foreach (GameObject other in activeTiles)
            {
                if ((!other.GetComponent<TileState>().IsWatered) && other.GetComponent<TileState>().HasPlant &&
                    (!other.GetComponent<TileState>().PlantModel.Equals("model 6")))
                {
                    Swap(other, 6, 6, 4);
                }
            }

        }

        private void CheckPlantStage()
        {
            //activeTiles = GameObject.FindGameObjectsWithTag("Tile");
            foreach (GameObject tile in activeTiles)
            {
                if (tile.GetComponent<TileState>().HasPlant)
                {
                    int plantType = tile.GetComponent<TileState>().PlantType;
                    string plantModel = tile.GetComponent<TileState>().PlantModel;
                    int plantStage = tile.GetComponent<TileState>().PlantStage;
                    if (plantStage == 2 && plantModel.Equals(modelPath + "model 7"))
                    {
                        Swap(tile, plantType, plantType, 3);
                    }
                    else if (plantStage == 1 && plantModel.Equals(modelPath + "model 4"))
                    {
                        Swap(tile, 7, plantType, 3);
                    }
                }
            }
        }


        public void OnPickedSeed(Item item)
        {
            // item unique ID can be referenced by item.itemID
            _ShowAndroidToastMessage("Picked " + item.itemName);
        }

        public void ReloadFarm(bool farmSaved)
        {
            if (farmSaved)
            {
                foreach (GameObject tile in activeTiles)
                {
                    Swap(tile, PlayerPrefs.GetInt(tile.name + "pModel"), PlayerPrefs.GetInt(tile.name + "pType"), 1);
                    DestroyImmediate(tile.GetComponent<TileState>());
                    TileState state = tile.AddComponent<TileState>();
                    state.PlantStage = PlayerPrefs.GetInt(tile.name + "pStage");
                    state.PlantType = PlayerPrefs.GetInt(tile.name + "pType");
                    state.PlantModel = PlayerPrefs.GetString(tile.name + "pModel");
                    if (PlayerPrefs.GetInt(tile.name + "isWatered") == 1)
                    {
                        state.IsWatered = true;
                    }
                    else
                    {
                        state.IsWatered = false;
                    }
                    if (PlayerPrefs.GetInt(tile.name + "hasPlant") == 1)
                    {
                        state.HasPlant = true;
                    }
                    else
                    {
                        state.HasPlant = false;
                    }
                    state.WateredLast = DateTime.Parse(PlayerPrefs.GetString(tile.name + "wateredLast"));
                    state.PlantedLast = DateTime.Parse(PlayerPrefs.GetString(tile.name + "plantedLast"));
                }
            }
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

        void OnApplicationPause()
        {
            foreach (GameObject tile in activeTiles)
            {
                PlayerPrefs.SetInt(tile.name + "pStage", tile.GetComponent<TileState>().PlantStage);
                PlayerPrefs.SetInt(tile.name + "pType", tile.GetComponent<TileState>().PlantType);
                PlayerPrefs.SetString(tile.name + "pModel", tile.GetComponent<TileState>().PlantModel);
                if (tile.GetComponent<TileState>().IsWatered)
                {
                    PlayerPrefs.SetInt(tile.name + "isWatered", 1);
                }
                else
                {
                    PlayerPrefs.SetInt(tile.name + "isWatered", 0);
                }
                if (tile.GetComponent<TileState>().HasPlant)
                {
                    PlayerPrefs.SetInt(tile.name + "hasPlant", 1);
                }
                else
                {
                    PlayerPrefs.SetInt(tile.name + "hasPlant", 0);
                }
                PlayerPrefs.SetString(tile.name + "wateredLast", tile.GetComponent<TileState>().WateredLast.ToString());
                PlayerPrefs.SetString(tile.name + "plantedlast", tile.GetComponent<TileState>().PlantedLast.ToString());
                PlayerPrefs.SetInt("farmSaved", 1);
            }
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private static void _ShowAndroidToastMessage(string message)
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

using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warlander.Deedplanner.Data;
using Warlander.Deedplanner.Utils;

namespace Warlander.Deedplanner.Logic
{
    public class LoadingManager : MonoBehaviour
    {

        [SerializeField] private GameObject splashRoot = null;
        [SerializeField] private GameObject managersRoot = null;
        [SerializeField] private MultiCamera[] cameras = null;

        [SerializeField] private TMP_Text text = null;
        [SerializeField] private Slider loadingBar = null;
        [SerializeField] private Animator fadeAnimator = null;
        
        private void Start()
        {
            splashRoot.SetActive(true);
            StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            loadingBar.value = 0.0f;
            Debug.Log("Loading data");
            yield return LoadDatabase();
            yield return null;
            Debug.Log("Data loaded");

            loadingBar.value = 0.33f;
            Debug.Log("Creating map");
            yield return LoadMap();
            yield return null;
            Debug.Log("Map created");

            loadingBar.value = 0.66f;
            Debug.Log("Initializing application");
            Initialize();
            yield return null;
            Debug.Log("Application initialized");
            
            loadingBar.value = 1.0f;
            text.text = "Loading complete";

            fadeAnimator.enabled = true;
            Destroy(gameObject);
        }

        private IEnumerator LoadDatabase()
        {
            text.text = "Loading database";
            yield return DataLoader.LoadData();
        }
        
        private IEnumerator LoadMap()
        {
            string mapLocationString = "";
            if (Properties.Web)
            {
                mapLocationString = JavaScriptUtils.GetMapLocationString();
                if (!string.IsNullOrEmpty(mapLocationString))
                {
                    mapLocationString = LoadingUtils.CreateDirectPastebinLink(mapLocationString);
                }
            }

            if (!string.IsNullOrEmpty(mapLocationString))
            {
                text.text = "Loading map from web address";
                yield return GameManager.Instance.LoadMap(new Uri(mapLocationString));
            }
            else if ((Application.isEditor || Debug.isDebugBuild) && DebugManager.Instance.LoadTestMap)
            {
                text.text = "Loading debug map";
                string fullTestMapLocation = Path.Combine(Application.streamingAssetsPath, "./Special/Maps/Test Map.MAP");
                yield return GameManager.Instance.LoadMap(new Uri(fullTestMapLocation));
            }
            else
            {
                text.text = "Creating map";
                GameManager.Instance.CreateNewMap(25, 25);
            }
        }

        private void Initialize()
        {
            text.text = "Initializing application";
            managersRoot.SetActive(true);
            foreach (MultiCamera multiCamera in cameras)
            {
                multiCamera.enabled = true;
            }
        }

    }
}
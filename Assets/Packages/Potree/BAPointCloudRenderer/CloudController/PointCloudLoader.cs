using BAPointCloudRenderer.CloudData;
using BAPointCloudRenderer.Loading;
using System;
using System.Threading;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

namespace BAPointCloudRenderer.CloudController
{
    /// <summary>
    /// Use this script to load a single PointCloud from a directory.
    ///
    /// Streaming Assets support provided by Pablo Vidaurre
    /// </summary>
    public class PointCloudLoader : MonoBehaviour
    {

        /// <summary>
        /// Path to the folder which contains the cloud.js file or URL to download the cloud from. In the latter case, it will be downloaded to a /temp folder
        /// </summary>
        public string cloudPath;

        private string localCachePath;

        /// <summary>
        /// When true, the cloudPath is relative to the streaming assets directory
        /// </summary>
        [HideInInspector]
        public bool streamingAssetsAsRoot = false;

        /// <summary>
        /// The PointSetController to use
        /// </summary>
        public AbstractPointCloudSet setController;

        /// <summary>
        /// True if the point cloud should be loaded when the behaviour is started. Otherwise the point cloud is loaded when LoadPointCloud is loaded.
        /// </summary>
        public bool loadOnStart = true;

        private Node rootNode;

        /// <summary>
        /// Ping object to verify app has connectivity to server
        /// </summary>
        private Ping testConnection;

        public GameObject noInternetNotification;

        private void Awake()
        {
            if (streamingAssetsAsRoot) cloudPath = Application.streamingAssetsPath + "/" + cloudPath;
        }

        void Start()
        {


#if UNITY_ANDROID && !UNITY_EDITOR
            localCachePath = Application.persistentDataPath;
#else
            localCachePath = "/temp/";
#endif

            if (loadOnStart)
            {
                Debug.Log(" ******path : " + cloudPath);
                LoadPointCloud();
            }
            bool isCloudOnline = Uri.IsWellFormedUriString(cloudPath, UriKind.Absolute);
            if (isCloudOnline)
                StartCoroutine(doesPingWork());            
        }

        void checkCacheIsValid(PointCloudMetaData meta)
        {
            var cachePath = meta.cloudPath;
            var lastMetaJsonPath = cachePath + "/lastcloud.js";
            string lastMetaJSON = "";
            if (File.Exists(lastMetaJsonPath))
                lastMetaJSON = File.ReadAllText(lastMetaJsonPath);

            var curJsonPath = cachePath + "/cloud.js";
            string curMetaJSON = "";
            if (File.Exists(curJsonPath))
                curMetaJSON = File.ReadAllText(curJsonPath);

            bool cacheIsValid = lastMetaJSON == curMetaJSON;

            if (!cacheIsValid)
            {
                Debug.LogWarning("should remove cached points !!!");
                var dataPath = cachePath + "/data";
                if (Directory.Exists(dataPath))
                    Directory.Delete(dataPath, true);
                Directory.CreateDirectory(cachePath);

                // overwrite last
                File.WriteAllText(lastMetaJsonPath, curMetaJSON);
            }

        }


        private void LoadHierarchy()
        {
            try
            {
                if (!cloudPath.EndsWith("/"))
                {
                    cloudPath = cloudPath + "/";
                }

                PointCloudMetaData metaData = CloudLoader.LoadMetaData(cloudPath, localCachePath + "/cloud/", false);

                if (metaData.cloudUrl != null)
                {
                    Debug.Log("local cache path is " + metaData.cloudPath);
                    checkCacheIsValid(metaData);
                }
                setController.UpdateBoundingBox(this, metaData.boundingBox_transformed, metaData.tightBoundingBox_transformed);

                rootNode = CloudLoader.LoadHierarchyOnly(metaData);

                setController.AddRootNode(this, rootNode, metaData);

            }
            catch (System.IO.FileNotFoundException ex)
            {
                Debug.LogError("Could not find file: " + ex.FileName);
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                Debug.LogError("Could not find directory: " + ex.Message);
            }
            catch (System.Net.WebException ex)
            {
                Debug.LogError("Could not access web address. " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex + Thread.CurrentThread.Name);
            }
        }

        /// <summary>
        /// Starts loading the point cloud. When the hierarchy is loaded it is registered at the corresponding point cloud set
        /// </summary>
        public void LoadPointCloud()
        {
            if (rootNode == null && setController != null && cloudPath != null)
            {
                setController.RegisterController(this);
                Thread thread = new Thread(LoadHierarchy);
                thread.Name = "Loader for " + cloudPath;
                thread.Start();
            }
            else
                Debug.Log("pb loading pointcloud");
        }

        /// <summary>
        /// Removes the point cloud from the scene. Should only be called from the main thread!
        /// </summary>
        /// <returns>True if the cloud was removed. False, when the cloud hasn't even been loaded yet.</returns>
        public bool RemovePointCloud()
        {
            if (rootNode == null)
            {
                return false;
            }
            setController.RemoveRootNode(this, rootNode);
            rootNode = null;
            return true;
        }

        IEnumerator doesPingWork()
        {
            noInternetNotification.SetActive(false);

            string pathToCloudJS = cloudPath + "cloud.js";
            Debug.Log("DL : " + pathToCloudJS + " to verify connectivity");
            using (UnityWebRequest www = UnityWebRequest.Get(pathToCloudJS))
            {
                yield return www.SendWebRequest();// www.Send();
                //if (www.isNetworkError || www.isHttpError)
                if(www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log(www.error);
                    Debug.LogError("we don't have connectivty to server : warn user");
                    noInternetNotification.SetActive(true);
                }
                else
                {
                    Debug.Log("connectivity to server ok");
                }
            }

        }
    }

}


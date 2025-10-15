using BAPointCloudRenderer.Loading;
using BAPointCloudRenderer.ObjectCreation;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BAPointCloudRenderer.CloudController
{

    /// <summary>
    /// Point Cloud Set to display a large point cloud. All the time, only the points which are needed for the current camera position are loaded from the disk (as described in the thesis).
    /// </summary>
    public class DynamicPointCloudSet : AbstractPointCloudSet
    {
        /// <summary>
        /// Point Budget - Maximum Number of Points in Memory / to Render
        /// </summary>
        public uint pointBudget = 1000000;
        /// <summary>
        /// Minimum Node Size
        /// </summary>
        public int minNodeSize = 10;

        public double distImportance = 1000;
        public double angleImportance = 1000;
        public double sizeImportance = 1;
        public double mainCamWeight = 1;

        /// <summary>
        // if distance is bigger than maxDistance, level won't exceed MaxLevel
        /// </summary>
        public int maxDistance = 0;

        /// <summary>
        // see MaxDistance
        /// </summary>
        public int maxLevel = 0;

        /// <summary>
        // no node will have a deeper level than that
        /// </summary>
        public int grandMaxLevel = 0;

        /// <summary>
        /// Maximum number of nodes loaded per frame
        /// </summary>
        public uint nodesLoadedPerFrame = 15;
        /// <summary>
        /// Maximum number of nodes having their gameobjects created per frame
        /// </summary>
        public uint nodesGOsPerFrame = 30;
        /// <summary>
        /// Cache Size in POints
        /// </summary>
        public uint cacheSizeInPoints = 1000000;
        /// <summary>
        /// Camera to use. If none is specified, Camera.main is used
        /// </summary>
        public List<Camera> userCameras;

        public bool preloadInBackground = false;

        // Use this for initialization
        protected override void Initialize()
        {
            if (userCameras == null)
            {
                userCameras = new List<Camera>();
                userCameras.Add(Camera.main);
            }
            PointRenderer = new V2Renderer(this, minNodeSize, grandMaxLevel, maxDistance, maxLevel, pointBudget, nodesLoadedPerFrame, nodesGOsPerFrame, userCameras, meshConfiguration, cacheSizeInPoints);
        }


        // Update is called once per frame
        void Update()
        {
            if (!CheckReady())
            {
                return;
            }
            var renderer = (PointRenderer as V2Renderer);
            var tt = renderer.traversalThread;
            tt.minNodeSize = minNodeSize;
            tt.grandMaxLevel = grandMaxLevel;
            tt.maxDistance = maxDistance;
            tt.maxLevel = maxLevel;
            tt.pointBudget = pointBudget;
            tt.nodesGOsPerFrame = nodesGOsPerFrame;
            tt.nodesLoadedPerFrame = nodesLoadedPerFrame;
            tt.distImportance = this.distImportance;
            tt.angleImportance = this.angleImportance;
            tt.sizeImportance = this.sizeImportance;
            tt.mainCamWeight = this.mainCamWeight;
            (PointRenderer as V2Renderer).cache.maxPoints = cacheSizeInPoints;
            (PointRenderer as V2Renderer).cameras = userCameras;
            if (preloadInBackground)
            {
                preloadInBackground = false;
                var metadata = renderer.rootNodes[0].MetaData;
                if (metadata.cloudUrl != null)
                {
                    Debug.Log("check preloadables , local path" + metadata.cloudPath);
                    var preloadable = CloudLoader.getPreloadableNodes(renderer.rootNodes[0], metadata);
                    Debug.Log("need to preload " + preloadable.Count);
                    if (preloadable.Count > 0)
                    {
                        renderer.loadingThread.nodesToPreload = preloadable;
                    }
                }
            }


            PointRenderer.Update();
            DrawDebugInfo();
        }
    }
}

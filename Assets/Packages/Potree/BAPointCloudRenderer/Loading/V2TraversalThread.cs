using BAPointCloudRenderer.CloudData;
using BAPointCloudRenderer.DataStructures;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BAPointCloudRenderer.Loading
{
    /// <summary>
    /// The traversal thread of the V2 Rendering System. Checks constantly, which nodes are visible and should be rendered and which not. Described in the Bachelor Thesis in chapter 3.2.4 "Traversal Thread".
    /// This is the place, where most of the magic happens.
    /// </summary>
    class V2TraversalThread
    {

        private GameObject parent;
        private object locker = new object();
        private List<Node> rootNodes;
        public double minNodeSize; //Min projected node size
        public double maxDistance;
        public int grandMaxLevel;
        public int maxLevel;
        public double distImportance = 1000;
        public double angleImportance = 0;
        public double sizeImportance = 0;
        public double mainCamWeight = 1;
        public uint pointBudget;   //Point Budget
        public uint nodesLoadedPerFrame;
        public uint nodesGOsPerFrame;
        private bool running = true;

        //Camera Data
        struct CamData
        {
            public CamData(Camera camera, Transform pcSetTransform)
            {
                this.camWeight = 1.0;
                this.cameraPosition = pcSetTransform.InverseTransformPoint(camera.transform.position);
                this.camForward = pcSetTransform.InverseTransformDirection(camera.transform.forward);

                if (camera.transform.root.name.StartsWith("Vortex"))
                {
                    // get mini camera that adapts fov
                    camera = camera.transform.GetChild(0).GetComponent<Camera>();
                }

                this.screenHeight = Math.Max(1080, camera.pixelRect.height);
                // prevent super big screens that would ask too different node sizes (see minNodeSizes)

                if (camera == Camera.main)
                {
                    // we use fixed field of view, so that frustum used by potree is consistent (doesnot use the one of editor Game view when in edit mode)
                    var fkO = camera.transform.Find("fakeVRCam");
                    //fake frustrum of Vive
                    if (fkO != null)
                    {
                        var fkCam = fkO?.GetComponent<Camera>();
                        fkO.localPosition = new Vector3(0, 0, 0);
                        fkCam.CopyFrom(camera);
                        fkCam.stereoTargetEye = StereoTargetEyeMask.None;
                        float targetFOV = 60;// 86.8f; // measured in unity , h
                        fkCam.fieldOfView = targetFOV;
                        fkCam.aspect = 1.2f;//1.06f;// camera.aspect * 2; // measured total of 1.06 // w.h
                        fkCam.ResetProjectionMatrix();

                        // Debug.Log("overriding main cam fov : " + camera.fieldOfView + "/" + fkCam.fieldOfView);
                        // Debug.Log("overriding main cam aspect : " + camera.aspect + "/" + fkCam.aspect);
                        // Debug.Log("overriding main cam scrrenS : " + camera.pixelRect + "/" + fkCam.pixelRect);

                        camera = fkCam;

                    }

                }



                this.fieldOfView = camera.fieldOfView;
                this.frustum = GeometryUtility.CalculateFrustumPlanes(camera.projectionMatrix * camera.worldToCameraMatrix * pcSetTransform.localToWorldMatrix);

                // prevent to weird sizes comming from VR
                // sizes for main cam pixels (so we can resize main window without loosing resolution)

                if (this.fieldOfView == 0)
                {
                    Debug.LogWarning("WTF cam with null FOv");
                }

            }

            public Vector3 cameraPosition;
            public float screenHeight;
            public double camWeight;
            public float fieldOfView;
            public Plane[] frustum;
            public Vector3 camForward;
        }
        List<CamData> cams;
        private Queue<Node> toDelete;
        private Queue<Node> toRender;
        private HashSet<Node> visibleNodes;

        private V2Renderer mainThread;
        private V2LoadingThread loadingThread;
        private V2Cache cache;

        private Thread thread;

        /// <summary>
        /// Creates the object, but does not start the thread yet
        /// </summary>
        public V2TraversalThread(GameObject parent, V2Renderer mainThread, V2LoadingThread loadingThread, List<Node> rootNodes, double minNodeSize, int grandMaxLevel, double maxDistance, int maxLevel, uint pointBudget, uint nodesLoadedPerFrame, uint nodesGOsPerFrame, V2Cache cache)
        {
            this.parent = parent;
            this.mainThread = mainThread;
            this.loadingThread = loadingThread;
            this.rootNodes = rootNodes;
            this.minNodeSize = minNodeSize;
            this.maxDistance = maxDistance;
            this.grandMaxLevel = grandMaxLevel;
            this.maxLevel = maxLevel;
            this.pointBudget = pointBudget;
            visibleNodes = new HashSet<Node>();
            this.cache = cache;
            this.nodesLoadedPerFrame = nodesLoadedPerFrame;
            this.nodesGOsPerFrame = nodesGOsPerFrame;
        }

        /// <summary>
        /// Starts the thread
        /// </summary>
        public void Start()
        {
            thread = new Thread(Run);
            running = true;
            thread.Start();
        }

        private void Run()
        {
            try
            {
                while (running)
                {
                    toDelete = new Queue<Node>();
                    toRender = new Queue<Node>();
                    uint pointcount = TraverseAndBuildRenderingQueue();
                    mainThread.SetQueues(toRender, toDelete, pointcount);
                    lock (this)
                    {
                        if (running)
                        {
                            Monitor.Wait(this);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private double getNodePriority(Node n, List<CamData> localCams, bool _dontCheckFrustrum)
        {

            Vector3 center = n.BoundingBox.GetBoundsObject().center;

            if (grandMaxLevel != 0 && (n.GetLevel() > grandMaxLevel))
                return -2;

            // Added ignore outside of frustrum to avoid queue pollution
            if (!_dontCheckFrustrum)
            {
                bool insideOneFrust = false;
                foreach (CamData cd in localCams)
                {
                    if (Util.IntersectsFrustum(n.BoundingBox, cd.frustum))
                    {
                        insideOneFrust = true;
                        break;
                    }

                }
                if (!insideOneFrust)
                    return -1;
            }


            double bestPriority = 0; //do we want to discard it fully?

            foreach (CamData cd in localCams)
            {

                double distance = Mathf.Max(0.0001f, (center - cd.cameraPosition).magnitude);
                double maxHAngle = cd.fieldOfView / 2;
                double projHAngle = Math.Atan(n.BoundingBox.Radius() / distance) * Mathf.Rad2Deg;
                double projectedSize = cd.screenHeight * projHAngle / maxHAngle;// n.BoundingBox.Radius() / (slope * distance);
                if (projectedSize > minNodeSize
                    && ((maxDistance == 0 && maxLevel == 0) || distance < maxDistance || n.GetLevel() < maxLevel)
                    )
                {
                    Vector3 camToNodeCenterDir = (center - cd.cameraPosition).normalized;
                    double dot = cd.camForward.x * camToNodeCenterDir.x + cd.camForward.y * camToNodeCenterDir.y + cd.camForward.z * camToNodeCenterDir.z;
                    double angle = dot > 0 ? Math.Acos(dot) : Math.PI / 2f;
                    double angleWeight = angleImportance > 0 ? (Math.Abs(angle) * 1.0 / angleImportance) : 0;
                    float inFrontImportance = dot > 0 ? (_dontCheckFrustrum ? 2 : 1) : 1;
                    double distWeight = distImportance > 0 ? distance * 1.0 / (distImportance) : 0;
                    double quotient = (distImportance == 0 && angleImportance == 0) ? 1 : Math.Max(.00000000000000001, angleWeight + distWeight);
                    double sizeWeight = sizeImportance > 0 ? sizeImportance * projectedSize : 1;
                    double priority = cd.camWeight * inFrontImportance * sizeWeight / quotient;//+1, to prevent divsion by zero

                    // #if UNITY_EDITOR
                    //                     if (!double.IsFinite(priority))
                    //                         Debug.LogError("cheese naaaan");
                    // #endif
                    if (priority > bestPriority)
                    {
                        bestPriority = priority;
                    }
                }
            }
            return bestPriority;


        }




        /// <summary>
        /// Sets the current camera data
        /// </summary>
        /// <param name="cameraPosition">Camera Position</param>
        /// <param name="camForward">Forward Vector</param>
        /// <param name="frustum">View Frustum</param>
        /// <param name="screenHeight">Screen Height</param>
        /// <param name="fieldOfView">Field of View</param>
        public void SetNextCamerasData(List<Camera> ncams, Transform pcSetTransform)
        {
            if (pcSetTransform == null) return;
            lock (locker)
            {
                if (this.cams == null)
                {
                    this.cams = new List<CamData>();
                }
                this.cams.Clear();
                foreach (Camera camera in ncams)
                {
                    if (camera.gameObject.activeSelf)
                    {
                        var isMain = camera == Camera.main;
                        var cam = new CamData(camera, pcSetTransform);
                        if (isMain && (this.mainCamWeight > 0))
                        {
                            cam.camWeight = this.mainCamWeight;
                        }

                        this.cams.Add(cam);

                    }


                }
            }
            // String info = "";
            // foreach (var cd in this.cams)
            // {
            //     info += cd.fieldOfView + " , ";
            // }
            // Debug.Log("fov : " + info);
        }
        bool dontCheckFrustrum = false;
        private uint TraverseAndBuildRenderingQueue()
        {
            List<CamData> localCams;

            PriorityQueue<double, Node> toProcess = new HeapPriorityQueue<double, Node>();

            lock (locker)
            {
                if (this.cams == null)
                {
                    return 0;
                }
                localCams = new List<CamData>(this.cams);
            }
            //Clearing Queues
            uint renderingpointcount = 0;
            uint maxnodestoload = nodesLoadedPerFrame;
            uint maxnodestorender = nodesGOsPerFrame;
            HashSet<Node> newVisibleNodes = new HashSet<Node>();

            foreach (Node rootNode in rootNodes)
            {
                double priority = getNodePriority(rootNode, localCams, true);
                if (priority >= 0)
                {
                    toProcess.Enqueue(rootNode, priority * 100);
                }
                else
                {
                    DeleteNode(rootNode);
                }
            }

            while (!toProcess.IsEmpty() && running)
            {
                Node n = toProcess.Dequeue(); //Min Node Size was already checked
                bool isValidInFrustrum = true;
#if false // already checked in getNodePriority
                if (!dontCheckFrustrum)
                {
                    isValidInFrustrum = false;
                    foreach (CamData cd in localCams)
                    {
                        if (cd.camWeight > 0 && Util.IntersectsFrustum(n.BoundingBox, cd.frustum))
                        {
                            isValidInFrustrum = true;
                            break;
                        }
                    }
                }
#endif
                //Is Node inside frustum?
                if (isValidInFrustrum)
                {// Test do not render when inside) {

                    bool loadchildren = false;
                    lock (n)
                    {
                        if (n.PointCount == -1)
                        {
                            if (maxnodestoload > 0)
                            {
                                loadingThread.ScheduleForLoading(n);
                                --maxnodestoload;
                                loadchildren = true;
                            }
                        }
                        else if (renderingpointcount + n.PointCount <= pointBudget)
                        {
                            if (n.HasGameObjects())
                            {
                                renderingpointcount += (uint)n.PointCount;
                                visibleNodes.Remove(n);
                                newVisibleNodes.Add(n);
                                loadchildren = true;
                            }
                            else if (n.HasPointsToRender())
                            {
                                //Might be in Cache -> Withdraw
                                if (maxnodestorender > 0)
                                {
                                    cache.Withdraw(n);
                                    renderingpointcount += (uint)n.PointCount;
                                    toRender.Enqueue(n);
                                    --maxnodestorender;
                                    newVisibleNodes.Add(n);
                                    loadchildren = true;
                                }
                            }
                            else
                            {
                                if (maxnodestoload > 0)
                                {
                                    loadingThread.ScheduleForLoading(n);
                                    --maxnodestoload;
                                    loadchildren = true;
                                }
                            }
                        }
                        else
                        {
                            maxnodestoload = 0;
                            maxnodestorender = 0;
                            if (n.HasGameObjects())
                            {
                                visibleNodes.Remove(n);
                                DeleteNode(n);
                            }
                        }
                    }

                    if (loadchildren)
                    {
                        foreach (Node child in n)
                        {
                            double priority = getNodePriority(child, localCams, dontCheckFrustrum);
                            if (priority >= 0)
                            {
                                toProcess.Enqueue(child, priority);
                            }
                            else
                            {
                                DeleteNode(child);
                            }
                        }
                    }

                }
                else
                {
                    //This node or its children might be visible
                    DeleteNode(n);
                }
            }
            foreach (Node n in visibleNodes)
            {
                DeleteNode(n);
            }
            visibleNodes = newVisibleNodes;
            return renderingpointcount;
        }

        private void DeleteNode(Node currentNode)
        {
            lock (currentNode)
            {
                if (!currentNode.HasGameObjects())
                {
                    return;
                }
            }
            Queue<Node> nodesToDelete = new Queue<Node>();
            nodesToDelete.Enqueue(currentNode);
            Stack<Node> tempToDelete = new Stack<Node>();   //To assure better order in cache

            while (nodesToDelete.Count != 0)
            {
                Node child = nodesToDelete.Dequeue();
                Monitor.Enter(child);
                if (child.HasGameObjects())
                {
                    Monitor.Exit(child);
                    tempToDelete.Push(child);

                    foreach (Node childchild in child)
                    {
                        nodesToDelete.Enqueue(childchild);
                    }
                }
                else
                {
                    Monitor.Exit(child);
                }
            }
            while (tempToDelete.Count != 0)
            {
                Node n = tempToDelete.Pop();
                toDelete.Enqueue(n);
            }
        }

        public void Stop()
        {
            lock (this)
            {
                running = false;
            }
        }

        public void StopAndWait()
        {
            running = false;
            if (thread != null)
            {
                thread.Join();
                thread = null;
            }

        }

    }
}

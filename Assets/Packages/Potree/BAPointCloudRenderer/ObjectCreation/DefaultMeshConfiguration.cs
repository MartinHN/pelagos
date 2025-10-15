// #define INSTANCIATE_GO 
#if false
using BAPointCloudRenderer.CloudData;
using System.Collections.Generic;
using UnityEngine;
namespace BAPointCloudRenderer.ObjectCreation
{

    /// <summary>
    /// What kind of interpolation to use
    /// </summary>
    enum FragInterpolationMode
    {
        /// <summary>
        /// No interpolation
        /// </summary>
        OFF,
        /// <summary>
        /// Paraboloids
        /// </summary>
        PARABOLOIDS,
        /// <summary>
        /// Cones
        /// </summary>
        CONES
    }

    /// <summary>
    /// This is the default Mesh Configuration, that is able to render points as pixels, quads, circles and also provides fragment and cone interpolations using the fragment shader (see Thesis chapter 3.3.4 "Interpolation").
    /// This works using Geometry Shader Quad Rendering, as described in the Bachelor Thesis in chapter 3.3.3.
    /// This configuration also supports changes of the parameters while the application is running. Just change the parameters and check the checkbox "reload".
    /// This class replaces GeoQuadMeshConfiguration in Version 1.2.
    /// </summary>
    class DefaultMeshConfiguration : MeshConfiguration
    {
        /// <summary>
        /// Radius of the point (in pixel or world units, depending on variable screenSize)
        /// </summary>
        public float pointRadius = 5;
        /// <summary>
        /// Whether the quads should be rendered as circles (true) or as squares (false)
        /// </summary>
        public bool renderCircles = false;
        /// <summary>
        /// True, if pointRadius should be interpreted as pixels, false if it should be interpreted as world units
        /// </summary>
        public bool screenSize = true;
        /// <summary>
        /// Wether and how to use interpolation
        /// </summary>
        public FragInterpolationMode interpolation = FragInterpolationMode.OFF;
        /// <summary>
        /// If changing the parameters should be possible during execution, this variable has to be set to true in the beginning! Later changes to this variable will not change anything
        /// </summary>
        public const bool reloadingPossible = true;
        /// <summary>
        /// Set this to true to reload the shaders according to the changed parameters. After applying the changes, the variable will set itself back to false.
        /// </summary>
        public bool reload = false;
        /// <summary>
        /// The camera that's used for rendering. If not set, Camera.main is used. 
        /// This should usually be the same camera that's used as "User Camera" in the point cloud set.
        /// </summary>
        public Camera renderCamera = null;
        /// <summary>
        /// If set to true, the Bounding Boxes of the individual octree nodes will be displayed.
        /// </summary>
        public bool displayLOD = false;

        public float fadeInTime = 1.0f;

        public bool lowQual = false;

        public Material material;
        private HashSet<GameObject> gameObjectCollection = null;
        private HashSet<BoundingBoxComponent> objsToUpdate = null;

        public GameObject meshPrefab;
        public Material materialDefaults;
        private void LoadShaders()
        {
            if (lowQual)
            {
                //  material = new Material(Shader.Find("Custom/Quad4PointScreenSizeShader"));
                // material = new Material(Shader.Find("Custom/PointShader"));

                //    material.dynamicOcclusion = false;
            }
            else if (interpolation == FragInterpolationMode.OFF)
            {
                if (screenSize)
                {
                    material = new Material(Shader.Find("Custom/QuadGeoScreenSizeShader"));
                }
                else
                {
                    material = new Material(Shader.Find("Custom/QuadGeoWorldSizeShader"));
                }
            }
            else if (interpolation == FragInterpolationMode.PARABOLOIDS || interpolation == FragInterpolationMode.CONES)
            {
                if (screenSize)
                {
                    material = new Material(Shader.Find("Custom/ParaboloidFragScreenSizeShader"));
                }
                else
                {
                    material = new Material(Shader.Find("Custom/ParaboloidFragWorldSizeShader"));
                }
                material.SetInt("_Cones", (interpolation == FragInterpolationMode.CONES) ? 1 : 0);
            }


            if (materialDefaults)
            {
                material.CopyPropertiesFromMaterial(materialDefaults); /// Warning to keep enableInstancing
                material.enableInstancing = true;
            }


            material.enableInstancing = true;
            material.SetFloat("_PointSize", pointRadius);
            material.SetInt("_Circles", renderCircles ? 1 : 0);

            if (renderCamera == null)
            {
                renderCamera = Camera.main;
            }



            initPrefab();

        }

        public void Start()
        {
            if (reloadingPossible)
            {
                gameObjectCollection = new HashSet<GameObject>();
                objsToUpdate = new HashSet<BoundingBoxComponent>();
            }
            LoadShaders();
        }

        public void Update()
        {
            if (reloadingPossible && (gameObjectCollection == null))
            {
                gameObjectCollection = new HashSet<GameObject>();
                objsToUpdate = new HashSet<BoundingBoxComponent>();
                reload = true;
                Debug.LogWarning("hot reload not working for now");
            }
            if (reload && gameObjectCollection != null)
            {
                LoadShaders();
                foreach (GameObject go in gameObjectCollection)
                {
                    go.GetComponent<MeshRenderer>().material = material;
                }
                reload = false;
            }
#if UNITY_EDITOR
            if (displayLOD)
            {
                foreach (GameObject go in gameObjectCollection)
                {
                    BoundingBoxComponent bbc = go.GetComponent<BoundingBoxComponent>();
                    Utility.BBDraw.DrawBoundingBox(bbc.boundingBox, bbc.parent, Color.red, false);
                }
            }
#endif
            if (fadeInTime > 0)
            {

                double now = Time.realtimeSinceStartup;
                foreach (var bbc in objsToUpdate)
                {
                    double dt = now - bbc.createdTime;
                    if (dt < 0) dt = 0;
                    float factor = (float)(dt / fadeInTime);
                    if (factor > 1) factor = 1;
                    // go.transform.localScale = new Vector3(factor, factor, factor);
                    if (!bbc.gameObject) { continue; }
                    var _renderer = bbc.gameObject.GetComponent<MeshRenderer>();
                    // bbc.matBlock = new MaterialPropertyBlock();
                    // Get the current value of the material properties in the renderer.
                    _renderer.GetPropertyBlock(bbc.matBlock);
                    // Assign our new value.
                    bbc.matBlock.SetFloat("_TimeIn", factor);
                    // Apply the edited values to the renderer.
                    _renderer.SetPropertyBlock(bbc.matBlock);
                }

                objsToUpdate.RemoveWhere(b => now - b.createdTime > fadeInTime);
                // Debug.Log("num obj being faded" + objsToUpdate.Count);
            }
            if (screenSize)
            {
                if (interpolation != FragInterpolationMode.OFF)
                {
                    Matrix4x4 invP = (GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, true)).inverse;
                    material.SetMatrix("_InverseProjMatrix", invP);
                }
                material.SetFloat("_FOV", Mathf.Deg2Rad * renderCamera.fieldOfView);
                Rect screen = renderCamera.pixelRect;
                material.SetInt("_ScreenWidth", (int)screen.width);
                material.SetInt("_ScreenHeight", (int)screen.height);
            }

        }

        private void initPrefab()
        {
#if INSTANCIATE_GO
            GameObject go = new GameObject("prefab");

            MeshFilter filter = go.AddComponent<MeshFilter>();
            // Mesh mesh = new Mesh();
            // filter.mesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Added
            renderer.allowOcclusionWhenDynamic = false;

            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            go.isStatic = true;
            //     var flags = StaticEditorFlags.ContributeGI;
            //  goUtility.SetStaticEditorFlags(go, flags);
            //  goUtility.Fla
            // end added
            renderer.material = material;
            BoundingBoxComponent bbc = go.AddComponent<BoundingBoxComponent>();
            meshPrefab = go;

#endif

        }

        public override GameObject CreateGameObject(string name, Vector3[] vertexData, Color[] colorData, BoundingBox boundingBox, Transform parent)
        {

#if !(INSTANCIATE_GO) // changed to prefab
            GameObject gameObject = new GameObject(name);
            Mesh mesh = new Mesh();

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Added
            renderer.allowOcclusionWhenDynamic = false;

            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            gameObject.isStatic = true;
            BoundingBoxComponent bbc = gameObject.AddComponent<BoundingBoxComponent>();

#else
            if (!meshPrefab)
            {
                Debug.LogWarning("no prefab ");
                return null;
            }
            GameObject gameObject = Instantiate(meshPrefab, parent);

            gameObject.name = name;
            Mesh mesh = new Mesh();
            var filter = gameObject.GetComponent<MeshFilter>();
            filter.mesh = mesh;
            BoundingBoxComponent bbc = gameObject.GetComponent<BoundingBoxComponent>();
#endif
            int[] indecies = new int[vertexData.Length];
            for (int i = 0; i < vertexData.Length; ++i)
            {
                indecies[i] = i;
            }
            mesh.vertices = vertexData;
            mesh.colors = colorData;
            mesh.SetIndices(indecies, MeshTopology.Points, 0);

            //Set Translation
            gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
            gameObject.transform.SetParent(parent, false);

            bbc.boundingBox = boundingBox;
            bbc.parent = parent;
            if (fadeInTime > 0)
            {
                bbc.matBlock = new MaterialPropertyBlock();

                var _renderer = gameObject.GetComponent<MeshRenderer>();
                // _renderer.GetPropertyBlock(bbc.matBlock);
                bbc.matBlock.SetFloat("_TimeIn", 0);
                _renderer.SetPropertyBlock(bbc.matBlock);
            }
            bbc.createdTime = Time.realtimeSinceStartup;
            if (gameObjectCollection != null)
            {
                gameObjectCollection.Add(gameObject);
            }
            if ((fadeInTime > 0) && (objsToUpdate != null))
            {
                objsToUpdate.Add(bbc);
            }

            return gameObject;
        }

        public override int GetMaximumPointsPerMesh()
        {
            return 65000;
        }

        public override void RemoveGameObject(GameObject gameObject)
        {
            if (gameObjectCollection != null)
            {
                gameObjectCollection.Remove(gameObject);
            }
            if (objsToUpdate != null)
            {
                var bbc = gameObject.GetComponent<BoundingBoxComponent>();
                objsToUpdate.Remove(bbc);
            }
            if (gameObject != null)
            {
                Destroy(gameObject.GetComponent<MeshFilter>().sharedMesh);
                Destroy(gameObject);
            }
        }
    }
}
#endif
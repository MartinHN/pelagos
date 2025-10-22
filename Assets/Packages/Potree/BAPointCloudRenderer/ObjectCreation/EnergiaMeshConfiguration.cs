// #define INSTANCIATE_GO 

using BAPointCloudRenderer.CloudData;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System.Linq;

namespace BAPointCloudRenderer.ObjectCreation
{
    class EnergiaMeshConfiguration : MeshConfiguration
    {
        [Header("Profile")]
        public PointCloudProfile profile;

        [Header("Global Look")]
        // [Range(0f, 1f)] public float _Alpha = 0.5f;
        public bool modePoint = true;

        [Header("Fade Apparition")]
        public float fadeInTime = 1.0f;
        public float fadeOutTime = 1.0f;

        [Header("Interaction")]
        public Transform effector;

        // [Header("Fade Distance")]
        // public float _FadeDistanceRange = 20f;
        // public float _FadeDistanceFeather = 5f;

        // [Header("Density Crop")]
        // public float _DensityCropRange = 2f;
        // public float _DensityCropFeather = 5f;
        // [Range(0f, 1f)] public float _DensityCropMax = 0.9f;

        public float _Explode = 0;
        // public float _hideCave = 0;
        // public float _hideCaveHeight = 0;
        [Header("References")]
        public Material materialDefaults;
        [Header("Internal Tambouille")]
        public bool reload = false;
        public bool displayLOD = false;

        GameObject meshPrefab;
        private bool reloadingPossible = true;
        // private Material material;
        private List<GameObject> closestList;
        private HashSet<GameObject> gameObjectCollection = null;
        private List<Renderer> renders;
        private HashSet<BoundingBoxComponent> objsToUpdate = null;

        private void LoadShaders()
        {
            // material = new Material(materialDefaults.shader);
            // material.CopyPropertiesFromMaterial(materialDefaults);
            // material = materialDefaults;
            // material.enableInstancing = true;
            materialDefaults.enableInstancing = true;
        }

        public void Start()
        {
            if (reloadingPossible)
            {
                gameObjectCollection = new HashSet<GameObject>();
                objsToUpdate = new HashSet<BoundingBoxComponent>();
                renders = new List<Renderer>();
            }
            closestList = new List<GameObject>();
            LoadShaders();
        }

        public void FixedUpdate()
        {

            if (fadeInTime > 0)
            {
                var _TimeInP = Shader.PropertyToID("_TimeIn");
                float incT = Time.deltaTime / fadeInTime;
                var block = new MaterialPropertyBlock();
                List<BoundingBoxComponent> toRm = new List<BoundingBoxComponent>();
                foreach (var bbc in objsToUpdate)
                {
                    // var mat = bbc.gameObject.GetComponent<MeshRenderer>().material;
                    var render = bbc.gameObject.GetComponent<Renderer>();
                    render.GetPropertyBlock(block);
                    float timeIn = block.GetFloat(_TimeInP);
                    if (timeIn + incT >= 1)
                    {
                        block.SetFloat(_TimeInP, 1);
                        toRm.Add(bbc);
                    }
                    else
                    {
                        block.SetFloat(_TimeInP, timeIn + incT);
                    }
                    render.SetPropertyBlock(block);
                }

                foreach (var o in toRm)
                    objsToUpdate.Remove(o);
            }
        }


        public void Update()
        {
            if (reloadingPossible && (gameObjectCollection == null))
            {
                gameObjectCollection = new HashSet<GameObject>();
                renders = new List<Renderer>();
                objsToUpdate = new HashSet<BoundingBoxComponent>();
                reload = true;
                Debug.LogWarning("hot reload not working for now");
            }

            if (reload && gameObjectCollection != null)
            {
                LoadShaders();
                foreach (GameObject go in gameObjectCollection)
                {
                    go.GetComponent<MeshRenderer>().sharedMaterial = materialDefaults;
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
            Camera.main.farClipPlane = profile._FadeDistanceRange+ profile._FadeDistanceFeather;
            var block = new MaterialPropertyBlock();
            foreach (Renderer render in renders)
            {
                render.GetPropertyBlock(block);
                block.SetFloat("_Alpha", profile._Alpha);
                block.SetFloat("_ModePoint", modePoint ? 1f : 0f);
                block.SetFloat("_FadeDistanceRange", profile._FadeDistanceRange);
                block.SetFloat("_FadeDistanceFeather", profile._FadeDistanceFeather);
                block.SetFloat("_DensityCropRange", profile._DensityCropRange);
                block.SetFloat("_DensityCropFeather", profile._DensityCropFeather);
                block.SetFloat("_DensityCropMax", profile._DensityCropMax);
                block.SetFloat("_Explode", _Explode);
                // block.SetFloat("_hideCave", _hideCave);
                // block.SetFloat("_hideCaveHeight", _hideCaveHeight);
                block.SetVector("_Effector", effector != null ? effector.position : Vector3.zero);
                render.SetPropertyBlock(block);
            }
        }

        public override GameObject CreateGameObject(string name, Vector3[] vertexData, Color[] colorData, BoundingBox boundingBox, Transform parent, string version, Vector3d translationV2)
        {
            GameObject gameObject = new GameObject(name);

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            BoundingBoxComponent bbc = gameObject.AddComponent<BoundingBoxComponent>();

            renderer.sharedMaterial = materialDefaults;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            // material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            var block = new MaterialPropertyBlock();

            if (fadeInTime > 0)
            {
                var render = gameObject.GetComponent<MeshRenderer>();
                render.GetPropertyBlock(block);
                block.SetFloat("_TimeIn", 0f);
                render.SetPropertyBlock(block);
            }

            if (modePoint)
            {
                Mesh mesh = new Mesh();
                mesh.vertices = vertexData;
                mesh.colors = colorData;
                int[] indices = Enumerable.Range(0, vertexData.Length).ToArray();
                mesh.SetIndices(indices, 0, vertexData.Length, MeshTopology.Points, 0, false);
                filter.mesh = mesh;
            }
            else
            {
                filter.mesh = Geometry.Quads(vertexData, colorData, Vector2.one, Vector3.one * 100f);
                // mesh.UploadMeshData(false);
            }

            //Set Translation
            if (version == "2.0")
            {
                // 20230125: potree v2 vertices have absolute coordinates,
                // hence all gameobjects need to reside at Vector.Zero.
                // And: the position must be set after parenthood has been granted.
                //gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
                gameObject.transform.SetParent(parent, false);
                gameObject.transform.localPosition = translationV2.ToFloatVector();
            }
            else
            {
                gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
                gameObject.transform.SetParent(parent, false);
            }

            bbc.boundingBox = boundingBox;
            bbc.parent = parent;
            bbc.createdTime = Time.realtimeSinceStartup;

            if (gameObjectCollection != null)
            {
                gameObjectCollection.Add(gameObject);
                renders.Add(gameObject.GetComponent<Renderer>());
            }

            if ((fadeInTime > 0) && (objsToUpdate != null))
            {
                objsToUpdate.Add(bbc);
            }

            return gameObject;
        }

        IEnumerator FadeOut(GameObject o)
        {
            if (gameObjectCollection != null)
            {
                if (!gameObjectCollection.Contains(o))
                {
                    yield break; // duplicated calls
                }
                gameObjectCollection.Remove(o);
                renders.Remove(o.GetComponent<Renderer>());
            }

            var bbc = o.GetComponent<BoundingBoxComponent>();
            if (objsToUpdate != null)
            {
                objsToUpdate.Remove(bbc);
            }
            if (o != null)
            {
                Renderer render = o.GetComponent<Renderer>();
                var block = new MaterialPropertyBlock();
                render.GetPropertyBlock(block);
                float timeInPct = block.GetFloat("_TimeIn");
                if (timeInPct <= 0)
                {
                    Debug.LogWarning("WTF");
                    timeInPct = 0;
                }
                float time = Mathf.Clamp01(timeInPct);
                float dt = Time.deltaTime;
                if (fadeOutTime <= 0)
                {
                    yield break;
                }
                float step = dt / fadeOutTime;
                if (timeInPct > 0)
                {
                    while (time > 0f)
                    {
                        if (objsToUpdate.Contains(bbc))
                        {
                            Debug.LogWarning("WTF4");
                            yield break;
                        }
                        time -= step;
                        block.SetFloat("_TimeIn", timeInPct * Mathf.Clamp01(time));
                        // yield return null;
                        render.SetPropertyBlock(block);
                        yield return new WaitForSecondsRealtime(dt);
                    }
                }
                block.SetFloat("_TimeIn", 0f);
                render.SetPropertyBlock(block);

                if (o != null)
                {
                    destroyGO(o);
                }
                else
                    Debug.LogWarning("alreaaady deleted?");


            }
        }


        private void destroyGO(GameObject o)
        {
            Renderer render = o.GetComponent<Renderer>();
            var mat = render.material;
            Destroy(o.GetComponent<MeshFilter>().sharedMesh);
            Destroy(o.GetComponent<MeshFilter>().mesh);
            //Destroy(o.GetComponent<BoundingBoxComponent>());
            Destroy(mat);
            Destroy(o);
        }


        public override void RemoveGameObject(GameObject o)
        {
            if (fadeOutTime > 0)
            { StartCoroutine(FadeOut(o)); }
            else
            {
                var bbc = o.GetComponent<BoundingBoxComponent>();
                if (objsToUpdate != null)
                {
                    objsToUpdate.Remove(bbc);
                }
                if (gameObjectCollection != null)
                {
                    if (!gameObjectCollection.Contains(o))
                    {
                        return; // duplicated calls
                    }
                    gameObjectCollection.Remove(o);
                    renders.Remove(o.GetComponent<Renderer>());
                }
                destroyGO(o);
            }
        }

        public override int GetMaximumPointsPerMesh()
        {
            return 65536;
        }
    }
}

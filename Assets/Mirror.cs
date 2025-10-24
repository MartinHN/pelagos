using System;
using System.Collections.Generic;
using System.Linq;
using BAPointCloudRenderer.CloudController;
using Unity.VisualScripting;
using UnityEngine;

public class Mirror : MonoBehaviour
{

    public bool usePlane = true;
    public RenderTexture rt;
    private Camera cam;
    private Camera globCam;
    private GameObject planeGO;
    private GameObject refPlaneGO;
    public Plane plane;

    private Mesh mirrorMesh;

    private float minDistForParticipate = 100;
    private bool isParticipating = false;
    Vector3 closestPlaneWP = new Vector3();
    public float shearY = 0;
    // cached
    DynamicPointCloudSet dynPointCloudSet;
    Vector3[] meshVertices;
    Vector2[] meshUvs;


    void Start()
    {
        globCam = Camera.main;
        cam = transform.GetComponentInChildren<Camera>();


        planeGO = transform.Find("Plane").gameObject;
        planeGO.GetComponent<MeshRenderer>().enabled = true;
        refPlaneGO = Instantiate(planeGO, transform);
        refPlaneGO.GetComponent<MeshRenderer>().enabled = false;
        mirrorMesh = planeGO.GetComponent<MeshFilter>().mesh; // create a copy when accessing?
        if (cam == null) { Debug.LogError("No cam"); }
        dynPointCloudSet = FindAnyObjectByType<DynamicPointCloudSet>();
        meshVertices = mirrorMesh.vertices;
        meshUvs = mirrorMesh.uv;

        rt = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        planeGO.GetComponent<Renderer>().material.SetTexture("_MainTex", rt);
        updatePlane();

    }

    // Update is called once per frame
    void LateUpdate()
    {
#if true // disable when static
        updatePlane();
#endif
        updatePlaneSize();
        updateCam();
    }

    void updatePlane()
    {
        List<Vector3> vertices = new List<Vector3>
        {
            Vector3.zero,
            Vector3.right*10,
           usePlane? Vector3.forward: Vector3.up,
        };
        for (int i = 0; i < 3; i++)
            vertices[i] = refPlaneGO.transform.TransformPoint(vertices[i]);
        plane = new Plane(vertices[0], vertices[1], vertices[2]);
        // DrawPlane(Vector3.zero - plane.normal * plane.distance, plane.normal);
    }



    Vector3 getBoundedInPlane(Vector3 wp)
    {
        var cp = plane.ClosestPointOnPlane(wp);
        var loc = refPlaneGO.transform.InverseTransformPoint(cp);
        float meshFactor = usePlane ? 10 : 1;
        float xHalf = meshFactor / 2;
        float yHalf = meshFactor / 2;
        float zHalf = meshFactor / 2;
        loc.x = Mathf.Clamp(loc.x, -xHalf, xHalf);
        if (usePlane)
            loc.z = Mathf.Clamp(loc.z, -zHalf, zHalf);
        else
            loc.y = Mathf.Clamp(loc.y, -yHalf, yHalf);

        return refPlaneGO.transform.TransformPoint(loc);
    }

   public bool isBehindMirror(Vector3 wp)
    {
       float minDistAway = 1;
        var loc = refPlaneGO.transform.InverseTransformPoint(wp);
        float meshFactor = usePlane ? 10 : 1;
        if (usePlane)
        {
            if (loc.y > -minDistAway) return false;
            if (Mathf.Abs(loc.x) > meshFactor/2 || Mathf.Abs(loc.z) > meshFactor/2) return false;
            return true;
        }
        else
        {
            if (loc.z > 0) return false;
            return false; //TODO
        }


    }
    public bool isActive()
    {
        return cam.gameObject.activeSelf;
    }
    void updatePlaneSize()
    {
        //update plane size
        List<Ray> frustRays = new List<Ray> {
            globCam.ViewportPointToRay(new Vector3(0, 0, 0)),
             globCam.ViewportPointToRay(new Vector3(1, 0, 0)),//
            globCam.ViewportPointToRay(new Vector3(1, 1, 0)),
            globCam.ViewportPointToRay(new Vector3(0, 1, 0))//
        };

        Vector3 minWP = Vector3.zero;
        Vector3 maxWP = Vector3.zero;
        Vector3 minLoc = new Vector3(9999999, 9999999, 99999999);
        Vector3 maxLoc = new Vector3(-9999999, -9999999, -9999999);
        int numValid = frustRays.Count;

        closestPlaneWP = Vector3.one * 999999;
        foreach (var fray in frustRays)
        {
            float distOnRay;
            Vector3 wp;
            if (plane.Raycast(fray, out distOnRay))
            {
                wp = fray.GetPoint(distOnRay);
            }
            else
            {
                wp = fray.GetPoint(999999);
                numValid--;
            }
            wp = getBoundedInPlane(wp);
            if ((wp - globCam.transform.position).magnitude < (closestPlaneWP - globCam.transform.position).magnitude)
                closestPlaneWP = wp;
            var locp = refPlaneGO.transform.InverseTransformPoint(wp);

            if (locp.x < minLoc.x)
                minLoc.x = locp.x;
            if (locp.x > maxLoc.x)
                maxLoc.x = locp.x;
            if (locp.y < minLoc.y)
                minLoc.y = locp.y;
            if (locp.y > maxLoc.y)
                maxLoc.y = locp.y;
            if (locp.z < minLoc.z)
                minLoc.z = locp.z;
            if (locp.z > maxLoc.z)
                maxLoc.z = locp.z;


        }
        // bool isCloseEnough = Mathf.Abs(plane.GetDistanceToPoint(globCam.transform.position)) < globCam.farClipPlane;
        cam.gameObject.SetActive(numValid >= 2 &&
                                Math.Abs(minLoc.x - maxLoc.x) > 0.0001
                                );
        if (!cam.gameObject.activeSelf) { return; }
        if (usePlane) { minLoc.y = 0; maxLoc.y = 0; }
        else { minLoc.z = 0; maxLoc.z = 0; }

        // minLoc = Divide(minLoc, refPlaneGO.transform.localScale);
        // maxLoc= Divide(maxLoc , refPlaneGO.transform.localScale);
        Vector3 locSize = (maxLoc - minLoc);
        if (locSize.magnitude == 0) { Debug.LogWarning("no diag"); return; }
        locSize.Scale(refPlaneGO.transform.localScale);
        float meshF = usePlane ? 10 : 1;
        locSize /= meshF; //plane mesh is 10
        minWP = refPlaneGO.transform.TransformPoint(minLoc);
        maxWP = refPlaneGO.transform.TransformPoint(maxLoc);
        Debug.DrawLine(minWP, maxWP, isParticipating ? Color.green : Color.red);
        var planeMinP = minWP; //plane.ClosestPointOnPlane(minWP);
        var planeMaxP = maxWP; //plane.ClosestPointOnPlane(maxWP);
        var wCenter = (planeMaxP + planeMinP) / 2;
        planeGO.transform.position = wCenter;

        var scale = planeGO.transform.localScale;

        scale.x = locSize.x;
        if (usePlane)
            scale.z = locSize.z;
        else
            scale.y = locSize.y;
        planeGO.transform.localScale = scale;
        bool isCloseEnough = (globCam.transform.position - planeGO.transform.position).magnitude < globCam.farClipPlane + locSize.magnitude * meshF / 2;
        cam.gameObject.SetActive(isCloseEnough);
    }

    void updateCam()
    {

        isParticipating = isActive() && Mathf.Abs(plane.GetDistanceToPoint(globCam.transform.position)) < 300; //minDistForParticipate;
        isParticipating &= Vector3.Angle(globCam.transform.forward, plane.normal) < 60;

        var ucam = dynPointCloudSet.userCameras;
        bool camIsIn = ucam.Contains(cam);
        if (isParticipating != camIsIn)
        {
            Debug.Log("cam should participate" + isParticipating + " " + transform.name);
            if (isParticipating)
                ucam.Add(cam);
            else
                ucam.Remove(cam);
        }


        float signedGlobDist = plane.GetDistanceToPoint(globCam.transform.position);
        var baseV = plane.ClosestPointOnPlane(globCam.transform.position);
        var destPos = baseV - plane.normal * signedGlobDist;
        // Debug.DrawRay(baseV, plane.normal * 1000000,Color.red,0.0f,false);
        //cam.projectionMatrix = globCam.projectionMatrix;
        cam.ResetProjectionMatrix();
        //cam.ResetWorldToCameraMatrix();
        cam.fieldOfView = globCam.fieldOfView;
        cam.nearClipPlane = globCam.nearClipPlane;

        cam.aspect = globCam.aspect;

        cam.transform.position = destPos;
        float distOnRay;
        var ray = new Ray(globCam.transform.position, globCam.transform.forward);
        if (plane.Raycast(ray, out distOnRay))
        {
            cam.transform.LookAt(ray.GetPoint(distOnRay));
            cam.nearClipPlane = Vector3.ProjectOnPlane(closestPlaneWP - globCam.transform.position, Vector3.up).magnitude;//distOnRay;
            for (int i = 0; i < meshVertices.Count(); i++)
            {
                var v = cam.WorldToViewportPoint(planeGO.transform.TransformPoint(meshVertices[i]));
                meshUvs[i] = v;
            }
            mirrorMesh.uv = meshUvs;
        }
        cam.farClipPlane = Mathf.Max(cam.nearClipPlane + 5, globCam.farClipPlane / 2);

        //TODO rotate clip planes
#if false
        var mat = cam.projectionMatrix;
        
        // mat[0, 2] = shearY;
        
        var shear = Matrix4x4.identity;
        shear[0, 2] = shearY;
        var rot = Matrix4x4.Rotate(Quaternion.AngleAxis(shearY, Vector3.up));
        var tNear = Matrix4x4.Translate(Vector3.forward * cam.nearClipPlane);
        var mtNear = Matrix4x4.Translate(Vector3.forward * -cam.nearClipPlane);
        rot.m03 = -shearY;
        mat = rot*mat ;
         

        cam.projectionMatrix = mat;
#endif

    }
    void OnApplicationQuit()
    {
        // Release the hardware resources used by the render texture
        rt.Release();
    }

    public static Vector3 Divide(Vector3 a, Vector3 b)
    {
        return new Vector3(
            b.x != 0 ? a.x / b.x : 0,
            b.y != 0 ? a.y / b.y : 0,
            b.z != 0 ? a.z / b.z : 0
        );
    }

    public void DrawPlane(Vector3 position, Vector3 normal)
    {
        float scale = 100;
        Vector3 v3;

        if (normal.normalized != Vector3.forward)
            v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        else
            v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;
        v3 *= scale;
        var corner0 = position + v3;
        var corner2 = position - v3;
        var q = Quaternion.AngleAxis(90.0f, normal);
        v3 = q * v3;
        var corner1 = position + v3;
        var corner3 = position - v3;

        Debug.DrawLine(corner0, corner2, Color.green);
        Debug.DrawLine(corner1, corner3, Color.green);
        Debug.DrawLine(corner0, corner1, Color.green);
        Debug.DrawLine(corner1, corner2, Color.green);
        Debug.DrawLine(corner2, corner3, Color.green);
        Debug.DrawLine(corner3, corner0, Color.green);
        Debug.DrawRay(position, normal * scale / 2, Color.red);
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using BAPointCloudRenderer.CloudController;
using System;

public class ExtendedFlycam : MonoBehaviour
{

    /*
	EXTENDED FLYCAM
		Desi Quintans (CowfaceGames.com), 17 August 2012.
		Based on FlyThrough.js by Slin (http://wiki.unity3d.com/index.php/FlyThrough), 17 May 2011.
 
	LICENSE
		Free as in speech, and free as in beer.
 
	FEATURES
		WASD/Arrows:    Movement
		          Q:    Climb
		          E:    Drop
                      Shift:    Move faster
                    Control:    Move slower
                        End:    Toggle cursor locking to screen (you can also press Ctrl+P to toggle play mode on and off).
	*/

    public float cameraSensitivity = 5;
    private float climbSpeed = 4;
    private float normalMoveSpeed = 100;
    public float playingFwdSpeed = .1f;
    public float slowFactor = 0.7f;
    private float fastMoveFactor = 3;


    bool invertX = true;

    bool isCamFlipped = false;


    private float maxY = 7;
    private float minY = 0;


    private bool isPlaying = true;
    List<Mirror> mirrors = new List<Mirror>();
    private List<SlowZone> slowZones = new List<SlowZone>();
    Camera cam;
    Vector3 initialWorldPos;
    Quaternion initialRotation;

    Vector2 smoothXY = Vector2.zero;
    public float smoothXYFactor = .98f;
    void Start()
    {

        initialWorldPos = transform.position;
        initialRotation = transform.rotation;
        mirrors = FindObjectsByType<Mirror>(FindObjectsSortMode.InstanceID).ToList();
        slowZones = FindObjectsByType<SlowZone>(FindObjectsSortMode.InstanceID).ToList();
        cam = GetComponent<Camera>();
        //Screen.lockCursor = true;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

    }

    void checkInside()
    {
        foreach (var m in mirrors)
        {
            if (m.isBehindMirror(transform.position))
            {
                Debug.Log("out???" + m.transform.name);
                resetPos();
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            bool isFull = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
            if (isFull)
                Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
            else
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            resetPos();
        }
        Vector3 newPos = handleNavInput();
        applyNewPos(newPos);
        checkInside();
    }

    Vector3 handleNavInput()
    {
        bool isLocked = Cursor.lockState != CursorLockMode.None;
        if (Input.GetMouseButtonDown(0))
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
        else if (Input.GetMouseButtonDown(1))
            invertX = !invertX;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPlaying = !isPlaying;
            if (isPlaying)
            {
                var pos = transform.position;
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
                transform.position = pos;
            }
        }
        float zspf = getZoneSpeedFactor();
        if (!isLocked)
            return transform.position;

        // rotation
        Vector3 forward = transform.localRotation * Vector3.forward;
        float rotationX = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        float rotationY = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        smoothXY.x = smoothXY.x * smoothXYFactor + (1 - smoothXYFactor) * Input.GetAxis("Mouse X");
        smoothXY.y = smoothXY.y * smoothXYFactor + (1 - smoothXYFactor) * Input.GetAxis("Mouse Y");
        rotationX += smoothXY.x * cameraSensitivity * Time.deltaTime * (invertX ? -1 : 1);
        rotationY += smoothXY.y * cameraSensitivity * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, -10, 10);
        if (Input.GetKeyDown(KeyCode.U))
            rotationX += 180;

        transform.localRotation = Quaternion.AngleAxis(rotationX, Vector3.up);
        transform.localRotation *= Quaternion.AngleAxis(rotationY, Vector3.left);

        // translation
        var newPos = transform.position;
        float forwardSpeed = isPlaying ? playingFwdSpeed : Input.GetAxis("Vertical");
        forwardSpeed *= zspf;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            newPos += transform.forward * (normalMoveSpeed * fastMoveFactor) * forwardSpeed * Time.deltaTime;
            newPos += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime * (invertX ? -1 : 1);
        }
        else
        {
            newPos += transform.forward * normalMoveSpeed * forwardSpeed * Time.deltaTime;
            newPos += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime * (invertX ? -1 : 1);
        }

        if (Input.GetKey(KeyCode.O)) { newPos += transform.up * climbSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.L)) { newPos -= transform.up * climbSpeed * Time.deltaTime; }
        return newPos;
    }

    public void resetPos()
    {
        transform.position = initialWorldPos;
        transform.rotation = initialRotation;
    }

    float getZoneSpeedFactor()
    {
        var pos = transform.position;
        foreach (var z in slowZones)
        {
            var locPoint = z.transform.InverseTransformPoint(pos);
            float locDist = locPoint.magnitude * 2;
            if (locDist < 1.0f)
            {
                Debug.DrawLine(pos, z.transform.position, Color.green);
                float slowRange = 0.5f;
                float pctInRange = (1 - locDist) / (1 - slowRange);
                float res = Math.Max(slowFactor, 1 - pctInRange * (1 - slowFactor));
                // Debug.Log("" + res);
                return res;
            }
        }
        return 1;
    }

    void applyNewPos(Vector3 pos)
    {
        var curPos = transform.position;
        if (curPos == pos) { return; }
        var vel = pos - curPos;
        // constrain height
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        float heightPct = (pos.y - minY) / (maxY - minY);
        float rangePct = .25f;
        if (heightPct > 1 - rangePct && vel.y > 0)
            vel.y = Mathf.Lerp(vel.y, 0, (heightPct - (1 - rangePct)) / rangePct);
        else if (heightPct < rangePct && vel.y < 0)
            vel.y = Mathf.Lerp(0, vel.y, 1 + (heightPct - rangePct) / rangePct);
        pos = curPos + vel;
        //var pos2Check = curPos + 2*vel;
        // check mirror bounce
        bool hasActiveCams = false;
        foreach (var m in mirrors)
        {
            float oldSDist = m.plane.GetDistanceToPoint(curPos);
            if (m.isActive())
                hasActiveCams = true;
            else continue;
            float signedDist = m.plane.GetDistanceToPoint(pos);
            if (oldSDist * signedDist == 0) Debug.LogError("collisionFailed");
            if (oldSDist * signedDist < 0)
            {
                Debug.Log("collision " + m.transform.name);
                float distOnRay;
                var ray = new Ray(curPos, vel);
                bool foundImpact = m.plane.Raycast(ray, out distOnRay);
                if (!foundImpact) Debug.LogError("no impact");
                float remainingDist = vel.magnitude - distOnRay;
                if (remainingDist <= 0)
                {
                    Debug.LogError("no remaining");
                    remainingDist = .01f;
                }
                var bouncedVec = Vector3.Reflect(vel.normalized * remainingDist, m.plane.normal);
                var impactV = ray.GetPoint(distOnRay);
                curPos = impactV + bouncedVec;
                transform.position = curPos;
                // TODO consider non vertical mirrors
                float hangle = Vector3.SignedAngle(Vector3.ProjectOnPlane(vel, Vector3.up), Vector3.ProjectOnPlane(-bouncedVec, Vector3.up), Vector3.up);
                Debug.Log(hangle);
                transform.Rotate(Vector3.up, 180 + hangle, Space.World);

                toggleFlipped();

                return;
            }
        }
        transform.position = pos;

        // if(needCamWeight)
        // {
        //             var dp = FindAnyObjectByType<DynamicPointCloudSet>();
        //             dp.mainCamWeight = hasCams
        // }
    }

    void toggleFlipped()
    {
        isCamFlipped = !isCamFlipped;
        cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
        invertX = !invertX;
    }
    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != cam) { return; }
        if (isCamFlipped)
        {
            //  cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
            GL.invertCulling = true;
        }
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != cam) { return; }
        if (isCamFlipped)
        {
            // cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
            GL.invertCulling = false;
        }
    }

    void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        if (isCamFlipped)
            toggleFlipped();
    }
}
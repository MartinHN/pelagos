using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class AlwaysDrawCameraFrustum : MonoBehaviour
{
    public Color frustumColor = Color.white;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Only draw frustum if the camera is not selected in the hierarchy.
        // If it is selected, Unity's default frustum drawing will handle it.
        if (Selection.activeGameObject != gameObject)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                Gizmos.color = frustumColor;
                Gizmos.matrix = Matrix4x4.TRS(cam.transform.position, cam.transform.rotation, Vector3.one);
                if (cam.orthographic)
                {
                    float spread = cam.farClipPlane - cam.nearClipPlane;
                    float center = (cam.farClipPlane + cam.nearClipPlane) / 2.0f;
                    Gizmos.DrawWireCube(new Vector3(0, 0, center), new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, spread));
                }
                else
                {
                    Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
                }
            }
        }
    }
#endif
}
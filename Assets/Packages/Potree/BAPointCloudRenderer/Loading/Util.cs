using BAPointCloudRenderer.CloudData;
using Unity.Mathematics;
using UnityEngine;

namespace BAPointCloudRenderer.Loading
{
    /// <summary>
    /// Various help functions
    /// </summary>
    class Util
    {

        /// <summary>
        /// Checks whether the bounding box is inside the frustum.
        /// Actually, there is a Unity function for this, however that one can only be called from the main thread.
        /// </summary>
        public static bool InsideFrustum(BoundingBox box, Plane[] frustum)
        {
            bool inside;
            for (int i = 0; i < 5; i++)
            {
                inside = false;
                Plane plane = frustum[i];   //Ignore Far Plane, because it doesnt work because of inf values
                inside |= plane.GetSide(new Vector3((float)box.Lx, (float)box.Ly, (float)box.Lz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Lx, (float)box.Ly, (float)box.Uz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Lx, (float)box.Uy, (float)box.Lz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Lx, (float)box.Uy, (float)box.Uz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Ux, (float)box.Ly, (float)box.Lz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Ux, (float)box.Ly, (float)box.Uz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Ux, (float)box.Uy, (float)box.Lz));
                if (inside) continue;
                inside |= plane.GetSide(new Vector3((float)box.Ux, (float)box.Uy, (float)box.Uz));
                if (!inside) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether the vector is inside the frustum.
        /// Actually, there is a Unity function for this, however that one can only be called from the main thread.
        /// </summary>
        public static bool InsideFrustum(Vector3 vec, Plane[] frustum)
        {
            bool inside;
            for (int i = 0; i < 5; i++)
            {
                Plane plane = frustum[i];
                inside = plane.GetSide(vec);
                if (!inside) return false;
            }
            return true;
        }



        public static bool IntersectsFrustum(BoundingBox box, Plane[] planes)
        {

            // not threaded : return GeometryUtility.TestPlanesAABB(frustum, box.GetBoundsObject());
            var bounds = box.GetBoundsObject();
            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                float3 normal_sign = math.sign(plane.normal);
                float3 test_point = (float3)(bounds.center) + (bounds.extents * normal_sign);

                float dot = math.dot(test_point, plane.normal);
                if (dot + plane.distance < 0)
                    return false;
            }

            return true;

        }

        // public static bool inFrontOfCamNearPlane(BoundingBox box, Plane[] frustum)
        // {

        //     // just get any point in front of farPlane
        //     ///[0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
        //     bool inside;
        //     Vector3 pt;
        //     pt = new Vector3((float)box.Lx, (float)box.Ly, (float)box.Lz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Lx, (float)box.Ly, (float)box.Uz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Lx, (float)box.Uy, (float)box.Lz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Lx, (float)box.Uy, (float)box.Uz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Ux, (float)box.Ly, (float)box.Lz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Ux, (float)box.Ly, (float)box.Uz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Ux, (float)box.Uy, (float)box.Lz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;
        //     pt = new Vector3((float)box.Ux, (float)box.Uy, (float)box.Uz);
        //     inside = frustum[4].GetSide(pt);
        //     if (inside) return true;

        //     return false;
        // }




    }
}

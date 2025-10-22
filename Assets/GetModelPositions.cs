#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using BAPointCloudRenderer.CloudController;
using UnityEditor;
using UnityEngine;

public class GetModelPositions
{
    [MenuItem("LAS/ExportMatrices")]
    public static void Export()
    {
        var allLoaders = GameObject.FindObjectsByType<PointCloudLoader>(FindObjectsSortMode.None);
        foreach (var loader in allLoaders)
        {
            Debug.Log(loader.cloudPath);
            Debug.Log(loader.transform.localToWorldMatrix);
        }



        string basePath = Path.GetFullPath("LASmatrices");
        Directory.Delete(basePath, true);
        Directory.CreateDirectory(basePath);
        string csvPath  = Path.Combine(basePath,"transformlist.txt");

        var sb = new StringBuilder();


        var loaders = GameObject.FindObjectsByType<PointCloudLoader>(FindObjectsSortMode.None);
        int i = 0;
        foreach (var loader in loaders)
        {
            if (loader == null) continue;
            if(loader.name == "POINTCLOUD_MAIN") continue;
            string cloudPath = loader.cloudPath;
            cloudPath = cloudPath.Replace("/5/", "/2/");
            if (string.IsNullOrEmpty(cloudPath))
            {
                Debug.LogWarning($"Empty cloudPath on {loader.name}");
                continue;
            }

            // Make transform filename based on cloud basename
            string cloudBase = Path.GetFileNameWithoutExtension(cloudPath);
            string transformFileName = cloudBase + "_"+i.ToString()+".txt";
            string transformFilePath = Path.Combine(basePath, transformFileName);

            // Write matrix file (row-major)
              var mat = loader.transform.localToWorldMatrix;
           MeshFilter meshFilter = loader.transform.GetChild(0).GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 meshCenterLocal = meshBounds.center;
                Debug.Log("mesh center" + meshCenterLocal.ToString());
                //loader.transform.position + meshCenterLocal
                // mat= Matrix4x4.TRS(meshCenterLocal, loader.transform.rotation, loader.transform.localScale);
                mat =  mat* Matrix4x4.Translate(meshCenterLocal);
            }
             else Debug.LogError("no meeeesh");
            WriteMatrixFile(transformFilePath,mat );

            // Use absolute paths in CSV
            string absCloudPath = Path.IsPathRooted(cloudPath) ? cloudPath : Path.GetFullPath(cloudPath);
            string absTransformPath = Path.GetFullPath(transformFilePath);

            // Escape CSV entries (simple: wrap in quotes and escape quotes)
           sb.AppendLine($"{absCloudPath} {absTransformPath}");

            Debug.Log($"Wrote transform for {cloudBase} -> {transformFilePath}");
            i++;
        }

        File.WriteAllText(csvPath, sb.ToString());
        Debug.Log($"Wrote TXT: {csvPath}");
        // Refresh Unity asset database so files appear in editor
        AssetDatabase.Refresh();

         System.Diagnostics.Process.Start("C:\\Users\\User\\Desktop\\pelagos\\unityprojects\\3dcore\\Scripts\\genLasses.sh");
    }

    static void WriteMatrixFile(string path, Matrix4x4 m)
    {
        using (var w = new StreamWriter(path, false))
        {
            #if false
                w.WriteLine($"{m.m00.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m01.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m02.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m03.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
                w.WriteLine($"{m.m10.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m11.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m12.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m13.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
                w.WriteLine($"{m.m20.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m21.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m22.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m23.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
                w.WriteLine($"{m.m30.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m31.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m32.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m33.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
            #else
                w.WriteLine($"{m.m00.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m02.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m01.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m03.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
                w.WriteLine($"{m.m10.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m12.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m11.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m13.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
                w.WriteLine($"{m.m20.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m22.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m21.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m23.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
                w.WriteLine($"{m.m30.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m32.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m31.ToString("R", System.Globalization.CultureInfo.InvariantCulture)} {m.m33.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}");
        #endif
        }
    }

    static string EscapeForCsv(string s)
    {
        return s;
        // if (s == null) return "";
        // return s.Replace("\"", "\"\"");
    }

  
}
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class StaticAnnotationCapture : MonoBehaviour
{
    public Camera cam;

    public static string ScreenshotPath => Path.Combine(Application.dataPath, "..", "Screenshots");
    public CollisionProbe palletCollisionProbe;

    public bool annotateAllPallets = false;

    /// <summary>
    /// Either take the object which do collide with the collisionProbe geometry in front of
    /// the forklift, or take all visible pallets. First one is preferred for annotations.
    /// TODO: Change to ray casting from camera into scene, if only the front pallet shall be recognized
    /// </summary>
    private IEnumerable<GameObject> TargetObjects => annotateAllPallets ? GameObject.FindGameObjectsWithTag("annotation") : palletCollisionProbe.Pallets.Select(p => p.transform.Find("Annotation").gameObject);

    private static bool OutputVisibleRenderers(IEnumerable<Renderer> renderers, Camera cam) => renderers.Any(r =>
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    });

    private GameObject[] VisibleTargetObjects(Camera c) => (from obj in TargetObjects
        let renderers = obj.GetComponentsInChildren<Renderer>()
        where OutputVisibleRenderers(renderers, c)
        select obj).ToArray();

    [Serializable]
    private class Rectangles
    {
        public List<Points> vertices = new List<Points>();
    }

    [Serializable]
    private class Points
    {
        public List<Vector2> positions = new List<Vector2>();
    }

    private IEnumerator WriteScreenshot()
    {
        var filename = Guid.NewGuid().ToString();

        yield return StartCoroutine(TakeScreenshot(filename, cam));

        var objs = VisibleTargetObjects(cam);
        var rectangles = new Rectangles();

        foreach (var gameObj in objs)
        {
            Debug.Log(gameObj.name);
            var localToWorld = gameObj.transform.localToWorldMatrix;
            var filter = gameObj.GetComponent<MeshFilter>();
            var list = new Points();
            for (var j = 0; j < filter.mesh.vertexCount; j++)
            {
                var v = localToWorld.MultiplyPoint3x4(filter.mesh.vertices[j]);
                var pos = cam.WorldToScreenPoint(v);
                list.positions.Add(pos);
            }

            rectangles.vertices.Add(list);
        }

        var json = JsonUtility.ToJson(rectangles, true);
        File.WriteAllText(Path.Combine(ScreenshotPath, $"{filename}.json"), json);
    }

    private static IEnumerator TakeScreenshot(string filename, Camera cam)
    {
        yield return new WaitForEndOfFrame();
        cam.Render();
        var rect = cam.rect;
        var screenshot = new Texture2D((int) (Screen.width * rect.width), Screen.height, TextureFormat.RGB24, true);
        var cropLeft = Screen.width * rect.x;
        screenshot.ReadPixels(new Rect(cropLeft, 0, screenshot.width, Screen.height), 0, 0);
        screenshot.Apply();

        var newScreenshot = new Texture2D(screenshot.width, screenshot.height);
        newScreenshot.SetPixels(screenshot.GetPixels());
        newScreenshot.Apply();

        byte[] bytes = newScreenshot.EncodeToJPG(95);
        var localUrl = Path.Combine(ScreenshotPath, filename + ".jpg");
        File.WriteAllBytes(localUrl, bytes);
        Destroy(screenshot);
        Destroy(newScreenshot);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(WriteScreenshot());
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            cam.Render();
        }
    }
}
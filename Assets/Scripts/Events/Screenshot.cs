using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class Screenshot : MonoBehaviour
{
    private class GameObjectInfo
    {
        public Renderer Renderer;
        public Material Material;
    }

    public GameObject panel;
    public Material labelMaterial;
    public Material material2;
    public Camera renderCamera;
    public string HideTag;
    public string TargetTag;

    private string _screenshotPath;
    private int ScreenshotCount => Directory.GetFiles(_screenshotPath, "screen*.png").Length;
    private IEnumerable<GameObject> TargetObjects => GameObject.FindGameObjectsWithTag(TargetTag);
    private IEnumerable<GameObject> HideObjects => GameObject.FindGameObjectsWithTag(HideTag);

    private bool OutputVisibleRenderers(IEnumerable<Renderer> renderers) => renderers.Any(r => IsVisible(r));

    private IEnumerable<GameObject> VisibleTargetObjects => (from obj in TargetObjects
        let renderers = obj.GetComponentsInChildren<Renderer>()
        where OutputVisibleRenderers(renderers)
        select obj).ToList();

    /// <summary>
    /// Create the screenshot folder once the game object is awake.
    /// </summary>
    private void Start()
    {
        _screenshotPath = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(_screenshotPath))
        {
            Directory.CreateDirectory(_screenshotPath);
        }
    }

    /// <summary>
    /// Renders two images and writes them to the disk.
    /// 1. Main camera view rendered without UI
    /// 2. Same rendering as 1. but all visible target object is highlighted
    /// </summary>
    private async void WriteScreenshot()
    {
        // Retains the original game objects with the original material
        var objects = new List<GameObjectInfo>();
        var count = ScreenshotCount;

        // 1. First rendering without UI
        panel.gameObject.SetActive(false);
        StartCoroutine(WaitForEndOfFrameCoroutine("screen_", count));
        await Task.Delay(50);

        foreach (var target in VisibleTargetObjects)
        {
            var objRenderer = target.GetComponent<Renderer>();
            objects.Add(new GameObjectInfo {Material = objRenderer.material, Renderer = objRenderer});
            await Task.Delay(50);
            objRenderer.materials = new[] {labelMaterial, material2};
            objRenderer.shadowCastingMode = ShadowCastingMode.Off;
            objRenderer.receiveShadows = false;
        }

        StartCoroutine(WaitForEndOfFrameCoroutine("label_", count));
        await Task.Delay(50);

        // Restore original materials
        foreach (var obj in objects)
        {
            obj.Renderer.materials = new[] {obj.Material};
            obj.Renderer.shadowCastingMode = ShadowCastingMode.On;
            obj.Renderer.receiveShadows = true;
        }

        panel.gameObject.SetActive(true);
    }

    private IEnumerator WaitForEndOfFrameCoroutine(string filename, int index)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(_screenshotPath + "/" + filename + index.ToString("D4") + ".png");
        //Application.OpenURL("file://" + _screenshotPath);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            WriteScreenshot();
        }
    }

    private bool IsVisible(Renderer r)
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(renderCamera);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    }
}
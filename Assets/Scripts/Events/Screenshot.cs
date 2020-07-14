using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Linq;
using DefaultNamespace;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class Screenshot : MonoBehaviour
{
    private class GameObjectInfo
    {
        public Renderer Renderer;
        public Material Material;
    }

    [Header("References")] public GameObject panel;
    public GameObject CollisionProbe;

    [Header("Settings")] public bool useCollisionProbeInsteadAllVisiblePallet;

    [Header("Materials")] public Material labelMaterial;
    public Color brickCornerColor;
    public Color brickSideColor;
    public Color brickCenterColor;
    public Color brickFrontColor;
    public Color plankTopColor;
    public Color plankBottomColor;
    public Color plankMiddleColor;

    [Header("Setup")] public Camera renderCamera;
    public string HideTag;
    public string TargetTag;

    #region Private

    private string _screenshotPath;
    private static string ScreenshotPrefix => Guid.NewGuid().ToString(); //Directory.GetFiles(_screenshotPath, "*_0_.png").Length;
    private IEnumerable<GameObject> TargetObjects => true ? CollisionProbe.GetComponent<CollisionProbe>().CollidedPallets.ToArray() : GameObject.FindGameObjectsWithTag(TargetTag);
    private IEnumerable<GameObject> HideObjects => GameObject.FindGameObjectsWithTag(HideTag);

    private bool OutputVisibleRenderers(IEnumerable<Renderer> renderers) => renderers.Any(r =>
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(renderCamera);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    });

    private IEnumerable<GameObject> VisibleTargetObjects => (from obj in TargetObjects
        let renderers = obj.GetComponentsInChildren<Renderer>()
        where OutputVisibleRenderers(renderers)
        select obj).ToList();

    /// <summary>
    /// Create the screenshot folder.
    /// </summary>
    private void Start()
    {
        _screenshotPath = Path.Combine(Application.dataPath, "..", "Screenshots");
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
    private IEnumerator WriteScreenshot()
    {
        //var probe = GameObject.FindGameObjectWithTag("probe").GetComponent<CollisionProbe>();
        //    Debug.Log(probe.collidedWithPallet);
        //  Debug.Log(probe.palletTag);

        // Retains the original game objects with the original material
        var originalState = new List<GameObjectInfo>();
        var hiddenObjects = new List<GameObject>();
        var filename = ScreenshotPrefix;

        // 1. First rendering without UI
        panel.gameObject.SetActive(false);
        yield return StartCoroutine(Capture(filename, "_0_"));
        yield return new WaitForEndOfFrame();

        // 2. Label entire pallet with one material
        foreach (var target in VisibleTargetObjects)
        {
            for (var i = 0; i < target.transform.childCount; i++)
            {
                Debug.Log(target.transform.GetChild(i).name);
                if (target.transform.GetChild(i).name.StartsWith("Pallet."))
                {
                    var child = target.transform.GetChild(i).GetComponent<Renderer>();
                    originalState.Add(new GameObjectInfo {Material = new Material(child.material), Renderer = child});
                    yield return new WaitForEndOfFrame();
                    child.material = null;
                    child.materials = new[] {labelMaterial};
                    child.shadowCastingMode = ShadowCastingMode.Off;
                    child.receiveShadows = false;
                }
                // Hide non essential parts
                else if (target.transform.GetChild(i).name.Equals("Pallet"))
                {
                    hiddenObjects.Add(target.transform.GetChild(i).gameObject);
                    target.transform.GetChild(i).gameObject.SetActive(false);
                }
            }

            yield return new WaitForEndOfFrame();
        }

        yield return StartCoroutine(Capture(filename, "_1_"));
        yield return new WaitForEndOfFrame();

        // 3. Label each part with different color
        foreach (var target in VisibleTargetObjects)
        {
            for (var i = 0; i < target.transform.childCount; i++)
            {
                var name = target.transform.GetChild(i).name;
                if (name.StartsWith("Pallet."))
                {
                    var child = target.transform.GetChild(i).GetComponent<Renderer>();
                    child.material = null;
                    var mat = Instantiate(labelMaterial);
                    mat.color = GetPartMaterial(name);
                    child.materials = new[] {mat};
                    child.shadowCastingMode = ShadowCastingMode.Off;
                    child.receiveShadows = false;
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        yield return StartCoroutine(Capture(filename, "_2_"));
        yield return new WaitForEndOfFrame();

        // 4. Restore original materials and show hidden stuff
        foreach (var obj in originalState)
        {
            obj.Renderer.materials = new[] {new Material(obj.Material)};
            obj.Renderer.shadowCastingMode = ShadowCastingMode.On;
            obj.Renderer.receiveShadows = true;
        }

        foreach (var obj in hiddenObjects)
        {
            obj.SetActive(true);
        }

        panel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Return material based on the game object prefix.
    /// </summary>
    /// <param name="namePrefix"></param>
    /// <returns></returns>
    private Color GetPartMaterial(string namePrefix)
    {
        if (namePrefix.StartsWith(PalletInfo.Brick.Corner))
            return brickCornerColor;
        if (namePrefix.StartsWith(PalletInfo.Brick.Side))
            return brickSideColor;
        if (namePrefix.StartsWith(PalletInfo.Brick.Center))
            return brickCenterColor;
        if (namePrefix.StartsWith(PalletInfo.Brick.Front))
            return brickFrontColor;
        if (namePrefix.StartsWith(PalletInfo.Plank.Top))
            return plankTopColor;
        if (namePrefix.StartsWith(PalletInfo.Plank.Bottom))
            return plankBottomColor;
        if (namePrefix.StartsWith(PalletInfo.Plank.Middle))
            return plankMiddleColor;

        return new Color(0.1f, 0.1f, 0.1f, 1);
    }

    private IEnumerator Capture(string filename, string postfix)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(_screenshotPath + "/" + filename + postfix + ".png");
        //Application.OpenURL("file://" + _screenshotPath);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(WriteScreenshot());
        }
    }

    #endregion
}
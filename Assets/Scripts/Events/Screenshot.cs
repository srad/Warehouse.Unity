using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading;
using DefaultNamespace;
using UnityEngine.Rendering;

public class Screenshot : MonoBehaviour
{
    private class GameObjectInfo
    {
        public Renderer Renderer;
        public Material Material;
        public Transform Child;
    }

    [Header("References")] public GameObject panel;
    //[Header("Settings")] public bool useCollisionProbeInsteadAllVisiblePallet = true;

    #region Annotation colors

    [Header("Annotation Properties")] public Material labelMaterial;

    public Color brickCornerColor = new Color(255 / 255f, 115 / 255f, 0);
    public Color brickSideColor = new Color(255 / 255f, 59 / 255f, 0);
    public Color brickCenterColor = new Color(255 / 255f, 0, 145 / 255f);
    public Color brickFrontColor = new Color(172 / 255f, 0, 255 / 255f);
    public Color plankTopColor = new Color(18 / 255f, 255 / 255f, 0);
    public Color plankMiddleColor = new Color(0, 253 / 255f, 255f);
    public Color plankBottomColor = new Color(70 / 255f, 0, 255 / 255f);
    public Color boxColor;

    #endregion

    [Header("Setup")] public Camera renderCamera;

    private const string TargetTag = "pallet";

    #region Private

    private string _screenshotPath;
    private static string ScreenshotPrefix => Guid.NewGuid().ToString();

    /// <summary>
    /// Either take the object which do collide with the collisionProbe geometry in front of
    /// the forklift, or take all visible pallets. First one is preferred for annotations.
    /// </summary>
    private static IEnumerable<GameObject> TargetObjects => true
        ? GameObject.Find("CollisionProbe").GetComponent<CollisionProbe>().Pallets
        : GameObject.FindGameObjectsWithTag(TargetTag);

    private bool OutputVisibleRenderers(IEnumerable<Renderer> renderers) => renderers.Any(r =>
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(renderCamera);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    });

    private IEnumerable<GameObject> VisibleTargetObjects => (from obj in TargetObjects
        let renderers = obj.GetComponentsInChildren<Renderer>()
        where OutputVisibleRenderers(renderers)
        select obj).ToArray();

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

    private IEnumerator AutoCapture()
    {
        yield return WriteScreenshot();
        yield return new WaitForEndOfFrame();
        GameObject.Find("Warehouse").GetComponent<GenerateWarehouse>().Generate();
        yield return new WaitForEndOfFrame();
    }

    /// <summary>
    /// Renders two images and writes them to the disk.
    /// 1. Main camera view rendered without UI
    /// 2. Same rendering as 1. but all visible target object is highlighted
    /// </summary>
    private IEnumerator WriteScreenshot()
    {
        // Retains the original game objects with the original material
        var targetStates = new List<GameObjectInfo>();
        var hiddenObjects = new List<GameObject>();
        var filename = ScreenshotPrefix;
        var targets = VisibleTargetObjects.ToArray();
        string palletLoaded = "0"; //TargetObjects.Any() ? TargetObjects.First().gameObject.transform.Find(PalletTags.Types.Layers).tag : "0";
        // 1. First rendering without UI
        panel.gameObject.SetActive(false);
        yield return StartCoroutine(TakeScreenshot(filename, $"_0_{targets.Count()}_{palletLoaded}"));
        yield return new WaitForEndOfFrame();

        // 2. Label entire pallet with one material
        foreach (var target in targets)
        {
            for (var i = 0; i < target.transform.childCount; i++)
            {
                var child = target.transform.GetChild(i);
                var isBox = child.name.StartsWith(PalletInfo.Box.Prefix);
                if (child.name.StartsWith("Pallet.") || isBox)
                {
                    var r = isBox ? child.GetChild(0).GetComponent<Renderer>() : child.GetComponent<Renderer>();
                    targetStates.Add(new GameObjectInfo {Material = new Material(r.material), Renderer = r, Child = child});

                    r.material = null;
                    r.materials = new[] {labelMaterial};
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                // Hide non essential parts
                else if (target.transform.GetChild(i).name.Equals("Pallet"))
                {
                    hiddenObjects.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
        }

        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(TakeScreenshot(filename, $"_1_{targets.Count()}_{palletLoaded}"));
        yield return new WaitForEndOfFrame();

        // 3. Label each part with different color
        targetStates.ForEach(info =>
        {
            var name = info.Child.name;
            var isBox = info.Child.name.StartsWith(PalletInfo.Box.Prefix);
            if (name.StartsWith("Pallet.") || isBox)
            {
                info.Renderer.material = null;
                var mat = Instantiate(labelMaterial);
                mat.color = GetPartColor(info.Renderer.gameObject);
                info.Renderer.materials = new[] {mat};
                info.Renderer.shadowCastingMode = ShadowCastingMode.Off;
                info.Renderer.receiveShadows = false;
            }
        });

        yield return StartCoroutine(TakeScreenshot(filename, $"_2_{targets.Count()}_{palletLoaded}"));

        // 4. Restore original materials and show hidden stuff
        targetStates.ForEach(info =>
        {
            info.Renderer.materials = new[] {new Material(info.Material)};
            info.Renderer.shadowCastingMode = ShadowCastingMode.On;
            info.Renderer.receiveShadows = true;
        });

        foreach (var obj in hiddenObjects)
        {
            obj.SetActive(true);
        }

        panel.gameObject.SetActive(true);
        yield return new WaitForEndOfFrame();
        targetStates.Clear();
    }

    /// <summary>
    /// Return material based on the game object prefix.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private Color GetPartColor(GameObject obj)
    {
        if (obj.CompareTag("brick.corner"))
            return brickCornerColor;
        if (obj.CompareTag("brick.side"))
            return brickSideColor;
        if (obj.CompareTag("brick.center"))
            return brickCenterColor;
        if (obj.CompareTag("brick.front"))
            return brickFrontColor;
        if (obj.CompareTag("plank.top"))
            return plankTopColor;
        if (obj.CompareTag("plank.bottom"))
            return plankBottomColor;
        if (obj.CompareTag("plank.middle"))
            return plankMiddleColor;
        if (obj.CompareTag("box"))
            return boxColor;

        return new Color(255, 255, 255, 255);
    }

    //private IEnumerator Capture(string filename, string postfix)
    //{
    //    yield return new WaitForEndOfFrame();
    //    ScreenCapture.CaptureScreenshot(_screenshotPath + "/" + filename + postfix + ".png");
    //Application.OpenURL("file://" + _screenshotPath);
    //}

    /// <summary>
    /// This also takes the camera's view port into account.
    /// The image is also down sampled.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="postfix"></param>
    /// <returns></returns>
    private IEnumerator TakeScreenshot(string filename, string postfix)
    {
        yield return new WaitForEndOfFrame();
        var rect = renderCamera.rect;
        var screenshot = new Texture2D((int) (Screen.width * rect.width), Screen.height, TextureFormat.RGB24, true);
        var cropLeft = Screen.width * rect.x;
        screenshot.ReadPixels(new Rect(cropLeft, 0, screenshot.width, Screen.height), 0, 0);
        screenshot.Apply();

        var newScreenshot = new Texture2D(screenshot.width / 4, screenshot.height / 4);
        newScreenshot.SetPixels(screenshot.GetPixels(2));
        newScreenshot.Apply();

        byte[] bytes = newScreenshot.EncodeToJPG(90);
        var localUrl = _screenshotPath + "/" + filename + postfix + ".jpg";
        File.WriteAllBytes(localUrl, bytes);
    }

    private float dT = 0;
    private bool startCapture = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(WriteScreenshot());
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            GameObject.Find("Warehouse").GetComponent<GenerateWarehouse>().Generate();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            startCapture = true;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            startCapture = false;
        }

        if (startCapture)
        {
            if (dT > 3)
            {
                dT = 0;
                StartCoroutine(AutoCapture());
            }

            dT += Time.deltaTime;
        }

        dT += Time.deltaTime;
    }

    #endregion
}
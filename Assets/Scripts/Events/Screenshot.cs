using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
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
    public GameObject forkLift;
    public Volume volume;

    private CollisionProbe _forkliftProbe;

    public GameObject frontLabel;
    //[Header("Settings")] public bool useCollisionProbeInsteadAllVisiblePallet = true;

    [Header("Captures")] public bool captureWholePallet = false;
    public bool captureEachPart = false;

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

    private GenerateWarehouse _generator;

    #endregion

    [Header("Setup")] public List<Camera> cams;

    private const string TargetTag = "pallet";
    public float screenshotDelay = 3.0f;

    #region Private

    public static string ScreenshotPath => Path.Combine(Application.dataPath, "..", "Screenshots");
    public static string ScreenshotPrefix => Guid.NewGuid().ToString();

    /// <summary>
    /// Either take the object which do collide with the collisionProbe geometry in front of
    /// the forklift, or take all visible pallets. First one is preferred for annotations.
    /// TODO: Change to ray casting from camera into scene, if only the front pallet shall be recognized
    /// </summary>
    private IEnumerable<GameObject> TargetObjects => false
        ? _forkliftProbe.Pallets
        : GameObject.FindGameObjectsWithTag(TargetTag);

    private bool OutputVisibleRenderers(IEnumerable<Renderer> renderers, Camera cam) => renderers.Any(r =>
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    });

    private IEnumerable<GameObject> VisibleTargetObjects(Camera cam) => (from obj in TargetObjects
        let renderers = obj.GetComponentsInChildren<Renderer>()
        where OutputVisibleRenderers(renderers, cam)
        select obj).ToArray();

    private IEnumerable<GameObject> VisibleObjects()
    {
        var targets = new List<GameObject>();
        foreach (var cam in cams)
        {
            targets.AddRange(VisibleTargetObjects(cam));
        }

        return targets;
    }

    /// <summary>
    /// Create the screenshot folder.
    /// </summary>
    private void Start()
    {
        if (!Directory.Exists(ScreenshotPath))
        {
            Directory.CreateDirectory(ScreenshotPath);
        }

        _forkliftProbe = forkLift.transform.Find("CollisionProbe").GetComponent<CollisionProbe>();
        _generator = GameObject.Find("Warehouse").GetComponent<GenerateWarehouse>();
    }

    private IEnumerator AutoCapture()
    {
        yield return new WaitForEndOfFrame();
        yield return WriteScreenshot();
        yield return new WaitForEndOfFrame();
        _generator.Generate();
    }

    /// <summary>
    /// Renders two images and writes them to the disk.
    /// 1. Main camera view rendered without UI
    /// 2. Same rendering as 1. but all visible target object is highlighted
    /// </summary>
    private IEnumerator WriteScreenshot()
    {
        // Retains the original game objects with the original material
        var targetObjects = new List<GameObjectInfo>();
        var hiddenObjects = new List<GameObject>();
        var filename = ScreenshotPrefix;
        var targets = VisibleObjects();


        var camObjects = VisibleTargetObjects(cams.First());
        var gameObjects = camObjects.ToList();
        Debug.Log("GameObjects count: " + gameObjects.Count);
        var loadTag = "0";
        if (gameObjects.Any())
        {
            loadTag = gameObjects.First().transform.Find(PalletTags.Types.Load).tag;
        }

        // ------------------- Start Annotation -------------------

        // 0. First rendering without UI
        //panel.gameObject.SetActive(false);
        for (var i = 0; i < cams.Count; i++)
        {
            yield return StartCoroutine(TakeScreenshot(filename, $"_0_{loadTag}_cam{i}", cams[i]));
        }

        // 1. Annotate front of pallet with a label
        foreach (var target in targets)
        {
            // Show front annotation
            for (var i = 0; i < target.transform.childCount; i++)
            {
                var child = target.transform.GetChild(i);
                if (child.name.StartsWith("Annotation.Front"))
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        //_gray.intensity.value = 1;

        for (var i = 0; i < cams.Count; i++)
        {
            yield return StartCoroutine(TakeScreenshot(filename, $"_1_{loadTag}_cam{i}", cams[i]));
        }

        // Hide front annotation again
        foreach (var target in targets)
        {
            // Show front annotation
            for (var i = 0; i < target.transform.childCount; i++)
            {
                var child = target.transform.GetChild(i);
                if (child.name.StartsWith("Annotation.Front"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        //_gray.intensity.value = 0;

        if (captureWholePallet)
        {
            yield return new WaitForEndOfFrame();

            // 2. Label entire pallet with one material
            foreach (var target in targets)
            {
                for (var i = 0; i < target.transform.childCount; i++)
                {
                    var child = target.transform.GetChild(i);
                    var isBox = child.name.StartsWith(PalletInfo.Box.Prefix);
                    if (child.name.StartsWith("Pallet.") && !isBox)
                    {
                        var r = child.GetComponent<Renderer>();
                        targetObjects.Add(new GameObjectInfo {Material = new Material(r.material), Renderer = r, Child = child});

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


            for (var i = 0; i < cams.Count; i++)
            {
                yield return StartCoroutine(TakeScreenshot(filename, $"_2_{loadTag}_cam{i}", cams[i]));
            }
        }


        if (captureEachPart)
        {
            // 3. Label each part with different color
            Debug.Log("TargetObjects count: " + targetObjects.Count);
            targetObjects.ForEach(info =>
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
            for (var i = 0; i < cams.Count; i++)
            {
                yield return StartCoroutine(TakeScreenshot(filename, $"_3_{loadTag}_cam{i}", cams[i]));
            }
        }

        // 4. Restore original materials and show hidden stuff
        targetObjects.ForEach(info =>
        {
            info.Renderer.materials = new[] {new Material(info.Material)};
            info.Renderer.shadowCastingMode = ShadowCastingMode.On;
            info.Renderer.receiveShadows = true;
        });

        foreach (var obj in hiddenObjects)
        {
            obj.SetActive(true);
        }

        // panel.gameObject.SetActive(true);
        targetObjects.Clear();
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
    public static IEnumerator TakeScreenshot(string filename, string postfix, Camera cam)
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

        byte[] bytes = newScreenshot.EncodeToPNG();
        var localUrl = Path.Combine(ScreenshotPath, filename + postfix + ".png");
        File.WriteAllBytes(localUrl, bytes);
        Destroy(screenshot);
        Destroy(newScreenshot);
    }

    private float _dT = 0;
    private bool _startCapture = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(WriteScreenshot());
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            _generator.Generate();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            _generator.missingPallets = !_generator.missingPallets;
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            _generator.generateLoad = !_generator.generateLoad;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            _generator.applyDamage = !_generator.applyDamage;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            _dT = 0;
            _startCapture = true;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            _startCapture = false;
        }

        if (_startCapture)
        {
            if (_dT > screenshotDelay)
            {
                _dT = 0;
                StartCoroutine(AutoCapture());
            }

            _dT += Time.deltaTime;
        }
    }

    #endregion
}
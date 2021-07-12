using UnityEngine;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System;
using static UnityEditor.Recorder.MovieRecorderSettings;
using UnityEditor;
using System.Collections;
using DefaultNamespace;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace UnityEngine.Recorder.Examples
{
    public enum RecorderControllerState
    {
        Video,
        Animation,
        ImageSequence
    }

    public class AnnotController : MonoBehaviour
    {
        private class GameObjectInfo
        {
            public Renderer Renderer;
            public Material Material;
            public Transform Child;
        }

        annotManager AnnotManager;
        RecorderController m_RecorderController;

        [Header("Annotation Properties")] public Material labelMaterial;

        private GenerateWarehouse _generator;

        [Header("Setup")] public List<Camera> cams;

        private const string TargetTag = "pallet";

        private Vector3 begin;
        private bool segmentation = false;

        [Header("The following two simply watch the data, don’t worry about it")]
        public RecorderControllerSettings controllerSettings;
        public MovieRecorderSettings videoRecorder;

        private string mediaOutputFolder;
        private IEnumerable<GameObject> TargetObjects => GameObject.FindGameObjectsWithTag(TargetTag);

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
            Debug.Log(targets.Count);

            return targets;
        }


        void Start()
        {
            GameObject annotController = GameObject.FindGameObjectWithTag("GameController");
            AnnotManager = annotController.GetComponent<annotManager>();

            begin = transform.position;

            controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            m_RecorderController = new RecorderController(controllerSettings);
            mediaOutputFolder = @"D:\Warehouse\data\";
            StartRecorder();
        }

        void Update()
        {
            StartCoroutine(sequence());
        }

        private IEnumerator sequence()
        {
            if (AnnotManager.annotation_counter < AnnotManager.annot_num - 1)
            {
                transform.position += AnnotManager.moveVector * Time.deltaTime * AnnotManager.seconds;
                yield return new WaitForSeconds(AnnotManager.seconds);
                if (segmentation == false)
                {
                    Debug.Log("counter: " + AnnotManager.annotation_counter);
                    Debug.Log("annotNum: " + AnnotManager.annot_num);

                    StopRecorder();
                    StartCoroutine(segmentPallet());
                    segmentation = true;
                    transform.position = begin;
                    AnnotManager.annotation_counter += 1;
                    StartRecorder();
                }
                transform.position += AnnotManager.moveVector * Time.deltaTime * AnnotManager.seconds;
                yield return new WaitForSeconds(AnnotManager.seconds);
                SceneManager.LoadScene("MainScene");
            } else if (AnnotManager.annotation_counter == (AnnotManager.annot_num - 1))
            {
                transform.position += AnnotManager.moveVector * Time.deltaTime * AnnotManager.seconds;
                yield return new WaitForSeconds(AnnotManager.seconds);
                if (segmentation == false)
                {
                    Debug.Log("counter: " + AnnotManager.annotation_counter);
                    Debug.Log("annotNum: " + AnnotManager.annot_num);

                    StartCoroutine(segmentPallet());
                    segmentation = true;
                    transform.position = begin;
                    StopRecorder();
                    StartRecorder();
                }
                transform.position += AnnotManager.moveVector * Time.deltaTime * AnnotManager.seconds;
                yield return new WaitForSeconds(AnnotManager.seconds);
                StopRecorder();
                EditorApplication.ExitPlaymode();
            }
        }

        private IEnumerator segmentPallet()
        {
            Debug.Log("segmenting image");
            var targetObjects = new List<GameObjectInfo>();
            var hiddenObjects = new List<GameObject>();
            var targets = VisibleObjects();

            yield return new WaitForEndOfFrame();

            foreach (var target in targets)
            {
                for (var i = 0; i < target.transform.childCount; i++)
                {
                    var child = target.transform.GetChild(i);
                    var isBox = child.name.StartsWith(PalletInfo.Box.Prefix);
                    if (child.name.StartsWith("Pallet.") && !isBox)
                    {
                        var r = child.GetComponent<Renderer>();
                        targetObjects.Add(new GameObjectInfo { Material = new Material(r.material), Renderer = r, Child = child });

                        r.material = null;
                        r.materials = new[] { labelMaterial };
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
        }

        public void StartRecorder(RecorderControllerState state = RecorderControllerState.Video)
        {
         
            RecorderVideo();
            // Setup Recording

            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 60.0f;

            RecorderOptions.VerboseMode = false;
            m_RecorderController.PrepareRecording();
            m_RecorderController.StartRecording();
        }

        private void RecorderVideo()
        {
            videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "My Video Recorder";
            videoRecorder.Enabled = true;

            videoRecorder.OutputFormat = VideoRecorderOutputFormat.MP4;
            videoRecorder.VideoBitRateMode = VideoBitrateMode.Low;

            // videoRecorder.SetOutput_720p_HD(); GameViewInputSettings modify screen resolution
            videoRecorder.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };

            string id = "Default";

            videoRecorder.AudioInputSettings.PreserveAudio = true;
            if (!segmentation)
            {
                id = "video/" + AnnotManager.annotation_counter.ToString().PadLeft(4, '0') ;
            } else
            {
                id = "segmented/" + AnnotManager.annotation_counter.ToString().PadLeft(4, '0');
            }
            videoRecorder.OutputFile = mediaOutputFolder + id;
            controllerSettings.AddRecorderSettings(videoRecorder);
        }

        public void StopRecorder()
        {
            Debug.Log("Stop recording");
            m_RecorderController.StopRecording();
            controllerSettings.RemoveRecorder(videoRecorder);
        }

        void OnDisable()
        {
            StopRecorder();

        }
    }
}

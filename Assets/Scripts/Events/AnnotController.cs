using UnityEngine;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using System;
using static UnityEditor.Recorder.MovieRecorderSettings;
using UnityEditor;

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

        annotManager AnnotManager;

        RecorderController m_RecorderController;

        [Header("The following two simply watch the data, don’t worry about it")]
        public RecorderControllerSettings controllerSettings;
        public MovieRecorderSettings videoRecorder;

        private string mediaOutputFolder;


        void Start()
        {
            GameObject annotController = GameObject.FindGameObjectWithTag("GameController");
            AnnotManager = annotController.GetComponent<annotManager>();

            controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            m_RecorderController = new RecorderController(controllerSettings);
            mediaOutputFolder = @"D:\Warehouse\SampleRecordings";
            StartRecorder();
        }

        void Update()
        {
            transform.Translate(AnnotManager.moveVector * AnnotManager.moveSpeed * Time.deltaTime);
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

            videoRecorder.AudioInputSettings.PreserveAudio = true;
            string str = DateTime.Now.Year.ToString() + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second;
            videoRecorder.OutputFile = mediaOutputFolder + "/Magic_" + str;
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

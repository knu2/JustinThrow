using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;




using Microsoft.Research.Kinect.Nui;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using System.IO;

namespace JustinThrow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Runtime kinectRuntime;
        readonly SwipeGestureDetector swipeGestureRecognizer = new SwipeGestureDetector();
        TemplatedGestureDetector circleGestureRecognizer;
        readonly ColorStreamManager streamManager = new ColorStreamManager();
        SkeletonDisplayManager skeletonDisplayManager;        
        readonly BarycenterHelper barycenterHelper = new BarycenterHelper();
        readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
        TemplatedPostureDetector templatePostureDetector;
        bool recordNextFrameForPosture;

        string circleKBPath;
        string letterT_KBPath;

        BindableNUICamera nuiCamera;

        public MainWindow()
        {
            InitializeComponent();
        }

        
        private void Window_PreviewKeyDown(object sender, ExecutedRoutedEventArgs e)
        {
            switch (e.Parameter as string)
            {
                case "1":
                    part2_avi.Stop();
                    part2_avi.Visibility = Visibility.Hidden;

                    part1_avi.Visibility = Visibility.Visible;
                    part1_avi.Position = TimeSpan.Zero;
                    part1_avi.Play();
                    break;
                case "2":
                    part1_avi.Stop();
                    part1_avi.Visibility = Visibility.Hidden;

                    part2_avi.Visibility = Visibility.Visible;
                    part2_avi.Position = TimeSpan.Zero;
                    part2_avi.Play();
                    break;
                //case "3":
                //    this.CommandBindings[0].Command.Execute("1");
                //    break;
            }


        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (kinectRuntime == null)
                    {
                        kinectRuntime = e.KinectRuntime;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (kinectRuntime == e.KinectRuntime)
                    {
                        Clean();
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectRuntime == e.KinectRuntime)
                    {
                        Clean();
                        MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
        }

        private void mainWin_Loaded(object sender, RoutedEventArgs e)
        {
            circleKBPath = Path.Combine(Environment.CurrentDirectory, @"data\circleKB.save");
            letterT_KBPath = Path.Combine(Environment.CurrentDirectory, @"data\t_KB.save");

            try
            {
                //listen to any status change for Kinects
                Runtime.Kinects.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
                foreach (Runtime kinect in Runtime.Kinects)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinectRuntime = kinect;
                        break;
                    }
                }

                if (Runtime.Kinects.Count == 0)
                    MessageBox.Show("No Kinect found");
                else
                    Initialize();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Initialize()
        {
            if (kinectRuntime == null)
                return;

            kinectRuntime.Initialize(RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            kinectRuntime.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
            kinectRuntime.SkeletonFrameReady += kinectRuntime_SkeletonFrameReady;
            kinectRuntime.VideoFrameReady += kinectRuntime_VideoFrameReady;

            swipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

            //skeletonDisplayManager = new SkeletonDisplayManager(kinectRuntime.SkeletonEngine, kinectCanvas);

            kinectRuntime.SkeletonEngine.TransformSmooth = true;
            var parameters = new TransformSmoothParameters
            {
                Smoothing = 1.0f,
                Correction = 0.1f,
                Prediction = 0.1f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.05f
            };
            kinectRuntime.SkeletonEngine.SmoothParameters = parameters;

            LoadCircleGestureDetector();
            LoadLetterTPostureDetector();

            nuiCamera = new BindableNUICamera(kinectRuntime.NuiCamera);

            //elevationSlider.DataContext = nuiCamera;

            //voiceCommander = new VoiceCommander("record", "stop");
            //voiceCommander.OrderDetected += voiceCommander_OrderDetected;

            //StartVoiceCommander();
        }

        void kinectRuntime_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            //kinectDisplay.Source = streamManager.Update(e);
        }

        void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (e.SkeletonFrame == null)
                return;

            //if (recorder != null)
            //    recorder.Record(e.SkeletonFrame);

            if (e.SkeletonFrame.Skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked).Count() == 0)
                return;

            ProcessFrame(e.SkeletonFrame);
        }

        void ProcessFrame(ReplaySkeletonFrame frame)
        {
            Dictionary<int, string> stabilities = new Dictionary<int, string>();
            foreach (var skeleton in frame.Skeletons)
            {
                if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                    continue;

                barycenterHelper.Add(skeleton.Position.ToVector3(), skeleton.TrackingID);

                stabilities.Add(skeleton.TrackingID, barycenterHelper.IsStable(skeleton.TrackingID) ? "Stable" : "Unstable");
                if (!barycenterHelper.IsStable(skeleton.TrackingID))
                    continue;

                if (recordNextFrameForPosture)
                {
                    recordNextFrameForPosture = false;
                    templatePostureDetector.AddTemplate(skeleton);
                }

                foreach (Joint joint in skeleton.Joints)
                {
                    if (joint.Position.W < 0.8f || joint.TrackingState != JointTrackingState.Tracked)
                        continue;

                    if (joint.ID == JointID.HandRight)
                    {
                        swipeGestureRecognizer.Add(joint.Position, kinectRuntime.SkeletonEngine);
                        circleGestureRecognizer.Add(joint.Position, kinectRuntime.SkeletonEngine);
                    }
                }

                algorithmicPostureRecognizer.TrackPostures(skeleton);
                templatePostureDetector.TrackPostures(skeleton);
            }

            //skeletonDisplayManager.Draw(frame);

            //stabilitiesList.ItemsSource = stabilities;

            //currentPosture.Text = "Current posture: " + algorithmicPostureRecognizer.CurrentPosture;
        }

        private void Clean()
        {
            swipeGestureRecognizer.OnGestureDetected -= OnGestureDetected;

            CloseGestureDetector();

            ClosePostureDetector();

            //if (voiceCommander != null)
            //{
            //    voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
            //    voiceCommander.Dispose();
            //    voiceCommander = null;
            //}

            //if (recorder != null)
            //{
            //    recorder.Stop();
            //    recorder = null;
            //}

            if (kinectRuntime != null)
            {
                kinectRuntime.SkeletonFrameReady -= kinectRuntime_SkeletonFrameReady;
                kinectRuntime.VideoFrameReady -= kinectRuntime_VideoFrameReady;
                kinectRuntime.Uninitialize();
                kinectRuntime = null;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Kinect.Toolbox;

namespace JustinThrow
{
    partial class MainWindow
    {
        void LoadCircleGestureDetector()
        {
            using (Stream recordStream = File.Open(circleKBPath, FileMode.OpenOrCreate))
            {
                circleGestureRecognizer = new TemplatedGestureDetector("Circle", recordStream);
                //circleGestureRecognizer.TraceTo(gesturesCanvas, Colors.Red);
                circleGestureRecognizer.OnGestureDetected += OnGestureDetected;

                //templates.ItemsSource = circleGestureRecognizer.LearningMachine.Paths;
            }
        }

        //private void recordCircle_Click(object sender, RoutedEventArgs e)
        //{
        //    if (circleGestureRecognizer.IsRecordingPath)
        //    {
        //        circleGestureRecognizer.EndRecordTemplate();
        //        recordCircle.Content = "Record new Circle";
        //        return;
        //    }

        //    circleGestureRecognizer.StartRecordTemplate();
        //    recordCircle.Content = "Stop Recording";
        //}

        void OnGestureDetected(string gesture)
        {
            int pos = detectedGestures.Items.Add(string.Format("{0} : {1}", gesture, DateTime.Now));

            switch (gesture)
            {
                case "SwipeToLeft":
                case "Circle":
                    this.CommandBindings[0].Command.Execute("2");
                    break;
                case "SwipeToRight":
                    this.CommandBindings[0].Command.Execute("1");
                    break;
            }
            
            detectedGestures.SelectedIndex = pos;
        }

        void CloseGestureDetector()
        {
            if (circleGestureRecognizer == null)
                return;

            using (Stream recordStream = File.Create(circleKBPath))
            {
                circleGestureRecognizer.SaveState(recordStream);
            }
            circleGestureRecognizer.OnGestureDetected -= OnGestureDetected;
        }
    }
}

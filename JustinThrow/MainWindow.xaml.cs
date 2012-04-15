using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace JustinThrow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
    }
}

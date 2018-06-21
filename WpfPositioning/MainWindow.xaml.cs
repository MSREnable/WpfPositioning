using EyeXFramework;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfPositioning
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EyeXHost eyeXHost;
        EyePositionDataStream eyePositionDataStream;

        Rect screenSize;

        // converts mm to pixels
        double conversionFactorX;
        double conversionFactorY;

        double verticalOffset;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var dpiScale = VisualTreeHelper.GetDpi(this);
            conversionFactorX = dpiScale.PixelsPerInchX / 25.4;
            conversionFactorY = dpiScale.PixelsPerInchY / 25.4;

            screenSize = SystemParameters.WorkArea;

            D40.Width = conversionFactorX * 40;
            D40.Height = conversionFactorY * 40;

            D100.Width = conversionFactorX * 100;
            D100.Height = conversionFactorY * 100;

            verticalOffset = (screenSize.Height * dpiScale.DpiScaleY) / 10.0;

            eyeXHost = new EyeXHost();
            eyeXHost.Start();

            eyePositionDataStream = eyeXHost.CreateEyePositionDataStream();
            eyePositionDataStream.Next += (s, e) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    // 0,0 is at the center of the screen
                    // need to convert X & Y Eye positions from mm to pixels
                    double leftEyeX = (screenSize.Width / 2) + (e.LeftEye.X * conversionFactorX);
                    double leftEyeY = (screenSize.Height / 2) + verticalOffset - (e.LeftEye.Y * conversionFactorY);
                    double rightEyeX = (screenSize.Width / 2) + (e.RightEye.X * conversionFactorX);
                    double rightEyeY = (screenSize.Height / 2) + verticalOffset - (e.RightEye.Y * conversionFactorY);
                    
                    double leftEyeZ = e.LeftEye.Z;
                    double rightEyeZ = e.RightEye.Z;

                    if (e.LeftEye.IsValid)
                    {
                        Canvas.SetLeft(LeftEyePositionEllipse, leftEyeX);
                        Canvas.SetTop(LeftEyePositionEllipse, leftEyeY);

                        LeftEyePositionEllipse.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LeftEyePositionEllipse.Visibility = Visibility.Collapsed;
                    }

                    if (e.RightEye.IsValid)
                    {
                        Canvas.SetLeft(RightEyePositionEllipse, rightEyeX);
                        Canvas.SetTop(RightEyePositionEllipse, rightEyeY);

                        RightEyePositionEllipse.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        RightEyePositionEllipse.Visibility = Visibility.Collapsed;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"LeftEyePos  ({e.LeftEye.X,6:F1}mm, {e.LeftEye.Y,6:F1}mm, {e.LeftEye.Z,6:F1}mm) - ({leftEyeX,6:F1}, {leftEyeY,6:F1})");
                    sb.AppendLine($"RightEyePos ({e.RightEye.X,6:F1}mm, {e.RightEye.Y,6:F1}mm, {e.RightEye.Z,6:F1}mm) - ({rightEyeX,6:F1}, {rightEyeY,6:F1})");

                    StatusTextBlock.Text = sb.ToString();
                }));

            };
        }

        private void ExitButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                if (StatusTextBlock.Visibility == Visibility.Visible)
                {
                    StatusTextBlock.Visibility = Visibility.Collapsed;
                }
                else // if (StatusTextBlock.Visibility == Visibility.Collapsed)
                {
                    StatusTextBlock.Visibility = Visibility.Visible;
                }
            }
        }

    }
}

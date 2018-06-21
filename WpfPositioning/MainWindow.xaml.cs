using EyeXFramework;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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

        uint rawDpiX;
        uint rawDpiY;

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
            FetchRawDpi(out rawDpiX, out rawDpiY);

            screenSize = SystemParameters.WorkArea;

            var dpiScale = VisualTreeHelper.GetDpi(this);

            conversionFactorX = (rawDpiX / dpiScale.DpiScaleX) / 25.4;
            conversionFactorY = (rawDpiY / dpiScale.DpiScaleY) / 25.4;

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

        private void FetchRawDpi(out uint rawDpiX, out uint rawDpiY)
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
            var screen = Screen.FromHandle(windowInteropHelper.Handle);

            var point = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var monitor = MonitorFromPoint(point, 2/*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(monitor, DpiType.Raw, out rawDpiX, out rawDpiY);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F12:
                    if (StatusTextBlock.Visibility == Visibility.Visible)
                    {
                        StatusTextBlock.Visibility = Visibility.Collapsed;
                    }
                    else // if (StatusTextBlock.Visibility == Visibility.Collapsed)
                    {
                        StatusTextBlock.Visibility = Visibility.Visible;
                    }
                    break;
                case Key.System:
                    if (e.SystemKey == Key.F10)
                    {
                        if (D100.Visibility == Visibility.Visible)
                        {
                            D100.Visibility = Visibility.Collapsed;
                            D40.Visibility = Visibility.Collapsed;
                            D4.Visibility = Visibility.Collapsed;
                        }
                        else // if (D100.Visibility == Visibility.Collapsed)
                        {
                            D100.Visibility = Visibility.Visible;
                            D40.Visibility = Visibility.Visible;
                            D4.Visibility = Visibility.Visible;
                        }
                    }
                    break;
            }
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In]System.Drawing.Point pt, [In]uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]DpiType dpiType, [Out]out uint dpiX, [Out]out uint dpiY);
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}

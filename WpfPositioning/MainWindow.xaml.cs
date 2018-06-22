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

            // There seems to be a 90mm offset for the data. It does not map to the screen height, 
            // but instead seems to be realatively consistent across multiple devices.
            verticalOffset = 90 * conversionFactorY;

            eyeXHost = new EyeXHost();
            eyeXHost.Start();

            eyePositionDataStream = eyeXHost.CreateEyePositionDataStream();
            eyePositionDataStream.Next += (s, e) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    var sb = new StringBuilder();

                    UpdateEyeData("Left", e.LeftEye, LeftEyePositionEllipse, sb);
                    UpdateEyeData("Right", e.RightEye, RightEyePositionEllipse, sb);
                    if (e.LeftEye.IsValid && e.RightEye.IsValid)
                    {
                        sb.AppendLine($"          IPD ({e.RightEye.X - e.LeftEye.X,7:F1}mm)");
                    }

                    StatusTextBlock.Text = sb.ToString();
                }));

            };
        }

        private void UpdateEyeData(string eyeName, EyePosition eyePosition, System.Windows.Shapes.Ellipse ellipse, StringBuilder sb)
        {
            // 0,0 is at the center of the screen
            // need to convert X & Y Eye positions from mm to pixels
            double eyePositionPixelX = (screenSize.Width / 2) + (eyePosition.X * conversionFactorX);
            double eyePositionPixelY = (screenSize.Height / 2) + verticalOffset - (eyePosition.Y * conversionFactorY);
            if (eyePosition.Z > 600.0 && eyePosition.Z < 700.00)
            {
                ellipse.Opacity = 1.0;
            }
            else if (eyePosition.Z > 500 && eyePosition.Z < 800.00)
            {
                ellipse.Opacity = 0.4;
            }
            else if (eyePosition.Z > 400 && eyePosition.Z < 900.00)
            {
                ellipse.Opacity = 0.2;
            }
            else
            {
                ellipse.Opacity = 0.1;
            }

            if (eyePosition.IsValid)
            {
                Canvas.SetLeft(ellipse, eyePositionPixelX);
                Canvas.SetTop(ellipse, eyePositionPixelY);

                ellipse.Visibility = Visibility.Visible;
            }
            else
            {
                ellipse.Visibility = Visibility.Collapsed;
            }

            sb.AppendLine($"{eyeName,7}EyePos ({eyePosition.X,6:F1}mm, {eyePosition.Y,6:F1}mm, {eyePosition.Z,6:F1}mm) - ({eyePositionPixelX,6:F1}, {eyePositionPixelY,6:F1})");
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

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In]System.Drawing.Point pt, [In]uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In]IntPtr hmonitor, [In]DpiType dpiType, [Out]out uint dpiX, [Out]out uint dpiY);

        private void DebugInformationCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (DebugInformationCheckbox.IsChecked.HasValue && DebugInformationCheckbox.IsChecked.Value)
            {
                StatusTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                StatusTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void PositioningReticleCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (PositioningReticleCheckbox.IsChecked.HasValue && PositioningReticleCheckbox.IsChecked.Value)
            {
                D40.Visibility = Visibility.Visible;
                D4.Visibility = Visibility.Visible;
            }
            else
            {
                D40.Visibility = Visibility.Collapsed;
                D4.Visibility = Visibility.Collapsed;
            }
        }
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}

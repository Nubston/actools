﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Win32;
using Microsoft.Win32;
using Application = System.Windows.Application;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// A window instance that is capable of per-monitor DPI awareness when supported.
    /// </summary>
    public abstract class DpiAwareWindow : Window {
        private static double? _optionScale;

        public static double OptionScale {
            get {
                if (!_optionScale.HasValue) throw new Exception("Set OptionScale first");
                return _optionScale.Value;
            }
            set {
                if (Equals(_optionScale, value)) return;
                if (_optionScale.HasValue) throw new Exception("OptionScale already has been set");
                _optionScale = value;
            }
        }

        /// <summary>
        /// Occurs when the system or monitor DPI for this window has changed.
        /// </summary>
        public event EventHandler DpiChanged;

        private HwndSource _source;
        private readonly bool _isPerMonitorDpiAware;

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiAwareWindow"/> class.
        /// </summary>
        protected DpiAwareWindow() {
            SourceInitialized += OnSourceInitialized;
            SizeChanged += OnSizeChanged;
            StateChanged += OnStateChanged;

            // WM_DPICHANGED is not send when window is minimized, do listen to global display setting changes
            SystemEvents.DisplaySettingsChanged += OnSystemEventsDisplaySettingsChanged;

            // try to set per-monitor dpi awareness, before the window is displayed
            _isPerMonitorDpiAware = ModernUiHelper.TrySetPerMonitorDpiAware();

            // set the default owner
            if (Application.Current != null && !ReferenceEquals(Application.Current.MainWindow, this)) {
                Owner = Application.Current.Windows.OfType<DpiAwareWindow>().FirstOrDefault(x => x.IsActive)
                        ?? (Application.Current.MainWindow.IsVisible ? Application.Current.MainWindow : null);
            }
        }

        public new void ShowDialog() {
            var owner = Owner as DpiAwareWindow;
            if (owner?.IsDimmed == true) {
                owner = null;
            }

            if (owner != null) {
                owner.IsDimmed = true;
            }

            if (owner?.Visibility == Visibility.Hidden) {
                ShowInTaskbar = true;
            }

            try {
                base.ShowDialog();
            } finally {
                if (owner != null) {
                    owner.IsDimmed = false;
                }
            }
        }

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            LoadLocationAndSize();
        }

        protected override void OnLocationChanged(EventArgs e) {
            base.OnLocationChanged(e);
            SaveLocationAndSize();
        }

        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            SaveLocationAndSize();
        }

        protected virtual void OnStateChanged(object sender, EventArgs e) {
            SaveLocationAndSize();
        }

        /// <summary>
        /// Gets the DPI information for this window instance.
        /// </summary>
        /// <remarks>
        /// DPI information is available after a window handle has been created.
        /// </remarks>
        public DpiInformation DpiInformation { get; private set; }

        /// <summary>
        /// Raises the System.Windows.Window.Closed event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);

            // detach global event handlers
            SystemEvents.DisplaySettingsChanged -= OnSystemEventsDisplaySettingsChanged;
        }

        private void OnSystemEventsDisplaySettingsChanged(object sender, EventArgs e) {
            if (_source != null && WindowState == WindowState.Minimized) {
                RefreshMonitorDpi();
            }
        }

        private void OnSourceInitialized(object sender, EventArgs e) {
            _source = (HwndSource)PresentationSource.FromVisual(this);
            if (_source?.CompositionTarget == null) return;

            // calculate the DPI used by WPF; this is the same as the system DPI
            var matrix = _source.CompositionTarget.TransformToDevice;
            DpiInformation = new DpiInformation(96D * matrix.M11, 96D * matrix.M22);

            if (!_isPerMonitorDpiAware) return;
            _source.AddHook(WndProc);
            RefreshMonitorDpi();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg != NativeMethods.WM_DPICHANGED || _source?.CompositionTarget == null) {
                return IntPtr.Zero;
            }

            // Marshal the value in the lParam into a Rect.
            var newDisplayRect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

            // Set the Window’s position & size.
            var matrix = _source.CompositionTarget.TransformFromDevice;
            var ul = matrix.Transform(new Vector(newDisplayRect.left, newDisplayRect.top));
            var hw = matrix.Transform(new Vector(newDisplayRect.right - newDisplayRect.left, newDisplayRect.bottom - newDisplayRect.top));
            Left = ul.X;
            Top = ul.Y;
            UpdateWindowSize(hw.X, hw.Y);

            // Remember the current DPI settings.
            var oldDpiX = DpiInformation.MonitorDpiX;
            var oldDpiY = DpiInformation.MonitorDpiY;

            // Get the new DPI settings from wParam
            var dpiX = (double)(wParam.ToInt32() >> 16);
            var dpiY = (double)(wParam.ToInt32() & 0x0000FFFF);

            if (oldDpiX != dpiX || oldDpiY != dpiY) {
                DpiInformation.UpdateMonitorDpi(dpiX, dpiY);

                // update layout scale
                UpdateLayoutTransform();

                // raise DpiChanged event
                OnDpiChanged(EventArgs.Empty);
            }

            handled = true;
            return IntPtr.Zero;
        }

        private void UpdateLayoutTransform() {
            if (!_isPerMonitorDpiAware) return;

            var root = (FrameworkElement)GetVisualChild(0);
            if (root == null) return;

            root.LayoutTransform = DpiInformation.ScaleX != 1.0 || DpiInformation.ScaleY != 1.0 ?
                    new ScaleTransform(DpiInformation.ScaleX, DpiInformation.ScaleY) : null;
        }

        private static bool IsFinite(double value) {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void UpdateWindowSize(double width, double height) {
            // determine relative scalex and scaley
            var relScaleX = width / Width;
            var relScaleY = height / Height;
            if (relScaleX == 1.0 && relScaleY == 1.0) return;

            if (!IsFinite(relScaleX)) {
                Logging.Warning("[DPIAWAREWINDOW] relScaleX is NaN: " + relScaleX + ", " + width + ", " + Width);
                return;
            }

            if (!IsFinite(relScaleY)) {
                Logging.Warning("[DPIAWAREWINDOW] relScaleY is NaN: " + relScaleY + ", " + width + ", " + Width);
                return;
            }

            // adjust window size constraints as well
            if (IsFinite(MaxWidth)) MaxWidth *= relScaleX;
            if (IsFinite(MaxHeight)) MaxHeight *= relScaleY;
            MinWidth *= relScaleX;
            MinHeight *= relScaleY;

            Width = width;
            Height = height;
        }

        private bool _scaled;

        private void RescaleIfNeeded() {
            if (OptionScale != 1d && !_scaled) {
                if (IsFinite(MaxWidth)) MaxWidth *= OptionScale;
                if (IsFinite(MaxHeight)) MaxHeight *= OptionScale;
                if (IsFinite(Width)) Width *= OptionScale;
                if (IsFinite(Height)) Height *= OptionScale;
                if (IsFinite(MinWidth)) MinWidth *= OptionScale;
                if (IsFinite(MinHeight)) MinHeight *= OptionScale;
                _scaled = true;
            }
        }

        /// <summary>
        /// Refreshes the current monitor DPI settings and update the window size and layout scale accordingly.
        /// </summary>
        protected void RefreshMonitorDpi() {
            RescaleIfNeeded();
            if (!_isPerMonitorDpiAware) {
                return;
            }

            // get the current DPI of the monitor of the window
            var monitor = NativeMethods.MonitorFromWindow(_source.Handle, NativeMethods.MONITOR_DEFAULTTONEAREST);

            uint xDpi = 96;
            uint yDpi = 96;
            if (NativeMethods.GetDpiForMonitor(monitor, (int)MonitorDpiType.EffectiveDpi, ref xDpi, ref yDpi) != NativeMethods.S_OK) {
                xDpi = 96;
                yDpi = 96;
            }

            // vector contains the change of the old to new DPI
            var dpiVector = DpiInformation.UpdateMonitorDpi(xDpi, yDpi);

            // update Width and Height based on the current DPI of the monitor
            UpdateWindowSize(Width * dpiVector.X, Height * dpiVector.Y);

            // update graphics and text based on the current DPI of the monitor
            UpdateLayoutTransform();
        }

        /// <summary>
        /// Raises the <see cref="E:DpiChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnDpiChanged(EventArgs e) {
            DpiChanged?.Invoke(this, e);
        }

        public static readonly DependencyProperty IsDimmedProperty = DependencyProperty.Register(nameof(IsDimmed), typeof(bool),
                typeof(DpiAwareWindow));

        public bool IsDimmed {
            get { return (bool)GetValue(IsDimmedProperty); }
            set { SetValue(IsDimmedProperty, value); }
        }

        public void ShowDialogWithoutBlocking() {
            Application.Current.Dispatcher.BeginInvoke(new Action(ShowDialog));
        }

        private const int GwlStyle = -16;
        private const int WsDisabled = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private bool _nativeEnabled;

        public bool NativeEnabled {
            get { return _nativeEnabled; }
            set {
                if (Equals(value, _nativeEnabled)) return;
                _nativeEnabled = value;

                var handle = new WindowInteropHelper(this).Handle;
                SetWindowLong(handle, GwlStyle, GetWindowLong(handle, GwlStyle) &
                        ~WsDisabled | (value ? 0 : WsDisabled));
            }
        }

        public static readonly DependencyProperty LocationAndSizeKeyProperty = DependencyProperty.Register(nameof(LocationAndSizeKey), typeof(string),
                typeof(DpiAwareWindow), new PropertyMetadata(OnLocationAndSizeKeyChanged));

        public string LocationAndSizeKey {
            get { return (string)GetValue(LocationAndSizeKeyProperty); }
            set { SetValue(LocationAndSizeKeyProperty, value); }
        }

        private static void OnLocationAndSizeKeyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DpiAwareWindow)o).OnLocationAndSizeKeyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private bool _skipLoading;

        public void SetLocationAndSizeKeyAndSave(string key) {
            _skipLoading = true;
            try {
                LocationAndSizeKey = key;
                SaveLocationAndSize();
            } finally {
                _skipLoading = false;
            }
        }

        private void OnLocationAndSizeKeyChanged(string oldValue, string newValue) {
            if (_skipLoading) return;
            LoadLocationAndSize();
        }

        private void LoadLocationAndSize() {
            var key = LocationAndSizeKey;
            if (key == null) return;

            RescaleIfNeeded();

            var locationKey = key + ".l";
            var sizeKey = key + ".s";
            var maximizedKey = key + ".m";

            var area = Screen.PrimaryScreen.WorkingArea;
            var location = ValuesStorage.GetPoint(locationKey, new Point(Left, Top));
            var size = ValuesStorage.GetPoint(sizeKey, new Point(Width, Height));

            Left = Math.Min(Math.Max(location.X, 0), area.Width - 200);
            Top = Math.Min(Math.Max(location.Y, 0), area.Height - 200);
            if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip) {
                Width = Math.Min(Math.Max(size.X, MinWidth), area.Width);
                Height = Math.Min(Math.Max(size.Y, MinHeight), area.Height);
                WindowState = ValuesStorage.GetBool(maximizedKey) ? WindowState.Maximized : WindowState.Normal;
            }
        }

        private void SaveLocationAndSize() {
            var key = LocationAndSizeKey;
            if (key == null || WindowState == WindowState.Minimized) return;

            RescaleIfNeeded();

            var locationKey = key + ".l";
            var sizeKey = key + ".s";
            var maximizedKey = key + ".m";

            ValuesStorage.Set(locationKey, new Point(Left, Top));
            if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip) {
                ValuesStorage.Set(sizeKey, new Point(Width, Height));
                ValuesStorage.Set(maximizedKey, WindowState == WindowState.Maximized);
            }
        }
    }
}

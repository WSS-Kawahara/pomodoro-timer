using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PomodoroTimer.App.ViewModels;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace PomodoroTimer.App;

/// <summary>
/// ミニ表示。枠なし・常に最前面・タスクバーと Alt+Tab に出さない小さなウィンドウ。
/// </summary>
/// <remarks>
/// 位置はすべて画面のピクセル座標で扱う（WPF の DIP は使わない）。
/// ウィンドウの位置は GetWindowRect / SetWindowPos、モニターの作業領域は Screen.WorkingArea で、
/// どちらもピクセル単位なので、画面の拡大率（DPI）が100%以外でも単位がそろう。
/// </remarks>
public partial class MiniWidgetWindow : Window
{
    /// <summary>初期位置での作業領域の端からの余白（DIP）。拡大率に合わせてピクセルに直して使う。</summary>
    private const double EdgeMarginDip = 8;

    private readonly DrawingPoint? _savedPosition;
    private readonly Action _onClick;
    private readonly Action _onRightClick;
    private readonly Action<DrawingPoint> _onMoved;

    /// <summary>左ボタンを押してから、まだドラッグを始めていない間だけ true。</summary>
    private bool _isPressing;
    private System.Windows.Point _pressPoint;

    /// <param name="viewModel">表示内容。</param>
    /// <param name="savedPosition">保存されている位置（ピクセル）。なければ null。</param>
    /// <param name="onClick">左クリックされたときの処理。</param>
    /// <param name="onRightClick">右クリックされたときの処理。</param>
    /// <param name="onMoved">ドラッグで移動したときの処理（移動後の位置、ピクセル）。</param>
    public MiniWidgetWindow(
        MiniWidgetViewModel viewModel,
        DrawingPoint? savedPosition,
        Action onClick,
        Action onRightClick,
        Action<DrawingPoint> onMoved)
    {
        InitializeComponent();
        DataContext = viewModel;
        _savedPosition = savedPosition;
        _onClick = onClick;
        _onRightClick = onRightClick;
        _onMoved = onMoved;
    }

    private IntPtr Handle => new WindowInteropHelper(this).Handle;

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        // Alt+Tab の一覧に出さないよう、ツールウィンドウにする
        var exStyle = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        // レイアウトが終わってウィンドウの大きさ（ピクセル）がわかってから置く
        var size = GetWindowRectangle().Size;
        var position = _savedPosition is { } saved && IsOnScreen(new DrawingRectangle(saved, size))
            ? saved
            : GetDefaultPosition(size);
        MoveTo(position);
    }

    /// <summary>Alt+F4 などでは閉じない。アプリの終了処理中だけ閉じる。</summary>
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (System.Windows.Application.Current is App { IsExiting: true })
        {
            return;
        }

        e.Cancel = true;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPressing = true;
        _pressPoint = e.GetPosition(this);
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isPressing || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        // 一定以上動いたらドラッグとみなす。ドラッグのあとはメイン画面を開かない
        var delta = e.GetPosition(this) - _pressPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _isPressing = false;
        ReleaseMouseCapture();
        DragMove(); // ボタンを離すまで戻らない
        _onMoved(GetWindowRectangle().Location);
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPressing)
        {
            return;
        }

        _isPressing = false;
        ReleaseMouseCapture();
        _onClick();
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e) => _onRightClick();

    /// <summary>初期位置：メインモニターの作業領域（タスクバーを除いた範囲）の右下。</summary>
    private DrawingPoint GetDefaultPosition(System.Drawing.Size size)
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        var margin = (int)Math.Round(EdgeMarginDip * VisualTreeHelper.GetDpi(this).DpiScaleX);
        return new DrawingPoint(
            workingArea.Right - size.Width - margin,
            workingArea.Bottom - size.Height - margin);
    }

    /// <summary>ウィンドウの中心が、いずれかのモニターの作業領域に入っているか。</summary>
    private static bool IsOnScreen(DrawingRectangle rect)
    {
        var center = new DrawingPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
        return Screen.AllScreens.Any(s => s.WorkingArea.Contains(center));
    }

    private DrawingRectangle GetWindowRectangle()
    {
        NativeMethods.GetWindowRect(Handle, out var r);
        return DrawingRectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }

    private void MoveTo(DrawingPoint position) =>
        NativeMethods.SetWindowPos(
            Handle, IntPtr.Zero, position.X, position.Y, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);

    private static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const nint WS_EX_TOOLWINDOW = 0x00000080;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        public static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    }
}

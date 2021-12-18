using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MonitorBlind.Common;
using MonitorBlind.ViewModels;

namespace MonitorBlind.Views
{
    /* 
	 * ViewModelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedWeakEventListenerや
     * CollectionChangedWeakEventListenerを使うと便利です。独自イベントの場合はLivetWeakEventListenerが使用できます。
     * クローズ時などに、LivetCompositeDisposableに格納した各種イベントリスナをDisposeする事でイベントハンドラの開放が容易に行えます。
     *
     * WeakEventListenerなので明示的に開放せずともメモリリークは起こしませんが、できる限り明示的に開放するようにしましょう。
     */

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private double _horizontalMargin;
        private double _verticalMargin;
        private MainWindowViewModel _vm = null;

        public MainWindow()
        {
            InitializeComponent();

            _vm = ViewModelManager.MainWindowViewModel;
            _vm.FixRateCommand.ExecuteHandler = FixRateCommandExecute;
            _vm.FixRateCommand.CanExecuteHandler = CanFixRateCommandExecute;
            this.DataContext = _vm;

            // キーボードのコールバックメソッドをフックする。
            if (_keyboardHookId == IntPtr.Zero)
            {
                using (Process currentProcess = Process.GetCurrentProcess())
                using (ProcessModule currentModule = currentProcess.MainModule)
                {
                    _keyboardHookId = NativeMethods.SetWindowsHookEx(
                        (int)NativeMethods.HookType.WH_KEYBOARD_LL,
                        _keyboardProc,
                        NativeMethods.GetModuleHandle(currentModule.ModuleName),
                        0
                        );
                }
            }
        }

        ~MainWindow()
        {
            // キーボードのコールバックメソッドをアンフックする。
            NativeMethods.UnhookWindowsHookEx(_keyboardHookId);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //マウスボタン押下状態でなければ何もしない
            if (e.ButtonState != MouseButtonState.Pressed) return;

            this.DragMove();
        }

        public static BitmapImage FileToBitmapImage(string filePath)
        {
            BitmapImage bi = null;

            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = fs;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                }
            }
            catch
            {
                bi = null;
            }

            return bi;
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            var hwndSource = (HwndSource)HwndSource.FromVisual(this);
            hwndSource.AddHook(WndHookProc);
        }

        private const int WM_SIZING = 0x214;
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        private IntPtr WndHookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SIZING)
            {
                // MainWindowの範囲を表す四角形
                var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));

                var w = rect.right - rect.left - this._horizontalMargin;
                var h = rect.bottom - rect.top - this._verticalMargin;

                switch (wParam.ToInt32())
                {
                    case WMSZ_LEFT:
                    case WMSZ_RIGHT:
                        rect.bottom = (int)(rect.top + h + this._verticalMargin);
                        break;
                    case WMSZ_TOP:
                    case WMSZ_BOTTOM:
                        rect.right = (int)(rect.left + w + this._horizontalMargin);
                        break;
                    case WMSZ_TOPLEFT:
                        rect.top = (int)(rect.bottom - h - this._verticalMargin);
                        rect.left = (int)(rect.right - w - this._horizontalMargin);
                        break;
                    case WMSZ_TOPRIGHT:
                        rect.top = (int)(rect.bottom - h - this._verticalMargin);
                        rect.right = (int)(rect.left + w + this._horizontalMargin);
                        break;
                    case WMSZ_BOTTOMLEFT:
                        rect.bottom = (int)(rect.top + h + this._verticalMargin);
                        rect.left = (int)(rect.right - w - this._horizontalMargin);
                        break;
                    case WMSZ_BOTTOMRIGHT:
                        rect.bottom = (int)(rect.top + h + this._verticalMargin);
                        rect.right = (int)(rect.left + w + this._horizontalMargin);
                        break;
                    default:
                        break;
                }
                Marshal.StructureToPtr(rect, lParam, true);
            }
            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            this.GetFixRate();
        }

        private void GetFixRate()
        {
            this.SizeToContent = SizeToContent.Manual;

            this._horizontalMargin = this.ActualWidth - this.Width;
            this._verticalMargin = this.ActualHeight - this.Height;

            this.Width = double.NaN;
            this.Height = double.NaN;
        }

        public DelegateCommand FixRateCommand
        {
            get
            {
                return _vm.FixRateCommand;
            }
            set
            {
                if (_vm == null) return;
                _vm.FixRateCommand = value;
            }
        }

        private bool CanFixRateCommandExecute(object param)
        {
            return param != null;
        }

        private void FixRateCommandExecute(object param)
        {
            this.GetFixRate();
        }

        private void window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    this.Left--;
                    break;
                case Key.Up:
                    this.Top--;
                    break;
                case Key.Right:
                    this.Left++;
                    break;
                case Key.Down:
                    this.Top++;
                    break;
                default:
                    // do nothing.
                    break;
            }
        }

        #region アプリ内外でグローバルに有効なイベントハンドラ

        private static IntPtr GlobalKeyboardInputCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                // キーボードのイベントに紐付けられた次のメソッドを実行する。メソッドがなければ処理終了。
                return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
            }

            // キーコードを取得する。
            KBDLLHOOKSTRUCT param = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            // キーボードの「押し上げ」が検出された場合
            if ((NativeMethods.KeyboardMessage)wParam == NativeMethods.KeyboardMessage.WM_KEYUP)
            {
                // キーコードを抽出する。
                int keyCode = param.vkCode;
                Key key = KeyInterop.KeyFromVirtualKey(keyCode);

                // VMのコマンドを実行する。
                ViewModelManager.MainWindowViewModel.KeyInputCommand.Execute(key);
            }

            // キーボードのイベントに紐付けられた次のメソッドを実行する。メソッドがなければ処理終了。
            return NativeMethods.CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private static IntPtr _keyboardHookId = IntPtr.Zero;
        private static readonly NativeMethods.LowLevelKeyboardProc _keyboardProc = GlobalKeyboardInputCallback;

        #endregion
    }
}
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
        private MainWindowViewModel _vm = null;

        public MainWindow()
        {
            InitializeComponent();

            _vm = ViewModelManager.MainWindowViewModel;
            _vm.ChangeIsEnabledToMoveOrZoom += OnChangeIsEnabledToMoveOrZoom;
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
            // マウス操作が無効の場合は何もしない。
            if (!_isEnabledToMoveOrZoom) return;

            //マウスボタン押下状態でなければ何もしない
            if (e.ButtonState != MouseButtonState.Pressed) return;

            this.DragMove();
        }

        private void DuplicateWindow(object sender, RoutedEventArgs e)
        {
            ViewManager.RequestShowMainWindow(this.ActualWidth, this.ActualHeight);
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnChangeIsEnabledToMoveOrZoom(object sender, bool e)
        {
            _isEnabledToMoveOrZoom = e;
            this.ResizeMode = _isEnabledToMoveOrZoom ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;
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

        private void window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // キーボード操作が無効の場合は何もしない。
            if (!_isEnabledToMoveOrZoom) return;

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

        private bool _isEnabledToMoveOrZoom = true;

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

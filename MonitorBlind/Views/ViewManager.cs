using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MonitorBlind.Properties;

namespace MonitorBlind.Views
{
    public static class ViewManager
    {
        /// <summary>
        /// メイン画面が表示されていない場合は表示する。
        /// </summary>
        public static void RequestShowMainWindow(double width, double height)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Width = width;
            mainWindow.Height = height;
            mainWindow.ContentRendered += (sd, ev) =>
            {
                _isMainWindowVisible = true;
            };
            mainWindow.Closed += (sd, ev) =>
            {
                if (_mainWindowList.Count == 0)
                {
                    _isMainWindowVisible = false;
                }
            };

            // メイン画面をリストに追加する。
            _mainWindowList.Add(mainWindow);

            // メイン画面を表示する。
            mainWindow.Show();
        }

        /// <summary>
        /// 設定画面が表示されていない場合は表示する。
        /// </summary>
        public static void RequestShowSettingDialog()
        {
            // メイン画面が表示されていない場合
            if (!_isMainWindowVisible)
            {
                // メイン画面を表示する。
                RequestShowMainWindow(Settings.Default.DefaultMainWindowWidth, Settings.Default.DefaultMainWindowHeight);
            }

            // 設定画面が表示されていない場合
            if (!_isSettingDialogVisible)
            {
                SettingDialog settingDialog = new SettingDialog();
                settingDialog.Owner = _mainWindowList[0];

                settingDialog.ContentRendered += (sd, ev) =>
                {
                    _isSettingDialogVisible = true;
                };
                settingDialog.Closed += (sd, ev) =>
                {
                    _isSettingDialogVisible = false;
                };

                // 設定画面を表示する。
                settingDialog.Show();
            }
        }

        /// <summary>
        /// 表示中のブラインド画面（メイン画面）のインスタンスを保持するためのリスト。
        /// </summary>
        private static List<MainWindow> _mainWindowList = new List<MainWindow>();

        private static bool _isMainWindowVisible = false;
        private static bool _isSettingDialogVisible = false;
    }
}

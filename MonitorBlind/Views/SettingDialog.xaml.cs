﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MonitorBlind.ViewModels;

namespace MonitorBlind.Views
{
    /// <summary>
    /// SettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingDialog : Window
    {
        private MainWindowViewModel _vm = null;

        public SettingDialog()
        {
            InitializeComponent();

            _vm = ViewModelManager.MainWindowViewModel;
            this.DataContext = _vm;
        }

        private void _settingDialog_Closed(object sender, EventArgs e)
        {
            ViewModelManager.MainWindowViewModel.SettingDialogTransitionMessage = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

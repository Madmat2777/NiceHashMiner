﻿using NHM.Common;
using NHM.Wpf.ViewModels;
using NHM.Wpf.Windows.Common;
using NHM.Wpf.Windows.Plugins;
using NiceHashMiner;
using NiceHashMiner.Mining;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using NiceHashMiner.Configs;
using NiceHashMiner.Utils;
using MessageBox = System.Windows.Forms.MessageBox;

namespace NHM.Wpf.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainVM _vm;

        public MainWindow()
        {
            InitializeComponent();

            _vm = WindowUtils.AssertViewModel<MainVM>(this);

            Translations.LanguageChanged += TranslationsOnLanguageChanged;
            TranslationsOnLanguageChanged(null, null);
        }

        private void TranslationsOnLanguageChanged(object sender, EventArgs e)
        {
            WindowUtils.Translate(this);
        }

        private void BenchButton_Click(object sender, RoutedEventArgs e)
        {
            using (var bench = new BenchmarkWindow(AvailableDevices.Devices))
            {
                bench.ShowDialog();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            using (var settings = new SettingsWindow())
            {
                settings.ShowDialog();

                if (settings.RestartRequired)
                {
                    if (!settings.DefaultsSet)
                    {
                        MessageBox.Show(
                            Translations.Tr("Settings change requires {0} to restart.", NHMProductInfo.Name),
                            Translations.Tr("Restart Notice"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    ApplicationStateManager.RestartProgram();
                    Close();
                }
            }
        }

        private void PluginButton_Click(object sender, RoutedEventArgs e)
        {
            var plugin = new PluginWindow();
            plugin.ShowDialog();
        }

        #region Minimize to tray stuff

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TaskbarIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized) // TODO && config min to tray
                Hide();
        }

        #endregion

        private void StatsHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.CredentialsSettings.IsCredentialsValid == false) return;
            ApplicationStateManager.VisitMiningStatsPage();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            var startup = new StartupLoadingWindow();
            startup.Owner = this;
            startup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            startup.CanClose = false;
            startup.Show();

            await _vm.InitializeNhm(startup.StartupLoader);

            startup.CanClose = true;

            // If owner is still set to this when close is called, 
            // it will minimize the main window for some reason
            startup.Owner = null;
            startup.Close();

            IsEnabled = true;
        }

        private static async Task FakeLoad(IStartupLoader loader)
        {
            for (var i = 0; i <= 100; i++)
            {
                loader.PrimaryProgress.Report(("Load", i));
                await Task.Delay(10);
                if (i == 60)
                {
                    loader.SecondaryVisible = true;
                    loader.SecondaryTitle = "Downloading miners...";
                    for (var j = 0; j <= 100; j++)
                    {
                        loader.SecondaryProgress.Report(("Sec load", j));
                        await Task.Delay(10);
                    }
                }
                else
                {
                    loader.SecondaryVisible = false;
                }
            }
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            await _vm.StartMining();
        }

        private async void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            await _vm.StopMining();
        }

        private void HelpButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Links.NhmHelp);
        }

        private void ExchangeButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Links.NhmPayingFaq);
        }

        private async void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            await _vm.StopMining();
        }
    }
}
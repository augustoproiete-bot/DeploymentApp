﻿using DeploymentApp.Configuration;
using DeploymentApp.Deployment;
using DeploymentApp.Dialogs;
using DeploymentApp.Helpers;
using DeploymentApp.Logs;
using DeploymentApp.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Web.Administration;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static DeploymentApp.Enums;

namespace DeploymentApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static TextBlock LogsTextBlock;
        public static Configuration.Binding Config;
        public ServerProfile SelectedServerProfile { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Config = new Configuration.Binding();
            Util.BindComboBox(ddlServerProfiles, Config.Config.ServerProfiles, "ProfileName", "Id");
            lblDeploymentLocation.Content = $@"\\{{SERVERNAME}}\{Config.Config.DefaultServerLocation}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LogsTextBlock = txtbLogs;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                txtFolderPath.Text = dialog.SelectedPath;
            }
        }
        
        private async void btnDeploy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnDeploy.IsEnabled = false;
                txtbLogs.Text = "";
                SwitchPbStatus(true);
                var deployer = new DeploymentManager(new DeploymentParams
                {
                    FolderToDeployPath = txtFolderPath.Text,
                    ServerLocation = Config.Config.DefaultServerLocation,
                    DeployToProps = new List<DeployToProps> 
                    { 
                        new DeployToProps { ServerName = txtServerName1.Text, FolderName = txtFolderName1.Text,},
                        new DeployToProps { ServerName = txtServerName2.Text, FolderName = txtFolderName2.Text,}
                    },
                    Backup = cbBackup.IsChecked,
                    Overwrite = cbOverwrite.IsChecked,
                    SecondsToDelay = 6
                });
                await deployer.StartDeploymentProcess();
                SwitchPbStatus(false);
                btnDeploy.IsEnabled = true;
            }
            catch (Exception ex)
            {
                btnDeploy.IsEnabled = true;
                SwitchPbStatus(false);
                await Logger.Log(ex.Message, true);
            }
        }

        void SwitchPbStatus(bool enabled)
        {
            pbStatus.Visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            pbStatus.IsIndeterminate = enabled;
        }

        void SwitchAppsControls(bool enabled)
        {
            ddlApplications.IsEnabled = enabled;
            btnEditWebApps.IsEnabled = enabled;
        }

        private void btnEditServerProfiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ServerProfilesDialog();
            dialog.ShowDialog();
            if (dialog.ChangedServer != null)
            {
                if (dialog.ChangedServer.Id == SelectedServerProfile.Id)
                {
                    txtServerName1.Text = dialog.ChangedServer.FirstServerName;
                    txtServerName2.Text = dialog.ChangedServer.SecondServerName;
                }
            }
        }

        private void btnEditWebApps_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WebAppsDialog(SelectedServerProfile.Id);
            dialog.ShowDialog();
            if (dialog.ChangedWebApp != null)
            {
                if (dialog.ChangedWebApp.Id == ((WebApp)ddlApplications.SelectedItem).Id)
                {
                    txtFolderName1.Text = dialog.ChangedWebApp.FolderName;
                    txtFolderName2.Text = dialog.ChangedWebApp.FolderName;
                }
            }
        }

        private void ddlServerProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlServerProfiles.SelectedItem == null) return;
            ClearFields();
            var selectedItem = ((ServerProfile)ddlServerProfiles.SelectedItem);
            SelectedServerProfile = selectedItem;
            txtServerName1.Text = selectedItem.FirstServerName;
            txtServerName2.Text = selectedItem.SecondServerName;
            Util.BindComboBox(ddlApplications, selectedItem.Applications, "Name", "FolderName");
            if (selectedItem.Applications != null)
                SwitchAppsControls(true);
            else
                SwitchAppsControls(false);
        }

        private void ddlApplications_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlApplications.SelectedItem == null) return;
            var selectedItem = ((WebApp)ddlApplications.SelectedItem);
            txtFolderName1.Text = selectedItem.FolderName;
            txtFolderName2.Text = selectedItem.FolderName;
        }

        private void ClearFields()
        {
            txtFolderPath.Text = string.Empty;
            txtFolderName1.Text = string.Empty;
            txtFolderName2.Text = string.Empty;
            txtServerName1.Text = string.Empty;
            txtServerName2.Text = string.Empty;
        }

        private void btnChangeDefaultLocation_Click(object sender, RoutedEventArgs e)
        {
            new EditDefaultLocationDialog().ShowDialog();
            lblDeploymentLocation.Content = $@"\{{SERVERNAME}}\{Config.Config.DefaultServerLocation}";
        }

        private void cbDeployToC_Checked(object sender, RoutedEventArgs e)
        {
            txtServerName1.Text = "c:";
            txtServerName1.IsEnabled = false;
        }

        private void cbDeployToC_Unchecked(object sender, RoutedEventArgs e)
        {
            txtServerName1.Text = SelectedServerProfile?.FirstServerName;
            txtServerName1.IsEnabled = true;
        }

        private bool autoScroll = true;
        private void svLogs_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0)
            {   // Content unchanged : user scroll event
                if (svLogs.VerticalOffset == svLogs.ScrollableHeight)
                {   // Scroll bar is in bottom
                    // Set auto-scroll mode
                    autoScroll = true;
                }
                else
                {   // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    autoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (autoScroll && e.ExtentHeightChange != 0)
            {   // Content changed and auto-scroll mode set
                // Autoscroll
                svLogs.ScrollToVerticalOffset(svLogs.ExtentHeight);
            }
        }
    }
}

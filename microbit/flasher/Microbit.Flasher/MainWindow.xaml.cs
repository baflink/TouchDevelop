﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.updateStatus("loading...");
            var downloads = KnownFoldersNativeMethods.GetDownloadPath();
            if (downloads == null)
            {
                this.updateStatus("oops, can't find the Downloads folder");
                return;
            }

            var watcher = new FileSystemWatcher(downloads);
            watcher.Renamed += (sender, e) => handleFileEvent(e);
            watcher.Created += (sender, e) => handleFileEvent(e);
            watcher.EnableRaisingEvents = true;

            this.updateStatus("Ready to copy your .hex file to your BBC micro:bit.");
            this.handleActivation();
        }

        private void handleActivation()
        {
            if (AppDomain.CurrentDomain.SetupInformation != null && AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null)
            {
                var ac = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                if (ac != null && ac.Length > 0 && !string.IsNullOrEmpty(ac[0]))
                {
                    try
                    {
                        var uri = new Uri(ac[0]);
                        var path = uri.AbsolutePath;
                        ThreadPool.QueueUserWorkItem(data => this.handleFile(path));
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        private void updateStatus(string value) {
            Action a = () => { this.Status = value; };
            Application.Current.Dispatcher.Invoke(a);
        }

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(string), typeof(MainWindow));
        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        static string getVolumeLabel(DriveInfo di)
        {
            try { return di.VolumeLabel; }
            catch(Exception)
            {
                return "";
            }
        }

        void handleFileEvent(FileSystemEventArgs e)
        {
            this.handleFile(e.FullPath);
        }

        void handleFile(string fullPath)
        {
            try
            {
                var info = new System.IO.FileInfo(fullPath);
                if (info.Extension != ".hex")
                    return;

                this.updateStatus("detected " + info.Name);
                var drives = System.IO.DriveInfo.GetDrives();
                var drive = drives.FirstOrDefault(d => getVolumeLabel(d) == "MICROBIT");
                if (drive == null)
                {
                    this.updateStatus("no BBC micro:bit detected, did you plug it?");
                    return;
                }

                this.updateStatus("flashing " + info.Name);

                var trg = System.IO.Path.Combine(drive.RootDirectory.FullName, "firmware.hex");
                File.Copy(info.FullName, trg);

                this.updateStatus("flashed " + info.Name);
            }
            catch (Exception)
            {
                this.updateStatus("oops, something wrong happened");
            }
        }
    }
}

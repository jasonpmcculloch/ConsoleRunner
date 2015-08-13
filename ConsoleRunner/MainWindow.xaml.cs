﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Timers;
using System.Configuration;

namespace ConsoleRunner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _autoScroll = true;
        private List<Process> _processes = new List<Process>();
        private Timer _processCheck = new Timer(1000);
        private const char _processPartSeparator = ';';

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            _processCheck.Elapsed += _processCheck_Elapsed;

            AutoScroll.IsChecked = _autoScroll;
            AutoScroll.Checked += new RoutedEventHandler((send, args) => { _autoScroll = ((CheckBox)send).IsChecked.Value; });
            AutoScroll.Unchecked += new RoutedEventHandler((send, args) => { _autoScroll = ((CheckBox)send).IsChecked.Value; });

            this.Title = WindowTitle;
            LoadButtons();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UpdateText(string.Format("ERROR: The following unexpected error was raised within the application:\r{0}\r{1}",
                ((Exception)e.ExceptionObject).Message, ((Exception)e.ExceptionObject).StackTrace));
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            UpdateText(string.Format("ERROR: The following unexpected error was raised within the application:\r{0}\r{1}", 
                e.Exception.Message, e.Exception.StackTrace));
        }

        private string WindowTitle
        {
            get
            {
                return string.Format("{0} x Processes Running", _processes.Count);
            }
        }

        private void LoadButtons()
        {
            LoadButton(Button1);
            LoadButton(Button2);
            LoadButton(Button3);
            LoadButton(Button4);
            LoadButton(Button5);
            LoadButton(Button6);
        }

        private void LoadButton(Button btn)
        {
            var buttonSettings = ConfigurationManager.AppSettings[btn.Name];
            var buttonSettingParts = buttonSettings.Split(_processPartSeparator);

            if (buttonSettingParts.Length > 0)
            {
                if (buttonSettingParts[0].Length > 0)
                {
                    if (buttonSettingParts.Length < 4)
                    {
                        throw new ArgumentNullException(btn.Name, string.Format("We were expecting the application setting '{0}' to contain 4 parts like the following: <caption>{1}<tooltip>{1}<process>{1}<process_args>", btn.Name, _processPartSeparator));
                    }

                    var caption = buttonSettingParts[0];
                    var toolTip = buttonSettingParts[1];
                    var process = buttonSettingParts[2];
                    var processArgs = buttonSettingParts[3];

                    btn.Content = caption;
                    btn.ToolTip = toolTip.Length > 0 ? string.Format("{0}\r{1} {2}", toolTip, process, processArgs) : null;
                    btn.Tag = new List<string> { process, processArgs };
                }
                else
                {
                    btn.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                btn.Visibility = Visibility.Hidden;
            }
        }

        private void HandleButtonClick(Button btn)
        {
            var processParts = btn.Tag as List<string>;
            KickOffProcess(processParts[0], processParts[1]);
        }

        private void _processCheck_Elapsed(object sender, ElapsedEventArgs e)
        {
            _processCheck.Stop();
            if (_processes.Count > 0) {
                for (int index = _processes.Count - 1; index >= 0; index--)
                {
                    var exited = _processes[index].HasExited;
                }
                _processCheck.Start();
            }
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            HandleButtonClick(sender as Button);
        }

        private void ClearTextButton_Click(object sender, RoutedEventArgs e)
        {
            ClearText();
        }

        private void KickOffProcess(string fileName, string args)
        {
            if (fileName.Length > 0)
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = fileName;
                start.Arguments = args;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;

                Process p = new Process();
                _processes.Add(p);
                p.StartInfo = start;
                p.OutputDataReceived += new DataReceivedEventHandler((procSender, procEventArgs) =>
                {
                    UpdateText(procEventArgs.Data);
                }
                );
                p.ErrorDataReceived += new DataReceivedEventHandler((procSender, procEventArgs) => { UpdateText(procEventArgs.Data); });
                p.Exited += new EventHandler((procSender, procEventArgs) => { CleanupProcess((Process)procSender); });
                p.Start();
                p.BeginOutputReadLine();
                p = null;

                UpdateWindowTitle();
                _processCheck.Start();
            }
            else
            {
                throw new ArgumentNullException("Process filename", "There is no process defined to execute");
            }
        }

        private void CleanupProcess(Process proc)
        {
            try
            {
                UpdateText("********** Exited **********");
                _processes.Remove(proc);
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                UpdateText(string.Format("ERROR: {0}", ex.Message));
            }
        }

        private void UpdateWindowTitle()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                Title = WindowTitle;
            }), DispatcherPriority.Render);
        }

        private void UpdateText(string data)
        {
            Dispatcher.Invoke((Action)(() => 
            {
                textBox.AppendText(data + Environment.NewLine);
                if (_autoScroll)
                {
                    textBox.ScrollToEnd();
                }
            }), DispatcherPriority.Render);
        }

        private void ClearText()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                textBox.Text = string.Empty;
            }), DispatcherPriority.Render);
        }
    }
}

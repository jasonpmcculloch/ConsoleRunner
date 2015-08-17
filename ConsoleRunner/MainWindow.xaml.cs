using System;
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

            try
            {
                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                ClearText();
                _processCheck.Elapsed += _processCheck_Elapsed;

                AutoScroll.IsChecked = _autoScroll;
                AutoScroll.Checked += new RoutedEventHandler((send, args) => { _autoScroll = ((CheckBox)send).IsChecked.Value; });
                AutoScroll.Unchecked += new RoutedEventHandler((send, args) => { _autoScroll = ((CheckBox)send).IsChecked.Value; });

                this.Title = WindowTitle;
                LoadButtons();
            }
            catch (Exception ex)
            {
                UpdateText(string.Format("CRITICAL ERROR ENCOUNTERED ON STARTUP\r{0}\r{1}", ex.Message, ex.TargetSite.Name));
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            UpdateText(string.Format("\r********** ERROR: {0} ({1}) **********", 
                e.Exception.Message, e.Exception.TargetSite.Name));
            e.Handled = true;
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
            try
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
            catch(Exception ex)
            {
                throw new Exception(string.Format("Exception while loading configuration for {0}\r{1}", btn.Name, ex.Message));
            }
        }

        private void HandleButtonClick(Button btn)
        {
            var processParts = btn.Tag as List<string>;
            if (processParts != null)
            {
                if (processParts.Count < 2)
                {
                    KickOffProcess(processParts[0], string.Empty);
                }
                else
                {
                    KickOffProcess(processParts[0], processParts[1]);
                }
            }
            else
            {
                throw new ArgumentNullException(string.Format("appSettings.{0}", btn.Name), "Button does not have process details defined");
            }
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
            try {
                if (fileName.Length > 0)
                {
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = fileName;
                    start.Arguments = args;
                    start.UseShellExecute = false;
                    start.RedirectStandardOutput = true;
                    start.CreateNoWindow = true;

                    Process proc = new Process();
                    _processes.Add(proc);
                    proc.StartInfo = start;
                    proc.OutputDataReceived += new DataReceivedEventHandler((procSender, procEventArgs) =>
                    {
                        UpdateText(procEventArgs.Data);
                    }
                    );
                    proc.ErrorDataReceived += new DataReceivedEventHandler((procSender, procEventArgs) => { UpdateText(procEventArgs.Data); });
                    proc.Exited += new EventHandler((procSender, procEventArgs) => { CleanupProcess((Process)procSender); });
                    UpdateText(string.Format("********** Starting [{0}]**********", proc.StartInfo.FileName));
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc = null;

                    UpdateWindowTitle();
                    _processCheck.Start();
                }
                else
                {
                    throw new ArgumentNullException("Process filename", "There is no process defined to execute");
                }
            } catch (Exception ex)
            {
                throw new Exception(string.Format("{0} - {1} ({2} {3})", ex.Message, ex.TargetSite.Name, fileName, args));
            }
        }

        private void CleanupProcess(Process proc)
        {
            try
            {
                UpdateText(string.Format("********** Exited [{0}]**********", proc.StartInfo.FileName));
                _processes.Remove(proc);
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                UpdateText(string.Format("********** ERROR: {0} **********", ex.Message));
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

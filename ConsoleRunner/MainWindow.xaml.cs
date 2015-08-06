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

namespace ConsoleRunner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Process proc = new Process();
            //proc.StartInfo.FileName = "netstat.exe";
            //proc.StartInfo.Arguments = "-a";
            //proc.StartInfo.UseShellExecute = false;
            //proc.StartInfo.RedirectStandardOutput = true;
            //proc.StartInfo.CreateNoWindow = true;
            //proc.Start();

            //Dispatcher.Invoke((Action)(() => {
            //    textBox.Text = proc.StandardOutput.ReadToEnd();
            //}), DispatcherPriority.Render);
            

            //proc.WaitForExit();

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"netstat.exe";
            start.Arguments = "-a";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.CreateNoWindow = true;

            //using (Process process = Process.Start(start))
            //{
            //    using (StreamReader reader = process.StandardOutput)
            //    {
            //        string result = "";

            //        while((result = reader.ReadLine()) != null)
            //        {
            //            Dispatcher.Invoke((Action)(() => {
            //                textBox.AppendText(result + Environment.NewLine);
            //            }), DispatcherPriority.Render);

            //        }
            //    }
            //}

            using (Process p = Process.Start(start))
            {
                p.OutputDataReceived += new DataReceivedEventHandler((procSender, procEventArgs) =>
                    {
                        UpdateText(procEventArgs.Data);
                    }
                );
                p.ErrorDataReceived += new DataReceivedEventHandler((procSender, procEventArgs) => { UpdateText(procEventArgs.Data); });

                p.Start();
                p.BeginOutputReadLine();
            }
        }

        private void UpdateText(string data)
        {
            Dispatcher.Invoke((Action)(() => 
            {
                textBox.AppendText(data + Environment.NewLine);
                textBox.ScrollToEnd();
            }), DispatcherPriority.Render);
        }
    }
}

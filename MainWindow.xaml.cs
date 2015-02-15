using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.Win32;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Media.Animation;

namespace wpfGreyZUpdator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebClient wcFile;

        System.Drawing.Point pt,
                             ptOffset;

        int downloadStartTime,
            mouseDown;

        // can you prevent it from jumping away from the center click?


        public MainWindow()
        {
            InitializeComponent();
        }

        private void imgMinimize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = System.Windows.WindowState.Minimized;
        }

        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Grid_Initialized_1(object sender, EventArgs e)
        {
            Thread t;

            t = new Thread(task);
            t.Start();

            t = new Thread(drag);
            t.Start();
        }

        public int GetFileSize(string file)
        {
            int ret;

            FileInfo fi;


            ret = 0;

            if (File.Exists(file) == true)
            {
                fi = new FileInfo(file);
                ret = (int)fi.Length;
            }

            return ret;
        }

        public void drag()
        {
            while (true)
            {
                if (mouseDown == 1)
                {
                    Dispatcher.Invoke(new Action(delegate()
                    {
                        //Left = System.Windows.Forms.Cursor.Position.X - pt.X - ptOffset.X;
                       // Top = System.Windows.Forms.Cursor.Position.Y - pt.Y - ptOffset.Y;
                    }));
                }

                Thread.Sleep(10);
            }
        }

        private void task()
        {
            string dir,
                    dirOA,
                  file;

            int iCount, 
                size;

            bool run,
                 check;

            FolderBrowserDialog folderDialog;

            RegistryKey key;

            List<string> list,
                         cols,
                         registrySources;

            try
            {

                registrySources = new List<string>();
                registrySources.Add("SOFTWARE\\Wow6432Node\\Bohemia Interactive Studio\\ArmA 2 OA");
                registrySources.Add("SOFTWARE\\Bohemia Interactive Studio\\ArmA 2 OA");
                registrySources.Add("SOFTWARE\\Wow6432Node\\Bohemia Interactive Studio\\ArmA 2");
                registrySources.Add("SOFTWARE\\Bohemia Interactive Studio\\ArmA 2");
                registrySources.Add("SOFTWARE\\Bohemia Interactive Studio\\ArmA 2 Free");

                int i = 0;
                iCount = 0;
                dir = null;
                key = null;
                dirOA = null;
                
                foreach (string s in registrySources)
                {
                    i = i + 1;
                    if (dir == null)
                    {
                        if (key != null)
                            key.Close();
                        key = Registry.LocalMachine.CreateSubKey(s);
                        dir = (string)key.GetValue("main");
                        //System.Windows.Forms.MessageBox.Show("derp:" + i + ":" + dir);
                        //dir = null;
                    }
                }
                
                run = true;
                wcFile = new WebClient();
                wcFile.DownloadProgressChanged += wcFile_DownloadProgressChanged;

                while (run == true)
                {
                    if (File.Exists("update.bat") == true)
                        File.Delete("update.bat");

                    key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\greyz updater");
                    dir = (string)key.GetValue("arma 2 directory");

                    if (dir != null)
                    {
                        dir = dir.Replace(" Operation Arrowhead", "");// this will reset it to just arma 2 if the arrowhead folder is found instead so that the code below works
                        dirOA = dir + " Operation Arrowhead\\";
                        using (WebClient wc = new WebClient())
                            list = wc.DownloadString("http://data.greyz.org/launcher/list.asp").Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        foreach (string s in list)
                        {
                            cols = s.Split(new char[] { ',' }, StringSplitOptions.None).ToList();
                            size = int.Parse(cols[1]);
                            file = dirOA + "\\" + cols[3] + "\\" + cols[0];

                            if (GetFileSize(file) != size)
                            {
                                Dispatcher.Invoke(new Action(delegate()
                                {
                                btnServer2.IsEnabled = false;
                                }));
                                if (Directory.Exists(dirOA + "\\" + cols[3]) == false)
                                    Directory.CreateDirectory(dirOA + "\\" + cols[3]);

                                Dispatcher.Invoke(new Action(delegate() 
                                {
                                    labFile.Content = "\"" + cols[0] + "\"";
                                }));

                                downloadStartTime = Environment.TickCount / 1000;
                                wcFile.DownloadFileAsync(new Uri(cols[2]), file);

                                check = true;
                                Dispatcher.Invoke(new Action(delegate()
                                {
                                    pbMain.Width = 0;
                                }));

                                while (check == true)
                                {
                                    Dispatcher.Invoke(new Action(delegate()
                                    {
                                        if (pbMain.Width == 300)
                                            check = false;
                                    }));

                                    Thread.Sleep(1000);
                                }

                                Dispatcher.Invoke(new Action(delegate()
                                {
                                    pbMain.Width = 0;
                                }));

                                if (cols[0] == "GreyZ.exe")
                                {
                                    using (StreamWriter sw = new StreamWriter("update.bat"))
                                    {
                                        sw.WriteLine("ping -n 5 127.0.0.1 > nul");
                                        sw.WriteLine("copy /B /Y \"" + file + "\" \"" + Process.GetCurrentProcess().MainModule.FileName.Replace(".vshost", "") + "\"");
                                        sw.WriteLine("start \"\" \"" + Process.GetCurrentProcess().MainModule.FileName.Replace(".vshost", "") + "\"");
                                    }

                                    Process.Start("update.bat");
                                    Environment.Exit(0);
                                }
                            }

                            
                        }



                        // allows us to see how many times this has hit the server. 
                        iCount = iCount + 1;

                        Dispatcher.Invoke(new Action(delegate()
                        {
                            btnServer2.IsEnabled = true;
                            labFile.Content = "GreyZ is up to date.";
                            labStatus.Content = "";
                            labStatus.Content = iCount + " times";
                        }));

                        Thread.Sleep(120000);
                    }
                    else
                    {
                        foreach (string s in registrySources)
                        {
                            if (dir == null)
                            {
                                key.Close();
                                key = Registry.LocalMachine.CreateSubKey(s);
                                dir = (string)key.GetValue("main");
                            }
                        }

                        if (dir != null)
                        {
                            key.Close();
                            key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\greyz updater");
                            key.SetValue("arma 2 directory", dir, RegistryValueKind.String);
                            key.Close();
                        }
                        else
                        {
                            folderDialog = new FolderBrowserDialog();
                            folderDialog.Description = "Find the Arma 2 Folder";

                            Dispatcher.Invoke(new Action(delegate()
                            {
                                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    key.Close();
                                    key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\greyz updater");
                                    key.SetValue("arma 2 directory", folderDialog.SelectedPath, RegistryValueKind.String);
                                    key.Close();
                                }
                                else
                                {
                                    run = false;
                                    labStatus.Content = "No directory found. Closing down.";
                                }
                            }));

                            Thread.Sleep(35000);
                        }
                    }
                }
                btnServer2.IsEnabled = true;
                labStatus.Content = "Finished Updating.";
            }
            catch (Exception x)
            {
                //labStatus.Content = "ERROR : Run me as administrator dumbass";
                System.Windows.Forms.MessageBox.Show("\r\nThe error was: " + x.Message);
            }
        }

        void wcFile_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            long time,
                 rate,
                 distance;

            float mb;

            TimeSpan ts;



            Dispatcher.Invoke(new Action(delegate()
            {
                if (e.ProgressPercentage != (pbMain.Width / 3))
                {
                    time = ((Environment.TickCount / 1000) - downloadStartTime);

                    if (time != 0 && e.BytesReceived != 0)
                    {
                        rate = e.BytesReceived / time;
                        mb = (((float)e.BytesReceived / (float)time) / 1024.0f) / 1024.0f;
                        distance = (e.TotalBytesToReceive - e.BytesReceived) / rate;

                        ts = new TimeSpan(distance * 10000000);

                        labStatus.Content = (((float)e.BytesReceived / (float)e.TotalBytesToReceive) * 100.0).ToString("0.0") + "%, " + mb.ToString("0.00") + " MBps, " + ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00") + " time remaining";
                    }
                    else
                        labStatus.Content = "Downloading";
                }

                pbMain.Width = e.ProgressPercentage * 3;
            }));
        }

        private void btnServer1_Click(object sender, RoutedEventArgs e)
        {
            string dir;

            RegistryKey key;

            key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\greyz updater");
            dir = (string)key.GetValue("arma 2 directory");

            if (dir != null)
            {
                dir = dir.Replace(" Operation Arrowhead", "");// this will reset it to just arma 2 if the arrowhead folder is found instead so that the code below works
                //"C:\Program Files (x86)\Steam\steamapps\common\Arma 2 Operation Arrowhead\Expansion\beta\arma2oa.exe"
                string test = dir + " Operation Arrowhead\\" + "Expansion\\beta\\ArmA2OA.exe";
                test = "\"-mod=" + dir + ";EXPANSION;ca\" \"-mod=Expansion\\beta;Expansion\\beta\\Expansion;@DayZ\" -nosplash -connect=208.109.168.215 -port=2302";
                Directory.SetCurrentDirectory(dir + " Operation Arrowhead\\");
                Process.Start(dir + " Operation Arrowhead\\" + "Expansion\\beta\\ArmA2OA.exe", "\"-mod=" + dir + ";EXPANSION;ca\" \"-mod=Expansion\\beta;Expansion\\beta\\Expansion;@DayZ\" -nosplash -connect=208.109.168.215 -port=2302          ");
                
            }
            /*
            //LaunchBat("hello");
            if (dir != null)
                Process.Start(""C:\Program Files (x86)\Steam\steamapps\common\Arma 2 Operation Arrowhead\Expansion\beta\arma2oa.exe" "-mod=C:\Program Files (x86)\Steam\steamapps\common\Arma 2;EXPANSION;ca" "-mod=Expansion\beta;Expansion\beta\Expansion;@DayZ" -nosplash -connect=208.109.168.215 -port=2302          "

                    // can you edit this above as a test?
                    //sure
            //    Process.Start(dir + "\\" + "Expansion\\beta\\ArmA2OA.exe", "-mod=@DayZ -beta=Expansion\\beta;Expansion\\beta\\Expansion -nosplash -connect=208.109.168.215 -port=2302");
            */

            key.Close();
        }

        private void btnServer2_Click(object sender, RoutedEventArgs e)
        {
            string dir;

            RegistryKey key;



            key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\greyz updater");
            dir = (string)key.GetValue("arma 2 directory");

            if (dir != null)
            {
                dir = dir.Replace(" Operation Arrowhead", "");// this will reset it to just arma 2 if the arrowhead folder is found instead so that the code below works
                //"C:\Program Files (x86)\Steam\steamapps\common\Arma 2 Operation Arrowhead\Expansion\beta\arma2oa.exe"
                string test = dir + " Operation Arrowhead\\" + "Expansion\\beta\\ArmA2OA.exe";
                test = "\"-mod=" + dir + ";EXPANSION;ca\" \"-mod=Expansion\\beta;Expansion\\beta\\Expansion;@DayZ\" -nosplash -connect=208.109.168.215 -port=2302";
                
                //System.Windows.Forms.MessageBox.Show(dir + " Operation Arrowhead\\");
                Directory.SetCurrentDirectory(dir + " Operation Arrowhead\\");
                Process.Start(dir + " Operation Arrowhead\\" + "Expansion\\beta\\ArmA2OA.exe", "\"-mod=" + dir + ";EXPANSION;ca\" \"-mod=Expansion\\beta;Expansion\\beta\\Expansion;@DayZ;@GreyZ\" -password=AnyoneInCherno -nosplash -connect=208.109.168.215 -port=2307          ");

            }
            /*
            if (dir != null)
                Process.Start(dir + "\\Expansion\\beta\\ArmA2OA.exe", "-mod=@DayZ;@GreyZ -beta=Expansion\\beta;Expansion\\beta\\Expansion  -nosplash -connect=208.109.168.215 -port=2307 -password=DayZGreyZ420");
            */
            key.Close();
        }

        private void imgSettings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RegistryKey key;

            FolderBrowserDialog folderDialog;


            folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Find the Arma 2 Folder";

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\greyz updater");
                key.SetValue("arma 2 directory", folderDialog.SelectedPath, RegistryValueKind.String);
                key.Close();
            }
        }

        private void Image_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            mouseDown = 1;
            pt = System.Windows.Forms.Cursor.Position;
            this.DragMove();

            //ptOffset.X = pt.X - (int)this.Parent.Left;
            //ptOffset.Y = pt.Y - (int)Top;
            
        }

        private void Image_MouseUp_1(object sender, MouseButtonEventArgs e)
        {
            mouseDown = 0;
        }

        private void mustacheClick(object sender, MouseButtonEventArgs e)
        {
            DoubleAnimation da = new DoubleAnimation();
            da.From = 0;
            da.To = 360;
            da.Duration = new Duration(TimeSpan.FromSeconds(0.5));
            RotateTransform rt = new RotateTransform();
            imgMustache.RenderTransform = rt;
            rt.CenterX = 9;
            rt.CenterY = 4.5;
            rt.BeginAnimation(RotateTransform.AngleProperty, da);
        }

        private void lbl_showReleaseNotes(object sender, MouseButtonEventArgs e)
        {
            
        }

    }
}

using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XTE_ScalesDAQ.DAL;
using XTE_ScalesDAQ.Entity;

namespace XTE_ScalesDAQ
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConfigData config;
        private MainDAL dal;
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private NotifyIcon notifyIcon = null;
        private DispatcherTimer ShowTimer;
        private SerialPort serialPort;
        private static BitmapImage IFalse = new BitmapImage(new Uri("/Static/01.png", UriKind.Relative));
        private static BitmapImage ITrue = new BitmapImage(new Uri("/Static/02.png", UriKind.Relative));
        private BarCodeInfo info = null;
        private decimal weight = 0;
        private List<decimal> list = new List<decimal>();
        private bool signal = false;
        private bool ready = false;
        private DateTime time;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                LoadJsonData();
                dal = new MainDAL(config);

                var mark = GetConnection();
                StatusImage.Source = mark ? ITrue : IFalse;

                DataReload();

                #region 时间定时器

                ShowTimer = new System.Windows.Threading.DispatcherTimer();
                ShowTimer.Tick += new EventHandler(ShowTimer1);
                ShowTimer.Interval = new TimeSpan(0, 0, 0, 1);
                ShowTimer.Start();

                #endregion 时间定时器

                InitialTray();
            }
            catch (Exception ex)
            {
                log.Error("1." + ex.Message);
            }
        }

        public void ShowTimer1(object sender, EventArgs e)
        {
            this.TM.Text = " ";
            //获得年月日
            this.TM.Text += DateTime.Now.ToString("yyyy年MM月dd日");   //yyyy年MM月dd日
            this.TM.Text += "  ";
            //获得时分秒
            this.TM.Text += DateTime.Now.ToString("HH:mm:ss");
            this.TM.Text += "  ";
            this.TM.Text += DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("zh-cn"));
            this.TM.Text += "  ";
        }

        private void DataReload()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (s, e) =>
            {
                try
                {
                    info = dal.GetBarCodeInfo(config.FWNo);
                    if (info != null)
                    {
                        if (!signal)
                        {
                            time = DateTime.Now;
                            signal = true;
                        }
                        barText.Text = info.FBarCode;

                        if (ready)
                        {
                            log.Info(list.ToArray());
                            info.FWMark = 2;
                            info.FWeight = list.Average();

                            if (dal.UpdateInfo(info))
                            {
                                weight = 0;
                                barText.Text = "";
                                list.Clear();
                                signal = false;
                                ready = false;
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    log.Error("------出错------");
                    log.Error(exc.Message);
                    dispatcherTimer.Stop();
                }
            };
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);
            dispatcherTimer.Start();
        }

        /// <summary>
        /// 本地配置文件读取
        /// </summary>
        private void LoadJsonData()
        {
            try
            {
                string path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                string file = path + $"config.json";

                using (var sr = File.OpenText(file))
                {
                    string JsonStr = sr.ReadToEnd();
                    config = JsonConvert.DeserializeObject<ConfigData>(JsonStr);
                }
            }
            catch (Exception e)
            {
                log.Error("LoadJsonError:" + e.Message);
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (System.Windows.MessageBox.Show("确定退出吗?",
                                               "提示",
                                                MessageBoxButton.YesNoCancel,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                {
                    dispatcherTimer.Stop();
                }

                if (ShowTimer != null && ShowTimer.IsEnabled)
                {
                    ShowTimer.Stop();
                }
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        #region 串口连接

        private bool GetConnection()
        {
            bool mark;
            if (serialPort == null)
            {
                serialPort = new SerialPort(config.PortName, config.BaudRate, Parity.None, 8, StopBits.One)
                {
                    DtrEnable = true,
                    RtsEnable = true,
                    ReadTimeout = 1000
                };
                serialPort.ReceivedBytesThreshold = 7;
                serialPort.DataReceived += SerialPort_DataReceived;
                mark = OpenPort();
            }
            else
            {
                mark = OpenPort();
            }
            return mark;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort port = (SerialPort)sender;
                //开启接收数据线程
                Thread threadReceiveSub = new Thread(new ParameterizedThreadStart(ReceiveData));
                threadReceiveSub.Start(port);
            }
            catch (Exception ex)
            {
                log.Error("thread  " + ex.Message);
            }
        }

        private void ReceiveData(object serialPortobj)
        {
            try
            {
                SerialPort port = (SerialPort)serialPortobj;

                //防止数据接收不完整
                //Thread.Sleep(100);

                string str = port.ReadExisting();
                str = str.Trim();

                if (!string.IsNullOrEmpty(str))
                {
                    //log.Info("Get:" + str);
                    weight = TransformData(str);
                    if (signal && !ready)
                    {
                        if (time.AddSeconds(2) >= DateTime.Now)
                        {
                            list.Add(weight);
                        }
                        else
                        {
                            ready = true;
                        }
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    weightText.Text = weight.ToString() + "KG";
                });
            }
            catch (Exception ex)
            {
                log.Error("ReceiveError:" + ex.Message);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="str">±000.000kg</param>
        /// <returns></returns>
        public decimal TransformData(string str)
        {
            //log.Info("Read:" + str);
            var tmpStr = str.Substring(0, str.Length - 2);

            return decimal.Parse(tmpStr);
        }

        private bool OpenPort()
        {
            string message = null;
            try//这里写成异常处理的形式以免串口打不开程序崩溃
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                serialPort.Open();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (serialPort.IsOpen)
            {
                log.Info("磅秤连接成功！");
                return true;
            }
            else
            {
                log.Error("磅秤打开失败!原因为： " + message);
                serialPort.Close();
                return false;
            }
        }

        #endregion 串口连接

        #region 托盘

        private void InitialTray()
        {
            //隐藏主窗体
            this.Visibility = Visibility.Hidden;
            //设置托盘的各个属性
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "磅秤采集";
            notifyIcon.Visible = true;//托盘按钮是否可见
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            //鼠标点击事件
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);
            //窗体状态改变时触发
            this.StateChanged += MainWindow_StateChanged;
        }

        // 窗口状态改变，最小化托盘
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        // 托盘图标鼠标单击事件
        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //鼠标左键，实现窗体最小化隐藏或显示窗体
            if (e.Button == MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                }
            }
            if (e.Button == MouseButtons.Right)
            {
                //exit_Click(sender, e);//触发单击退出事件
                Close();
            }
        }

        // 窗体状态改变时候触发
        private void SysTray_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        #endregion 托盘

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            log.Info("重新连接开始");

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort = null;
            }

            var mark = GetConnection();
            StatusImage.Source = mark ? ITrue : IFalse;
        }
    }
}
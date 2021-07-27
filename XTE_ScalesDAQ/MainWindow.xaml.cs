using log4net;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Ports;
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

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                LoadJsonData();
                dal = new MainDAL(config);

                StatusImage.Source = ITrue;

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
                    var info = dal.GetBarCodeInfo(config.FWNo);
                    bool save = false;
                    if (info != null)
                    {
                        barText.Text = info.FBarCode;
                        // 数据采集

                        info.FWeight = (decimal)1.132;

                        info.FWMark = 2;
                        weightText.Text = info.FWeight.ToString();

                        save = dal.UpdateInfo(info);
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
                if (dispatcherTimer.IsEnabled)
                {
                    dispatcherTimer.Stop();
                }

                if (ShowTimer.IsEnabled)
                {
                    ShowTimer.Stop();
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

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
    }
}
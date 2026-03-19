using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CCD.Views
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        private int status = 0; // 用来存储属性的二进制状态
        private Timer timer;
        private bool isRunning = false;

        private readonly CCamera m_Camera;
        private float exMax;
        private float exMin;
        private float diMax;
        private float diMin;

        public SettingWindow(CCamera camera)
        {
            InitializeComponent();
            m_Camera = camera;
        }
        // 在窗体加载时获取并显示曝光时间和增益的值
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CEnumValue exEnum = new();
            m_Camera.GetEnumValue("ExposureAuto", ref exEnum);
            comboBox1.SelectedIndex = (int)exEnum.CurValue;
            if (exEnum.CurValue == 2)
            {
                UpdateStatus(0, 1);
                tbExposureTime.IsReadOnly = true;
                apply1.IsEnabled = false;
            }
            CEnumValue GainEnum = new();
            m_Camera.GetEnumValue("GainAuto", ref GainEnum);
            comboBox2.SelectedIndex = (int)GainEnum.CurValue;
            if (GainEnum.CurValue == 2)
            {
                UpdateStatus(1, 1);
                tbGain.IsReadOnly = true;
                apply2.IsEnabled = false;
            }

            GetExposureTime(true);
            GetGain(true);
        }

        // 点击应用按钮时设置新的曝光时间和增益值
        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(tbExposureTime.Text, out float newExposureTime))
            {
                if (newExposureTime >= exMin && newExposureTime <= exMax)
                {
                    //m_Camera.SetEnumValue("ExposureAuto", 0);
                    _ = m_Camera.SetFloatValue("ExposureTime", newExposureTime);
                }
            }
            else
            {
                GetExposureTime(false);
            }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            m_Camera.SetEnumValue("ExposureAuto", (uint)index);
            if (index == 2)
            {
                UpdateStatus(0, 1);
                tbExposureTime.IsReadOnly = true;
                apply1.IsEnabled = false;
            }
            else
            {
                UpdateStatus(0, 0);
                tbExposureTime.IsReadOnly = false;
                apply1.IsEnabled = true;

                if (index == 1)
                {
                    GetExposureTime(false);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(tbGain.Text, out float newGain))
            {

                if (newGain >= diMin && newGain <= diMax)
                {
                    //m_Camera.SetEnumValue("GainAuto", 0);
                    _ = m_Camera.SetFloatValue("Gain", newGain);
                }
            }
            else
            {
                GetGain(false);
            }
        }

        private void comboBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = comboBox2.SelectedIndex;
            m_Camera.SetEnumValue("GainAuto", (uint)index);
            if (index == 2)
            {
                UpdateStatus(1, 1);
                tbGain.IsReadOnly = true;
                apply2.IsEnabled = false;
            }
            else
            {
                UpdateStatus(1, 0);
                tbGain.IsReadOnly = false;
                apply2.IsEnabled = true;

                if (index == 1)
                {
                    GetGain(false);
                }
            }
        }
        private void UpdateStatus(int index, int value)
        {
            if (index != 0 && index != 1)
            {
                return;
            }

            status ^= (-value ^ status) & (1 << index);

            if (status == 0) // 如果新的状态为0且计时器正在运行，则停止计时器
            {
                StopTimer();
            }
            else if (status != 0) // 如果新的状态不为0且计时器停止，则启动计时器
            {
                StartTimer();
            }
        }

        private void TimerCallback(object state)
        {
            if ((status & (1 << 0)) != 0)
            {
                CFloatValue pcExposureTime = new CFloatValue();
                m_Camera.GetFloatValue("ExposureTime", ref pcExposureTime);
                float exposureTime = pcExposureTime.CurValue;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    tbExposureTime.Text = exposureTime.ToString();
                }));
            }

            if ((status & (1 << 1)) != 0)
            {
                CFloatValue pcDigitalShift = new CFloatValue();
                m_Camera.GetFloatValue("Gain", ref pcDigitalShift);
                float gain = pcDigitalShift.CurValue;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    tbGain.Text = gain.ToString();
                }));
            }

            // 处理属性值
        }
        private void StartTimer()
        {
            if (!isRunning)
            {
                timer = new Timer(TimerCallback, null, 0, 500);
                isRunning = true;
            }
        }

        private void StopTimer()
        {
            if (isRunning)
            {
                timer.Dispose();
                isRunning = false;
            }
        }

        private void GetGain(bool isM)
        {
            CFloatValue pcDigitalShift = new CFloatValue();
            m_Camera.GetFloatValue("Gain", ref pcDigitalShift);
            tbGain.Text = pcDigitalShift.CurValue.ToString();
            if (isM)
            {
                diMax = pcDigitalShift.Max;
                diMin = pcDigitalShift.Min;
                laGain.Content += $"({diMin}-{diMax})";
            }
        }

        private void GetExposureTime(bool isM)
        {
            CFloatValue pcExposureTime = new CFloatValue();
            m_Camera.GetFloatValue("ExposureTime", ref pcExposureTime);
            tbExposureTime.Text = pcExposureTime.CurValue.ToString();
            if (isM)
            {
                exMax = pcExposureTime.Max;
                exMin = pcExposureTime.Min;
                laEX.Content += $"({exMin}-{exMax})";
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StopTimer();
        }
    }
}

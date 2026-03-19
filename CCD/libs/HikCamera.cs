using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows;
using MvCamCtrl.NET;
using MvCamCtrl.NET.CameraParams;
using CCD.Views;
using System.Configuration;
using OperationLogManager.libs;
using System.Windows.Input;
using System.Threading;

namespace CCD.libs
{
    public class HikCamera : CamerasAbstract
    {
        private int nRet = CErrorDefine.MV_OK;
        private bool m_bIsDeviceOpen = false;       // ch:设备打开状态 | en:Is device open
        private readonly CCamera m_MyCamera;
        private static cbOutputExdelegate ImageCallback;        

        //private readonly Stopwatch stopWatch = new();
        CPixelConvertParam pcConvertParam = new();


        public HikCamera()
        {
            m_MyCamera = new CCamera();


        }

        public override bool CameraInit()
        {
            List<CCameraInfo> ltDeviceList = new();

            // ch:枚举设备 | en:Enum device
            nRet = CSystem.EnumDevices(CSystem.MV_GIGE_DEVICE | CSystem.MV_USB_DEVICE, ref ltDeviceList);
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("初始化失败");
                return false;
            }
            if (0 == ltDeviceList.Count)
            {
                MessageBox.Show("未找到设备");
                return false;
            }
            int nDevIndex = 0;
            // ch:获取选择的设备信息 | en:Get selected device information
            CCameraInfo stDevice = ltDeviceList[nDevIndex];

            nRet = m_MyCamera.CreateHandle(ref stDevice);
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("创建设备失败");
                return false;
            }
            nRet = m_MyCamera.OpenDevice();
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("打开设备失败");
                return false;
            }
            m_bIsDeviceOpen = true;
            nRet = m_MyCamera.SetEnumValue("TriggerMode", (uint)MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("设置失败");
                return false;
            }

            // ch:注册回调函数 | en:Register image callback
            ImageCallback = new cbOutputExdelegate(ImageCallbackFunc);
            nRet = m_MyCamera.RegisterImageCallBackEx(ImageCallback, IntPtr.Zero);
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("设置回调函数失败");
                return false;
            }

            nRet = m_MyCamera.SetEnumValue("PixelFormat", (uint)MvGvspPixelType.PixelType_Gvsp_BayerGB8);
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("设置回调函数失败");
                return false;
            }

            MessageBox.Show("连接成功");
            return true;

        }

        public override void CamerasClose()
        {
            // ch:关闭设备 | en:Close device
            nRet = m_MyCamera.CloseDevice();
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("关闭相机失败");
            }
            m_bIsDeviceOpen = false;

            // ch:销毁设备 | en:Destroy device
            nRet = m_MyCamera.DestroyHandle();
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("销毁相机失败");
            }
        }

        //public override void CamerasSetting()
        //{
        //    SettingWindow settingForm = new(m_MyCamera); // 创建SettingForm对象，并传入m_MyCamera
        //    settingForm.Show(); // 显示窗体
        //}

        public override void Adjust()
        {
            m_MyCamera?.SetEnumValue("ExposureAuto", 1);
            m_MyCamera?.SetEnumValue("GainAuto", 1);


            CEnumValue exEnum = new CEnumValue();
            // 轮询直到 Exposure 结束
            while (true)
            {
                m_MyCamera.GetEnumValue("ExposureStatus", ref exEnum);

                // 0 表示 Idle（曝光稳定了）
                if (exEnum.CurValue == 0)
                    break;

                Thread.Sleep(10);
            }

            // Step 2 切回手动曝光
            m_MyCamera.SetEnumValue("ExposureAuto", 0);
        }

        public override void KeepShot(out int width, out int height)
        {
            width = DefaultSize;
            height = DefaultSize;
            if (m_MyCamera == null)
            {
                return;
            }

            CIntValue pcWidth = new CIntValue();
            nRet = m_MyCamera.GetIntValue("Width", ref pcWidth);
            if (CErrorDefine.MV_OK != nRet)
            {
                return;
            }
            width = (int)pcWidth.CurValue;
            CIntValue pcHeight = new CIntValue();
            nRet = m_MyCamera.GetIntValue("Height", ref pcHeight);
            if (CErrorDefine.MV_OK != nRet)
            {
                return;
            }
            height = (int)pcHeight.CurValue;
            // ch:开启抓图 || en: start grab image
            m_MyCamera.SetEnumValue("AcquisitionMode", (uint)MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);

            nRet = m_MyCamera.StartGrabbing();
            if (CErrorDefine.MV_OK != nRet)
            {
                return;
            }

        }

        public override void OneShot()
        {
            m_MyCamera.SetEnumValue("AcquisitionMode", (uint)MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_SINGLE);
            nRet = m_MyCamera.StartGrabbing();
            if (CErrorDefine.MV_OK != nRet)
            {
                return;
            }
        }

        public override void Stop()
        {
            nRet = m_MyCamera.StopGrabbing();
            if (nRet != CErrorDefine.MV_OK)
            {
                return;
            }

        }

        public void ImageCallbackFunc(IntPtr pData, ref MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if (m_bIsDeviceOpen)
            {
                pcConvertParam.InImage = new CImage(pData, pFrameInfo.enPixelType, pFrameInfo.nFrameLen, pFrameInfo.nHeight, pFrameInfo.nWidth, 0, 0);
                pcConvertParam.OutImage.PixelType = MvGvspPixelType.PixelType_Gvsp_BGR8_Packed;
                m_MyCamera.ConvertPixelType(ref pcConvertParam);
                OnImageGet(pcConvertParam.OutImage.ImageAddr);
            }
        }

        public override void GetHW(out int width, out int height)
        {
            width = DefaultSize;
            height = DefaultSize;
            if (m_MyCamera == null)
            {
                return;
            }

            CIntValue pcWidth = new();
            nRet = m_MyCamera.GetIntValue("Width", ref pcWidth);
            if (CErrorDefine.MV_OK != nRet)
            {
                return;
            }
            width = (int)pcWidth.CurValue;
            CIntValue pcHeight = new();
            nRet = m_MyCamera.GetIntValue("Height", ref pcHeight);
            if (CErrorDefine.MV_OK != nRet)
            {
                return;
            }
            height = (int)pcHeight.CurValue;
        }
      
        public override void GetCameraPatam(out string timeOfExposure, out string gain, out string framerate)
        {
            timeOfExposure = "empty";
            gain = "empty";
            framerate = "empty";

            CFloatValue stParam = new CFloatValue();
            int nRet = m_MyCamera.GetFloatValue("ExposureTime", ref stParam);     // 曝光时间
            if (CErrorDefine.MV_OK == nRet)
            {
                timeOfExposure = stParam.CurValue.ToString("F1");     // 一位小数
            }

            nRet = m_MyCamera.GetFloatValue("Gain", ref stParam);     // 增益
            if (CErrorDefine.MV_OK == nRet)
            {
                gain = stParam.CurValue.ToString("F1");
            }

            nRet = m_MyCamera.GetFloatValue("ResultingFrameRate", ref stParam);       // 帧率
            if (CErrorDefine.MV_OK == nRet)
            {
                framerate = stParam.CurValue.ToString("F1");
            }
        }

        public override void SetCameraParam(string timeOfExposure, string gain, string framerate)
        {
            m_MyCamera.SetEnumValue("ExposureAuto", 0);
            if (string.IsNullOrWhiteSpace(timeOfExposure)) return;
            int nRet = m_MyCamera.SetFloatValue("ExposureTime", float.Parse(timeOfExposure));
            if (CErrorDefine.MV_OK != nRet)
            {
                MessageBox.Show("设置曝光时间失败！");
                LoggingService.Instance.LogError("设置曝光时间失败！");
            }
            else LoggingService.Instance.LogInfo($"设置曝光时间：{timeOfExposure}");

            //m_MyCamera.SetEnumValue("GainAuto", 0);
            //if (string.IsNullOrWhiteSpace(gain)) return;
            //nRet = m_MyCamera.SetFloatValue("GainAuto", float.Parse(gain));
            //if (nRet != CErrorDefine.MV_OK)
            //{
            //    MessageBox.Show("设置增益失败！");
            //    LoggingService.Instance.LogError("设置增益失败！");
            //}
            //else LoggingService.Instance.LogInfo($"设置增益：{gain}");

            if (string.IsNullOrWhiteSpace(framerate)) return;
            nRet = m_MyCamera.SetFloatValue("AcquisitionFrameRate", float.Parse(framerate));
            if (nRet != CErrorDefine.MV_OK)
            {
                MessageBox.Show("设置帧率失败！");
                LoggingService.Instance.LogError("设置帧率失败！");
            }
            else LoggingService.Instance.LogInfo($"设置帧率：{framerate}");
        }

        public override bool SetAutoExposure(object ob, string timeOfExposure)
        {
            if ((bool)ob)
            {
                int nRet = m_MyCamera.SetEnumValue("ExposureAuto", 2);
                if (CErrorDefine.MV_OK != nRet)
                {
                    MessageBox.Show("设置持续自动曝光出错！");
                    LoggingService.Instance.LogError("设置持续自动曝光出错！");
                    return false;
                }
                return true;
            }
            else
            {
                m_MyCamera.SetEnumValue("ExposureAuto", 0);
                int nRet = m_MyCamera.SetFloatValue("ExposureTime", float.Parse(timeOfExposure));
                if (nRet != CErrorDefine.MV_OK)
                {
                    MessageBox.Show("设置曝光时间失败!");
                    return true;
                }
                return false;
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace CCD.libs
{
    [Serializable]
    public class Calibration
    {
        public Vector careamRelative;
        public Vector careamOffset;

        public readonly OpenCvSharp.Mat rotationVector; // 相机内参数矩阵
        public readonly OpenCvSharp.Mat translationVector; // 畸变系数
        public readonly OpenCvSharp.Mat distCoeffs;  // 畸变系数，不考虑畸变时为空的 Mat 对象
        public readonly OpenCvSharp.Mat cameraMatrix;

        // 私有构造函数
        private Calibration(OpenCvSharp.Mat cameraMatrix, OpenCvSharp.Mat distCoeffs, Vector careamRelative, Point pixCenter, OpenCvSharp.Mat rotationVector, OpenCvSharp.Mat translationVector)
        {
            this.distCoeffs = distCoeffs;
            this.cameraMatrix = cameraMatrix;
            OpenCvSharp.Mat rotationMatrix = new OpenCvSharp.Mat();
            OpenCvSharp.Cv2.Rodrigues(rotationVector, rotationMatrix);
            this.rotationVector = rotationMatrix;
            this.translationVector = translationVector;
            Point point1 = GetRelativePoint(pixCenter);
            careamOffset = new Vector(point1.X, point1.Y);
            this.careamRelative = careamRelative + careamOffset;
        }
        private Calibration(Vector careamRelative, Vector careamOffset, OpenCvSharp.Mat rotationVector, OpenCvSharp.Mat translationVector, OpenCvSharp.Mat distCoeffs, OpenCvSharp.Mat cameraMatrix)
        {
            this.careamRelative = careamRelative;
            this.careamOffset = careamOffset;
            this.distCoeffs = distCoeffs;
            this.cameraMatrix = cameraMatrix;
            this.rotationVector = rotationVector;
            this.translationVector = translationVector;
        }

        public static Calibration CreateAndSaveInstance(Vector careamRelative, Point pixCenter, List<Point> points, List<Point> points2, string filePath)
        {
            double[,] cameraMatrixData = new double[,] { { 1, 0, pixCenter.X }, { 0, 1, pixCenter.Y }, { 0, 0, 1 } };
            OpenCvSharp.Mat cameraMatrix = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_64FC1, cameraMatrixData);

            OpenCvSharp.Mat rotationVector = new OpenCvSharp.Mat();
            OpenCvSharp.Mat translationVector = new OpenCvSharp.Mat();

            double[] distCoeffsData = new double[] { 0, 0, 0, 0, 0 };
            OpenCvSharp.Mat distCoeffs = new OpenCvSharp.Mat(1, 5, OpenCvSharp.MatType.CV_64FC1, distCoeffsData);

            // 将像素坐标转换为 OpenCV 的 Point2f[] 数组
            OpenCvSharp.Point2f[] pointsArray = points.Select(p => new OpenCvSharp.Point2f((float)p.X, (float)p.Y)).ToArray();
            // 创建矩阵用于存储图像坐标
            OpenCvSharp.Mat imagePointsMat = new OpenCvSharp.Mat(pointsArray.Length, 1, OpenCvSharp.MatType.CV_32FC2, pointsArray);

            // 将像素坐标转换为 OpenCV 的 Point2f[] 数组
            OpenCvSharp.Point3f[] objectPointsArray = points2.Select(p => new OpenCvSharp.Point3f((float)p.X, (float)p.Y, 0)).ToArray();
            // 创建矩阵用于存储图像坐标
            OpenCvSharp.Mat objectPointsMat = new OpenCvSharp.Mat(objectPointsArray.Length, 1, OpenCvSharp.MatType.CV_32FC3, objectPointsArray);

            OpenCvSharp.Cv2.SolvePnP(objectPointsMat, imagePointsMat, cameraMatrix, distCoeffs, rotationVector, translationVector);
            Calibration calibration = new Calibration(cameraMatrix, distCoeffs, careamRelative, pixCenter, rotationVector, translationVector);
            // 将 calibration 对象保存到二进制文件
            try
            {
                // 创建CameraData对象并设置属性值
                CameraData cameraData = new CameraData(calibration);
                // 将CameraData对象转换为Json字符串
                string jsonString = JsonConvert.SerializeObject(cameraData, Formatting.Indented);
                File.WriteAllText(filePath, jsonString);
            }
            catch
            {
                return null;
            }


            return calibration;
        }

        public static Calibration LoadInstanceFromFile(string filePath)
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                return null;
            }

            Calibration calibration;
            try
            {
                // 读取Json字符串
                string jsonFromFile = File.ReadAllText(filePath);

                // 将Json字符串转换为CameraData对象
                CameraData loadedData = JsonConvert.DeserializeObject<CameraData>(jsonFromFile);

                OpenCvSharp.Mat cameraMatrix = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_64FC1);
                cameraMatrix.SetArray(loadedData.cameraMatrix);

                OpenCvSharp.Mat rotationVector = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_64FC1);
                rotationVector.SetArray(loadedData.rotationVector);
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var num = rotationVector.At<double>(i, j);
                    }
                }

                OpenCvSharp.Mat translationVector = new OpenCvSharp.Mat(3, 1, OpenCvSharp.MatType.CV_64FC1);
                translationVector.SetArray(loadedData.translationVector);

                OpenCvSharp.Mat distCoeffs = new OpenCvSharp.Mat(1, 5, OpenCvSharp.MatType.CV_64FC1);
                distCoeffs.SetArray(loadedData.distCoeffs);

                calibration = new Calibration(loadedData.careamRelative, loadedData.careamOffset, rotationVector, translationVector, distCoeffs, cameraMatrix);
            }
            catch
            {
                return null;
            }

            return calibration;
        }

        public Point GetRelativePoint(Point point)
        {
            return ConvertToRelative(point);
        }

        private Point ConvertToRelative(Point currentPoint)
        {
            if (rotationVector != null && translationVector != null)
            {
                CameraToWorld(cameraMatrix, distCoeffs, rotationVector, translationVector, currentPoint, out Point point);
                return point;
            }
            return currentPoint;
        }


        public Point ConvertToPix(Point currentPoint)
        {
            if (rotationVector != null && translationVector != null)
            {
                WorldToCamera(cameraMatrix, distCoeffs, rotationVector, translationVector, currentPoint, out Point point);
                return point;
            }
            return currentPoint;
        }


        public void CameraToWorld(OpenCvSharp.Mat cameraMatrix, OpenCvSharp.Mat distCoeffs, OpenCvSharp.Mat rV, OpenCvSharp.Mat tV, Point imgPoints, out Point worldPoints)
        {
            OpenCvSharp.Mat imagePoint = new(1, 1, OpenCvSharp.MatType.CV_64FC2);
            imagePoint.Set(0, 0, new OpenCvSharp.Point2d(imgPoints.X, imgPoints.Y));
            OpenCvSharp.Mat undistortedPoint = new(1, 1, OpenCvSharp.MatType.CV_64FC2);
            OpenCvSharp.Cv2.UndistortPoints(imagePoint, undistortedPoint, cameraMatrix, distCoeffs, null, cameraMatrix);
            var point = undistortedPoint.At<OpenCvSharp.Point2d>(0, 0);

            // 根据公式求Zc，即s
            imagePoint = new OpenCvSharp.Mat(3, 1, OpenCvSharp.MatType.CV_64F);
            // 输入一个2D坐标点，便可以求出相应的s
            imagePoint.Set(0, 0, point.X);
            imagePoint.Set(1, 0, point.Y);
            imagePoint.Set<double>(2, 0, 1);
            double zConst = 0; // 实际坐标系的距离

            double s;
            OpenCvSharp.Mat tempMat = rV.Inv() * cameraMatrix.Inv() * imagePoint;
            OpenCvSharp.Mat tempMat2 = rV.Inv() * tV;
            s = zConst + tempMat2.At<double>(2, 0);
            s /= tempMat.At<double>(2, 0);

            OpenCvSharp.Mat wcPoint = rV.Inv() * (cameraMatrix.Inv() * s * imagePoint - tV);

            worldPoints = new Point(wcPoint.At<double>(0, 0) - careamOffset.X, wcPoint.At<double>(1, 0) - careamOffset.Y);
        }

        public void WorldToCamera(OpenCvSharp.Mat cameraMatrix, OpenCvSharp.Mat distCoeffs, OpenCvSharp.Mat rV, OpenCvSharp.Mat tV, Point worldPoints, out Point imgPoints)
        {
            OpenCvSharp.Mat wPoints = new(1, 1, OpenCvSharp.MatType.CV_64FC3);
            wPoints.Set(0, 0, new OpenCvSharp.Point3d(worldPoints.X + careamOffset.X, worldPoints.Y + careamOffset.Y, 0));

            OpenCvSharp.Mat imagePoints = new(1, 1, OpenCvSharp.MatType.CV_64FC2);
            OpenCvSharp.Cv2.ProjectPoints(wPoints, rV, tV, cameraMatrix, distCoeffs, imagePoints);
            OpenCvSharp.Point2d values = imagePoints.Get<OpenCvSharp.Point2d>(0, 0);
            imgPoints = new(values.X, values.Y);
        }
    }
    // 定义存储对象的类
    public class CameraData
    {
        public Vector careamRelative;
        public Vector careamOffset;
        public double[] rotationVector;
        public double[] translationVector;
        public double[] distCoeffs;
        public double[] cameraMatrix;

        public CameraData()
        {

        }

        public CameraData(Calibration calibration)
        {
            careamOffset = calibration.careamOffset;
            careamRelative = calibration.careamRelative;
            calibration.rotationVector.GetArray(out rotationVector);
            calibration.translationVector.GetArray(out translationVector);
            calibration.distCoeffs.GetArray(out distCoeffs);
            calibration.cameraMatrix.GetArray(out cameraMatrix);
        }
    }
}

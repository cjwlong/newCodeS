using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCD.libs
{
    internal class ImageProcessHelper
    {
        static public double CalClarity(Mat mat)
        {
            int centerX = mat.Width / 2;
            int centerY = mat.Height / 2;
            int halfWidth = mat.Width / 4;
            int halfHeight = mat.Height / 4;

            int temp_long = Math.Min(halfWidth, halfHeight);

            // 定义中心区域的矩形
            Rect centerRect = new Rect(centerX - temp_long, centerY - temp_long, temp_long * 2, temp_long * 2);
            Mat centerImage = new Mat(mat, centerRect);

            return LaplacianComputation(centerImage);
        }
        public static double LaplacianComputation(Mat matImager, double brightnessThreshold = 70)
        {
            //转换为灰度图像
            Mat gray = new Mat();
            if (matImager.Channels() > 1)
                Cv2.CvtColor(matImager, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = matImager.Clone();
            var meanBrightness = Cv2.Mean(gray);
            if (meanBrightness.Val0 < brightnessThreshold)
                return 0;
            //计算拉普拉斯方差
            Mat laplacian = new Mat();
            Mat mean = new();
            Mat stddev = new();

            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);
            Cv2.MeanStdDev(laplacian, mean, stddev);

            gray.Dispose();
            laplacian.Dispose();
            mean.Dispose();
            //return stddev * stddev.VO:
            return stddev.At<double>(0, 0);
        }

        private static bool AreValidDefinitions(double num1, double num2)
        {
            const double epsilon = 1e-6;  // 浮点数精度容差
            return !(Math.Abs(num1) < epsilon || Math.Abs(num2) < epsilon);
        }

        private static void SplitImageToLeftRightRegions(Mat source, Mat left, Mat right)
        {
            int regionWidth = source.Width / 3;
            source[new Rect(0, 0, regionWidth, source.Height)].CopyTo(left);
            source[new Rect(source.Width - regionWidth, 0, regionWidth, source.Height)].CopyTo(right);
        }

        public static double CalculateLeftRightDefinitionDifference(Mat sourceImage)
        {
            if (sourceImage?.Width < 3) return -1;

            using var leftRegion = new Mat();
            using var rightRegion = new Mat();
            SplitImageToLeftRightRegions(sourceImage, leftRegion, rightRegion);

            double leftDef = LaplacianComputation(leftRegion);
            double rightDef = LaplacianComputation(rightRegion);

            leftRegion.Dispose();
            rightRegion.Dispose();

            return AreValidDefinitions(leftDef, rightDef)
                ? Math.Abs(leftDef - rightDef)
                : -1;
        }

        private static void SplitImageToOnDownRegions(Mat source, Mat on, Mat down)
        {
            int regionHeight = source.Height * 2 / 5;
            source[new Rect(0, 0, source.Width, regionHeight)].CopyTo(on);
            source[new Rect(0, source.Height - regionHeight, source.Width, regionHeight)].CopyTo(down);
        }

        public static double CalculateOnDownDefinitionDifference(Mat sourceImage)
        {
            if (sourceImage?.Width < 3) return -1;

            using var OnRegion = new Mat();
            using var DownRegion = new Mat();
            SplitImageToOnDownRegions(sourceImage, OnRegion, DownRegion);

            double OnDef = LaplacianComputation(OnRegion);
            double DownDef = LaplacianComputation(DownRegion);

            OnRegion.Dispose();
            DownRegion.Dispose();

            return AreValidDefinitions(OnDef, DownDef)
                ? Math.Abs(OnDef - DownDef)
                : -1;
        }
    }
}

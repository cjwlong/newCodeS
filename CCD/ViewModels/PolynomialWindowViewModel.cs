using CCD.tools;
using CCD.Views;
using netDxf.Entities;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;

namespace CCD.ViewModels
{
    public class PolynomialWindowViewModel : BindableBase
    {
        private List<Point> _points;
        public List<Point> Points
        {
            get { return _points; }
            set { SetProperty(ref _points, value); }
        }

        private List<Point> _realPoints;
        public List<Point> RealPoints
        {
            get { return _realPoints; }
            set { SetProperty(ref _realPoints, value); }
        }

        private int _selectIndex;
        public int SelectIndex
        {
            get { return _selectIndex; }
            set { SetProperty(ref _selectIndex, value); }
        }


        public Point MirrorPoint { get; set; }
        public Point CameraPoint { get; set; }

        public PolynomialWindow WidPoly{ get; set; }
        

        public string RowNum { get; set; } = "3";
        public string ColNum { get; set; } = "3";
        public string SpacingText { get; set; }

        private bool _isClii;
        public bool IsClii
        {
            get { return _isClii; }
            set { SetProperty(ref _isClii, value); }
        }
        private string _resultText;
        public string ResultText
        {
            get { return _resultText; }
            set { SetProperty(ref _resultText, value); }
        }

       // public delegate List<Point> PointListDelegate();
        

        public delegate List<Point> PointListDelegate(
         PolynomialWindow wid
       );
        public PointListDelegate listDelegate;

        public void CreateReal()
        {
            if (RealPoints == null || Points.Count != RealPoints.Count)
            {
                MessageBox.Show("生成坐标与定位点数量不符！");
                return;
            }

            ResultText = CoordinateHelper.Instance.FittingCoord(MirrorPoint, CameraPoint, Points, RealPoints);
            IsClii = false;
        }


        public void Validate()
        {
            if (!int.TryParse(RowNum, out int rows) || !int.TryParse(ColNum, out int columns) || !double.TryParse(SpacingText, out double spacing))
            {
                MessageBox.Show("请输入合法的整数和浮点数！");
                return;
            }

            //if (rows <= 1 || rows % 2 != 1 || columns <= 1 || columns % 2 != 1)
            //{
            //    MessageBox.Show("行数和列数必须为大于1的奇数！");
            //    return;
            //}
            if (rows * columns != Points.Count)
            {
                MessageBox.Show("生成坐标与定位点数量不符！");
                return;
            }
            RealPoints = GeneratePoints(rows, columns, spacing, SelectIndex);
            IsClii = true;
        }

        public bool NextStep()
        {
            List<System.Windows.Point> points = null;
            try
            {
                points = listDelegate?.Invoke(WidPoly);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            if (points == null)// || points.Count < 9|| points.Count>9)
            {
                //MessageBox.Show("请添加定位点!");
                WidPoly.Close();//WindowState = WindowState.Minimized;
                return false;
            }
            Points = SortPoint(points, 12);
            return true;
        }
         public   List<Point>   SortPoint(List<System.Windows.Point> points,double tolerance)
        {
            var sorted = points.OrderBy(p => p.Y).ToList();

            // 2. 分行
            List<List<System.Windows.Point>> rows = new List<List<System.Windows.Point>>();
            List<System.Windows.Point> currentRow = new List<System.Windows.Point>();
            double? rowY = null;

            foreach (var p in sorted)
            {
                if (rowY == null)
                {
                    currentRow.Add(p);
                    rowY = p.Y;
                }
                else
                {
                    if (Math.Abs(p.Y - rowY.Value) <= tolerance)
                    {
                        currentRow.Add(p);
                    }
                    else
                    {
                        // 结束上一行
                        currentRow = currentRow.OrderBy(pt => pt.X).ToList();
                        rows.Add(currentRow);

                        // 开始新行
                        currentRow = new List<System.Windows.Point> { p };
                        rowY = p.Y;
                    }
                }
            }

            // 添加最后一行
            if (currentRow.Count > 0)
            {
                currentRow = currentRow.OrderBy(pt => pt.X).ToList();
                rows.Add(currentRow);
            }

            // 合并所有行
            var finalSorted = rows.SelectMany(r => r).ToList();

            foreach (var p in finalSorted)
            {
                Console.WriteLine(p);
            }
            return finalSorted;
        }
        public void ReturnStep()
        {
            Points = null;
            RealPoints = null;
            ResultText = "";
            IsClii = false;
        }

        private List<Point> GeneratePoints(int rows, int columns, double interval, int mode)
        {

            List<Point> points = new List<Point>();

            // 计算中心点的坐标
            int centerX = columns / 2;
            int centerY = rows / 2;
            // 生成二维数组表示的点
            //switch (mode)
            //{
            //    case 0:
            //        for (int i = 0; i < rows; i++)
            //        {
            //            for (int j = 0; j < columns; j++)
            //            {
            //                double x = (j - centerX) * interval;
            //                double y = (centerY - i) * interval;
            //                points.Add(new Point(x, y));
            //            }
            //        }
            //        break;
            //    case 1:
            //        for (int i = 0; i < rows; i++)
            //        {
            //            for (int j = 0; j < columns; j++)
            //            {
            //                double x = (centerX - j) * interval;
            //                double y = (centerY - i) * interval;
            //                points.Add(new Point(y, x));
            //            }
            //        }
            //        break;
            //    case 2:
            //        for (int i = 0; i < rows; i++)
            //        {
            //            for (int j = 0; j < columns; j++)
            //            {
            //                double x = (centerX - j) * interval;
            //                double y = (i - centerY) * interval;
            //                points.Add(new Point(x, y));
            //            }
            //        }
            //        break;
            //    case 3:
            //        for (int i = 0; i < rows; i++)
            //        {
            //            for (int j = 0; j < columns; j++)
            //            {
            //                double x = (j - centerX) * interval;
            //                double y = (i - centerY) * interval;
            //                points.Add(new Point(y, x));
            //            }
            //        }
            //        break;
            //}

            for (int i = 0; i < rows; i++)// xy右手坐标系
            {
                for (int j = 0; j < columns; j++)
                {
                    double x = (j - centerX) * interval;
                   
                    double y = (centerY - i) * interval;
                    if (x == 0.0)
                    {

                    }
                    else
                    {
                        x = -x;
                    }
           
                    points.Add(new Point(x, y));
                }
            }

            return points;
        }
    }
}

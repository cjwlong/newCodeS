using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.libs
{
    public class GlobalCraftPara : BindableBase
    {
        private string _sourceFile_path = Path.Combine(Directory.GetCurrentDirectory(), "YuankeShi_PL.txt");
        public string SourceFile_path
        {
            get { return _sourceFile_path; }
            set 
            {
                if (value == _sourceFile_path || string.IsNullOrEmpty(_sourceFile_path)) return;
                SetProperty(ref _sourceFile_path, value); }
        }

        private string _targetFile_path = Path.Combine(Directory.GetCurrentDirectory(), "NewkeShi_PL.txt");
        public string TargetFile_path
        {
            get { return _targetFile_path; }
            set 
            {
                if (value == _targetFile_path || string.IsNullOrEmpty(TargetFile_path)) return;
                SetProperty(ref _targetFile_path, value);
            }
        }

        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set
            {                
                SetProperty(ref _fileName, value);
            }
        }

        private double _BallDiameter;
        public double BallDiameter
        {
            get { return _BallDiameter; }
            set { SetProperty(ref _BallDiameter, value); }
        }

        private double _xProcessPlace;
        public double XProcessPlace
        {
            get { return _xProcessPlace; }
            set { SetProperty(ref _xProcessPlace, value); }
        }

        private double _yProcessPlace;
        public double YProcessPlace
        {
            get { return _yProcessPlace; }
            set { SetProperty(ref _yProcessPlace, value); }
        }

        private double _zProcessPlace;
        public double ZProcessPlace
        {
            get { return _zProcessPlace; }
            set { SetProperty(ref _zProcessPlace, value); }
        }

        private double _aProcessPlace;
        public double AProcessPlace
        {
            get { return _aProcessPlace; }
            set { SetProperty(ref _aProcessPlace, value); }
        }

        private double _bProcessPlace;
        public double BProcessPlace
        {
            get { return _bProcessPlace; }
            set { SetProperty(ref _bProcessPlace, value); }
        }
    }
}

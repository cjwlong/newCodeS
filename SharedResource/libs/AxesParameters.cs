using Prism.Mvvm;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SharedResource.libs
{
    public class AxesParameters : BindableBase
    {
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

        private double _xSpeed;
        [Range(0, 100, ErrorMessage = "速度必须在0-100之间")]
        public double XSpeed
        {
            get { return _xSpeed; }
            set { SetProperty(ref _xSpeed, value); }
        }

        private double _ySpeed;
        [Range(0, 100, ErrorMessage = "速度必须在0-100之间")]
        public double YSpeed
        {
            get { return _ySpeed; }
            set { SetProperty(ref _ySpeed, value); }
        }
        private double _zSpeed;
        [Range(0, 100, ErrorMessage = "速度必须在0-100之间")]
        public double ZSpeed
        {
            get { return _zSpeed; }
            set { SetProperty(ref _zSpeed, value); }
        }

        private double _aSpeed;
        [Range(0, 100, ErrorMessage = "速度必须在0-100之间")]
        public double ASpeed
        {
            get { return _aSpeed; }
            set { SetProperty(ref _aSpeed, value); }
        }

        private double _bSpeed;
        [Range(0, 100, ErrorMessage = "速度必须在0-100之间")]
        public double BSpeed
        {
            get { return _bSpeed; }
            set { SetProperty(ref _bSpeed, value); }
        }

        private double _xAccelerate;
        [Range(0, 100, ErrorMessage = "加速度必须在0-100之间")]
        public double XAccelerate
        {
            get { return _xAccelerate; }
            set { SetProperty(ref _xAccelerate, value); }
        }

        private double _yAccelerate;
        [Range(0, 100, ErrorMessage = "加速度必须在0-100之间")]
        public double YAccelerate
        {
            get { return _yAccelerate; }
            set { SetProperty(ref _yAccelerate, value); }
        }

        private double _zAccelerate;
        [Range(0, 100, ErrorMessage = "加速度必须在0-100之间")]
        public double ZAccelerate
        {
            get { return _zAccelerate; }
            set { SetProperty(ref _zAccelerate, value); }
        }

        private double _aAccelerate;
        [Range(0, 100, ErrorMessage = "加速度必须在0-100之间")]
        public double AAccelerate
        {
            get { return _aAccelerate; }
            set { SetProperty(ref _aAccelerate, value); }
        }

        private double _bAccelerate;
        [Range(0, 100, ErrorMessage = "加速度必须在0-100之间")]
        public double BAccelerate
        {
            get { return _bAccelerate; }
            set { SetProperty(ref _bAccelerate, value); }
        }

        private double _xDecelerate;
        [Range(0, 100, ErrorMessage = "减速度必须在0-100之间")]
        public double XDecelerate
        {
            get { return _xDecelerate; }
            set { SetProperty(ref _xDecelerate, value); }
        }

        private double _yDecelerate;
        [Range(0, 100, ErrorMessage = "减速度必须在0-100之间")]
        public double YDecelerate
        {
            get { return _yDecelerate; }
            set { SetProperty(ref _yDecelerate, value); }
        }

        private double _zDecelerate;
        [Range(0, 100, ErrorMessage = "减速度必须在0-100之间")]
        public double ZDecelerate
        {
            get { return _zDecelerate; }
            set { SetProperty(ref _zDecelerate, value); }
        }

        private double _aDecelerate;
        [Range(0, 100, ErrorMessage = "减速度必须在0-100之间")]
        public double ADecelerate
        {
            get { return _aDecelerate; }
            set { SetProperty(ref _aDecelerate, value); }
        }

        private double _bDecelerate;
        [Range(0, 100, ErrorMessage = "减速度必须在0-100之间")]
        public double BDecelerate
        {
            get { return _bDecelerate; }
            set { SetProperty(ref _bDecelerate, value); }
        }

        private double _xMAXProcessSpeed;
        [Range(0, double.MaxValue)]
        public double XMAXProcessSpeed
        {
            get { return _xMAXProcessSpeed; }
            set { SetProperty(ref _xMAXProcessSpeed, value); }
        }

        private double _yMAXProcessSpeed;
        [Range(0, double.MaxValue)]
        public double YMAXProcessSpeed
        {
            get { return _yMAXProcessSpeed; }
            set { SetProperty(ref _yMAXProcessSpeed, value); }
        }

        private double _zMAXProcessSpeed;
        [Range(0, double.MaxValue)]
        public double ZMAXProcessSpeed
        {
            get { return _zMAXProcessSpeed; }
            set { SetProperty(ref _zMAXProcessSpeed, value); }
        }

        private double _aMAXProcessSpeed;
        [Range(0, double.MaxValue)]
        public double AMAXProcessSpeed
        {
            get { return _aMAXProcessSpeed; }
            set { SetProperty(ref _aMAXProcessSpeed, value); }
        }

        private double _bMAXProcessSpeed;
        [Range(0, double.MaxValue)]
        public double BMAXProcessSpeed
        {
            get { return _bMAXProcessSpeed; }
            set { SetProperty(ref _bMAXProcessSpeed, value); }
        }

        public Dictionary<string, string> Validate()
        {
            var validationResults = new Dictionary<string, string>();
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(this, context, results, true))
            {
                foreach (var result in results)
                {
                    validationResults[result.MemberNames.First()] = result.ErrorMessage;
                }
            }
            return validationResults;
        }
    }
}
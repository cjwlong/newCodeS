using Prism.Mvvm;
using SharedResource.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace ParamConfigManager.libs
{
    public class PositionModel : BindableBase
    {
        private Configfile_type _type = Configfile_type.none;
        public Configfile_type Type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private bool _isAbsolute = true;
        public bool IsAbsolute
        {
            get => _isAbsolute;
            set => SetProperty(ref _isAbsolute, value);
        }

        private double _aLimitPlace;
        public double ALimitPlace
        {
            get { return _aLimitPlace; }
            set { SetProperty(ref _aLimitPlace, value); }
        }

        private double _bLimitPlace;
        public double BLimitPlace
        {
            get { return _bLimitPlace; }
            set { SetProperty(ref _bLimitPlace, value); }
        }

        private double _xLimitPlace;
        public double XLimitPlace
        {
            get { return _xLimitPlace; }
            set { SetProperty(ref _xLimitPlace, value); }
        }

        private double _yLimitPlace;
        public double YLimitPlace
        {
            get { return _yLimitPlace; }
            set { SetProperty(ref _yLimitPlace, value); }
        }

        private double _zLimitPlace;
        public double ZLimitPlace
        {
            get { return _zLimitPlace; }
            set { SetProperty(ref _zLimitPlace, value); }
        }

        private double _xPresetPlace;
        public double XPresetPlace
        {
            get { return _xPresetPlace; }
            set { SetProperty(ref _xPresetPlace, value); }
        }

        private double _yPresetPlace;
        public double YPresetPlace
        {
            get { return _yPresetPlace; }
            set { SetProperty(ref _yPresetPlace, value); }
        }

        private double _zPresetPlace;
        public double ZPresetPlace
        {
            get { return _zPresetPlace; }
            set { SetProperty(ref _zPresetPlace, value); }
        }

        private double _aPresetPlace;
        public double APresetPlace
        {
            get { return _aPresetPlace; }
            set { SetProperty(ref _aPresetPlace, value); }
        }

        private double _bPresetPlace;
        public double BPresetPlace
        {
            get { return _bPresetPlace; }
            set { SetProperty(ref _bPresetPlace, value); }
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
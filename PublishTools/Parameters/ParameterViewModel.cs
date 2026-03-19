using OperationLogManager.libs;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedResource.Parameters
{
    public class ParameterViewModel<T> : BindableBase
    {
        private ParameterMeg<T> parameterMeg = new();
        public ParameterViewModel()
        {
            PreDataChange += (old_value, new_value) =>
            {
                if (!new_value.Equals(old_value))
                {
                    LoggingService.Instance.LogInfo($"{Name} 修改：({old_value})->({new_value})");
                }
                return false;
            };
        }
        public ParameterViewModel(int Id, string Name, T Value, string Description)
        {
            this.Id = Id;
            this.Name = Name;
            parameterMeg.Value = Value;
            this.Description = Description;

            PreDataChange += (old_value, new_value) =>
            {
                if (!new_value.Equals(old_value))
                {
                    LoggingService.Instance.LogInfo($"{Name} 修改：({old_value})->({new_value})");
                }
                return false;
            };
        }

        IContainerProvider containerProvider;

        /// <summary>
        /// 值改变之前触发，
        /// 参数：OldValue，NewVlaue
        /// 返回值：handled
        /// </summary>
        public Func<T, T, bool> PreDataChange { get; set; }
        public Action<T> DataChanged { get; set; }
        /// <summary>
        /// 参数Id
        /// </summary>
        public int Id { get => parameterMeg.Id; set => SetProperty(ref parameterMeg.Id, value); }
        public string Name { get => parameterMeg.Name; set => SetProperty(ref parameterMeg.Name, value); }
        public T Value
        {
            get => parameterMeg.Value; set
            {
                if (PreDataChange?.Invoke(parameterMeg.Value, value) == false)
                {
                    SetProperty(ref parameterMeg.Value, value);
                    ParameterManager.SavePara(this);
                    DataChanged?.Invoke(parameterMeg.Value);
                }
            }
        }
        public string Description { get => parameterMeg.Description; set => SetProperty(ref parameterMeg.Description, value); }
        public string Type { get => parameterMeg.Type; set => SetProperty(ref parameterMeg.Type, value); }
    }
}

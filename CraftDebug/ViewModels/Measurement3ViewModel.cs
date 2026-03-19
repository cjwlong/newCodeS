using CraftDebug.libs;
using DiastimeterManager.libs;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SharedResource.events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CraftDebug.ViewModels
{
    public class Measurement3ViewModel : BindableBase
    {
        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;
        private LGQuick lGQuick;
        public ObservableCollection<MeasurementPoint> Measurements { get; } = new();

        public DelegateCommand AddRowCommand { get; }
        public DelegateCommand<MeasurementButtonParam> FillPointCommand { get; }
        public DelegateCommand ClearContentsCommand { get; }
        public DelegateCommand<string> OutputCommand { get; }

        public Measurement3ViewModel(IContainerProvider provider)
        {
            containerProvider = provider;
            eventAggregator = containerProvider.Resolve<IEventAggregator>();
            lGQuick = containerProvider.Resolve<LGQuick>();

            AddRowCommand = new DelegateCommand(AddRow);
            FillPointCommand = new DelegateCommand<MeasurementButtonParam>(FillPoint);
            ClearContentsCommand = new DelegateCommand(ClearContentsTheMeasurements);
            OutputCommand = new DelegateCommand<string>((step) =>
            {
                if (!string.IsNullOrWhiteSpace(step))
                {
                    eventAggregator.GetEvent<MeasurementOutputEvent>().Publish(step);
                }
            });

            eventAggregator.GetEvent<MeasurementOutputAllEvent>().Subscribe((step) =>
            {
                Task.Run(() =>
                {
                    if (int.Equals(step.Item2, 3))
                    {
                        var sb = new StringBuilder();

                        sb.AppendLine("========================");
                        sb.AppendLine("调试步骤三");

                        sb.Append(",");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => $"第{m.Index}次")));

                        sb.Append("A4点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Point1?.ToString() ?? "")));

                        sb.Append("A5点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Point2?.ToString() ?? "")));

                        sb.Append("A6点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Point3?.ToString() ?? "")));

                        sb.Append("平均值,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Average?.ToString() ?? "")));
                        sb.AppendLine("========================");
                        File.AppendAllText(step.Item1, sb.ToString(), Encoding.UTF8);

                        eventAggregator.GetEvent<MeasurementOutputAllEvent>().Publish(new(step.Item1, 4));
                    }
                });
            }, ThreadOption.BackgroundThread);

            eventAggregator.GetEvent<MeasurementOutputSingleEvent>().Subscribe((step) =>
            {
                Task.Run(() =>
                {
                    if (int.Equals(step.Item2, 3))
                    {
                        var sb = new StringBuilder();

                        sb.AppendLine("========================");
                        sb.AppendLine("调试步骤三");

                        sb.Append(",");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => $"第{m.Index}次")));

                        sb.Append("A4点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Point1?.ToString() ?? "")));

                        sb.Append("A5点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Point2?.ToString() ?? "")));

                        sb.Append("A6点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Point3?.ToString() ?? "")));

                        sb.Append("平均值,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Average?.ToString() ?? "")));
                        sb.AppendLine("========================");
                        File.WriteAllText(step.Item1, sb.ToString(), Encoding.UTF8);

                        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            System.Windows.MessageBox.Show($"导出完毕\n\t地址：{step.Item1}", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                });
            }, ThreadOption.UIThread);

            AddRow();
            AddRow();
            AddRow();
        }

        private void ClearContentsTheMeasurements()
        {
            Measurements.Clear();
            AddRow();
            AddRow();
            AddRow();
        }

        private void AddRow()
        {
            Measurements.Add(new MeasurementPoint() { Index = Measurements.Count + 1 });
        }

        private void FillPoint(MeasurementButtonParam param)
        {
            if (param == null) return;

            double? value = lGQuick.LGQuickValue;
            if (!value.HasValue) return;

            switch (param.ColumnIndex)
            {
                case 1:
                    Measurements[param.RowIndex].Point1 = value;
                    break;
                case 2:
                    Measurements[param.RowIndex].Point2 = value;
                    break;
                case 3:
                    Measurements[param.RowIndex].Point3 = value;
                    break;
            }
        }
    }
}

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
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace CraftDebug.ViewModels
{
    public class Measurement1ViewModel : BindableBase
    {
        private IContainerProvider containerProvider;
        private IEventAggregator eventAggregator;
        private LGQuick lGQuick;
        public ObservableCollection<MeasurementDifPoint> Measurements { get; } = new();

        public DelegateCommand AddRowCommand { get; }
        public DelegateCommand<MeasurementButtonParam> FillPointCommand { get; }
        public DelegateCommand ClearContentsCommand { get; }
        public DelegateCommand<string> OutputCommand { get; }

        public Measurement1ViewModel(IContainerProvider provider)
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
                    if (int.Equals(step.Item2, 1))
                    {
                        var sb = new StringBuilder();

                        sb.AppendLine("========================");
                        sb.AppendLine("调试步骤一");

                        sb.Append(",");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => $"第{m.Index}次")));

                        sb.Append("A点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.PointA?.ToString() ?? "")));

                        sb.Append("B点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.PointB?.ToString() ?? "")));

                        sb.Append("差值,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Difference?.ToString() ?? "")));
                        sb.AppendLine("========================");
                        File.WriteAllText(step.Item1, sb.ToString(), Encoding.UTF8);

                        eventAggregator.GetEvent<MeasurementOutputAllEvent>().Publish(new(step.Item1, 2));
                    }
                });
            }, ThreadOption.BackgroundThread);

            eventAggregator.GetEvent<MeasurementOutputSingleEvent>().Subscribe((step) =>
            {
                Task.Run(() =>
                {
                    if (int.Equals(step.Item2, 1))
                    {
                        var sb = new StringBuilder();

                        sb.AppendLine("========================");
                        sb.AppendLine("调试步骤一");

                        sb.Append(",");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => $"第{m.Index}次")));

                        sb.Append("A点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.PointA?.ToString() ?? "")));

                        sb.Append("B点,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.PointB?.ToString() ?? "")));

                        sb.Append("差值,");
                        sb.AppendLine(string.Join(",", Measurements.Select(m => m.Difference?.ToString() ?? "")));
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
            Measurements.Add(new MeasurementDifPoint() { Index = Measurements.Count + 1 });
        }

        private void FillPoint(MeasurementButtonParam param)
        {
            if (param == null) return;

            double? value = lGQuick.LGQuickValue;
            if (!value.HasValue) return;

            switch (param.ColumnIndex)
            {
                case 1:
                    Measurements[param.RowIndex].PointA = value;
                    break;
                case 2:
                    Measurements[param.RowIndex].PointB = value;
                    break;
            }
        }
    }
}

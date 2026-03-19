using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Machine.Controls
{
    /// <summary>
    /// LineNumberedTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class LineNumberedTextBox : UserControl
    {


        public static readonly DependencyProperty TextProperty =
             DependencyProperty.Register(nameof(Text), typeof(string), typeof(LineNumberedTextBox),
                 new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty LineCountProperty =
            DependencyProperty.Register(nameof(LineCount), typeof(int), typeof(LineNumberedTextBox),
                new PropertyMetadata(0));

        public int LineCount
        {
            get => (int)GetValue(LineCountProperty);
            private set => SetValue(LineCountProperty, value);
        }

        public LineNumberedTextBox()
        {
            InitializeComponent();
            Loaded += LineNumberedTextBox_Loaded;
        }

        private void LineNumberedTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLineNumbers();
            // Auto focus the textbox when control is loaded
            if (InnerTextBox != null)
            {
                InnerTextBox.Focus();
            }
        }

        private void InnerTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLineNumbers();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LineNumberedTextBox control && control.InnerTextBox != null)
            {
                // Only update if text is different to avoid infinite loop
                if (control.InnerTextBox.Text != control.Text)
                {
                    control.InnerTextBox.Text = control.Text;
                }
                control.UpdateLineNumbers();
            }
        }

        private void InnerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the dependency property
            if (InnerTextBox.Text != Text)
            {
                Text = InnerTextBox.Text;
            }
            UpdateLineNumbers();
        }

        private void UpdateLineNumbers()
        {
            if (InnerTextBox == null) return;

            string text = InnerTextBox.Text ?? string.Empty;

            // Count lines properly handling both \r\n and \n
            int lineCount = 1;
            if (text.Length > 0)
            {
                lineCount = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
            }

            // Ensure at least one line is displayed
            if (lineCount == 0) lineCount = 1;

            LineCount = lineCount;

            // Update line number list
            var lineNumbers = new List<string>();
            for (int i = 1; i <= lineCount; i++)
            {
                lineNumbers.Add(i.ToString());
            }

            LineNumberItemsControl.ItemsSource = lineNumbers;

            // Sync scroll position after a small delay to ensure UI is updated
            Dispatcher.BeginInvoke(new Action(() => SyncScrollViewers()),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void TextScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            SyncScrollViewers();
        }

        private void SyncScrollViewers()
        {
            if (TextScrollViewer != null && LineNumberScrollViewer != null)
            {
                LineNumberScrollViewer.ScrollToVerticalOffset(TextScrollViewer.VerticalOffset);
            }
        }

        // Expose common TextBox properties and events
        public event TextChangedEventHandler TextChanged
        {
            add => InnerTextBox.TextChanged += value;
            remove => InnerTextBox.TextChanged -= value;
        }

        public event KeyEventHandler KeyDown
        {
            add => InnerTextBox.KeyDown += value;
            remove => InnerTextBox.KeyDown -= value;
        }

        public event KeyEventHandler KeyUp
        {
            add => InnerTextBox.KeyUp += value;
            remove => InnerTextBox.KeyUp -= value;
        }

        public void FocusTextBox()
        {
            InnerTextBox?.Focus();
        }

        public int CaretIndex
        {
            get => InnerTextBox.CaretIndex;
            set => InnerTextBox.CaretIndex = value;
        }

        public int SelectionStart
        {
            get => InnerTextBox.SelectionStart;
            set => InnerTextBox.SelectionStart = value;
        }

        public int SelectionLength
        {
            get => InnerTextBox.SelectionLength;
            set => InnerTextBox.SelectionLength = value;
        }

        public string SelectedText
        {
            get => InnerTextBox.SelectedText;
            set => InnerTextBox.SelectedText = value;
        }
    }
}
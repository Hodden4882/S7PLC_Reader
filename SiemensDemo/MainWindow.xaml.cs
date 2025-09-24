using MahApps.Metro.Controls;
using SiemensDemo.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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

namespace SiemensDemo
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        #region Fields
        private readonly MainViewModel mVM;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
        }

        internal MainWindow(MainViewModel vm)
        {
            mVM = vm;
            this.DataContext = vm;

            InitializeComponent();
        }

        private void LogListBox_Loaded(object sender, RoutedEventArgs e)
        {
            // 取得綁定的 ObservableCollection
            var logMessages = (ObservableCollection<string>)LogListBox.ItemsSource;
            if (logMessages != null)
            {
                // 為 CollectionChanged 事件註冊處理器
                logMessages.CollectionChanged += LogMessages_CollectionChanged;
            }
        }

        private void LogMessages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 當有新項目新增到集合時
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // 檢查 ListBox 中是否有項目
                if (LogListBox.Items.Count > 0)
                {
                    // 捲動到最後一個項目
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
            }
        }
    }
}

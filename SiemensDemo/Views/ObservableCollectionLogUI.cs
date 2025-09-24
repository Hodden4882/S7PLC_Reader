using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SiemensDemo.Views
{
    [Target("ObservableCollection")]
    public sealed class ObservableCollectionLogUI : TargetWithLayout
    {
        public static ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();

        protected override void Write(LogEventInfo logEvent)
        {
            // 確保在 UI 執行緒上執行，以避免跨執行緒錯誤
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 格式化日誌訊息並新增到列表中
                    LogMessages.Add(Layout.Render(logEvent));
                });
            }
        }
    }
}

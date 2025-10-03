using Newtonsoft.Json;
using NLog;
using S7.Net;
using SiemensDemo.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace SiemensDemo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly PlcService _plcService;
        private string _ipAddress;
        private PlcDataModel _plcData = new PlcDataModel();
        private bool _isConnected;
        private string _connectButtonContent = "連線";
        private int _dbNumber;
        private string _startByteAddress;
        private string _bitAddress;
        private string _selectedDataType;
        private string _writeData;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource _cts;
        private bool _isTestRunning;
        int count = 0;
        long times = 0;
        public ObservableCollection<string> DataTypes { get; } = new ObservableCollection<string>();
        #endregion

        #region Properties
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                ConnectButtonContent = value ? "斷線" : "連線";
                OnPropertyChanged();
                ((AsyncRelayCommand)ReadCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)WriteCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SendApiCommand).RaiseCanExecuteChanged();
            }
        }

        public string ConnectButtonContent
        {
            get => _connectButtonContent;
            set
            {
                _connectButtonContent = value;
                OnPropertyChanged();
            }
        }

        public int DbNumber
        {
            get => _dbNumber;
            set
            {
                _dbNumber = value;
                OnPropertyChanged();
            }
        }

        public string StartByteAddress
        {
            get => _startByteAddress;
            set
            {
                _startByteAddress = value;
                OnPropertyChanged();
            }
        }

        public string BitAddress
        {
            get => _bitAddress;
            set
            {
                _bitAddress = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDataType
        {
            get => _selectedDataType;
            set
            {
                _selectedDataType = value;
                OnPropertyChanged();
            }
        }

        public PlcDataModel PlcData
        {
            get => _plcData;
            set
            {
                _plcData = value;
                OnPropertyChanged();
            }
        }

        public string WriteData
        {
            get => _writeData;
            set
            {
                _writeData = value;
                OnPropertyChanged();
            }
        }

        public bool IsTestRunning
        {
            get => _isTestRunning;
            set
            {
                _isTestRunning = value;
                OnPropertyChanged();
                // 更新按鈕狀態 (如果有的話)
                (StartTestCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (StopTestCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region ICommand
        public ICommand ConnectCommand { get; }
        public ICommand ReadCommand { get; }
        public ICommand WriteCommand { get; }
        public ICommand SendApiCommand { get; }
        public ICommand StartTestCommand { get; private set; }
        public ICommand StopTestCommand { get; private set; }
        #endregion

        public MainViewModel(PlcService plcService)
        {
            _plcService = plcService;
            _plcService.ConnectionStatusChanged += OnPlcConnectionStatusChanged;

            // 初始化命令
            ConnectCommand = new AsyncRelayCommand(ConnectToPlc);
            ReadCommand = new AsyncRelayCommand(ReadFromPlc, () => _plcService.IsConnected);
            WriteCommand = new AsyncRelayCommand(WriteToPlc, () => _plcService.IsConnected);
            SendApiCommand = new AsyncRelayCommand(SendToWebApi, () => _plcService.IsConnected);
            StartTestCommand = new AsyncRelayCommand(RunContinuousTest,
        () => !_isTestRunning);

            // 只有在測試運行時才能停止
            StopTestCommand = new AsyncRelayCommand(StopContinuousTest,
                () => _isTestRunning);

            // 初始化 ComboBox 的資料
            DataTypes.Add("BOOL");
            DataTypes.Add("BYTE");
            DataTypes.Add("WORD");
            DataTypes.Add("DWORD");
            DataTypes.Add("INT");
            DataTypes.Add("DINT");
            DataTypes.Add("REAL");
            DataTypes.Add("STRING");

            _selectedDataType = DataTypes.FirstOrDefault();
        }

        private async Task<bool> ConnectToPlc()
        {
            // 如果已經連線，則執行斷線邏輯
            if (_plcService.IsConnected)
            {
                DisconnectFromPlc();
                return false;
            }

            if (IpAddress == null)
            {
                Logger.Warn("請輸入 IP 位址！");
                return false;
            }

            // 呼叫服務層的連線方法
            IsConnected = await _plcService.ConnectAsync(IpAddress);
            return IsConnected;
        }

        private void DisconnectFromPlc()
        {
            // 呼叫服務層的斷線方法
            _plcService.Disconnect();
            IsConnected = false;
        }

        private async Task ReadFromPlc()
        {
            Logger.Info($"介面讀取 DB:{DbNumber}, Adress:{StartByteAddress}, DataType:{SelectedDataType}");
            // 呼叫服務層的讀取方法
            object readResult = await _plcService.ReadDataAsync(DbNumber, StartByteAddress, SelectedDataType);

            switch (SelectedDataType.ToUpper())
            {
                case "BOOL":
                    _plcData.TestBool = (bool)readResult;
                    break;
                case "BYTE":
                    _plcData.TestByte = (byte)readResult;
                    break;
                case "WORD":
                    _plcData.TestWord = (ushort)readResult;
                    break;
                case "DWORD":
                    _plcData.TestDWord = (uint)readResult;
                    break;
                case "INT":
                    _plcData.TestInt = (short)readResult;
                    break;
                case "DINT":
                    _plcData.TestDint = (int)readResult;
                    break;
                case "REAL":
                    _plcData.TestReal = (float)readResult;
                    break;
                case "STRING":
                    _plcData.TestString = readResult as string;
                    break;
            }
        }

        private async Task WriteToPlc()
        {
            Logger.Info($"介面寫入 DB:{DbNumber}, Adress:{StartByteAddress}, DataType:{SelectedDataType}, Data:{WriteData}");
            // 呼叫服務層的寫入方法
            await _plcService.WriteDataAsync(DbNumber, StartByteAddress, SelectedDataType, WriteData);

            switch (SelectedDataType.ToUpper())
            {
                case "BOOL":
                    _plcData.TestBool = bool.Parse(WriteData);
                    break;
                case "BYTE":
                    _plcData.TestByte = byte.Parse(WriteData);
                    break;
                case "WORD":
                    _plcData.TestWord = ushort.Parse(WriteData);
                    break;
                case "DWORD":
                    _plcData.TestDWord = uint.Parse(WriteData);
                    break;
                case "INT":
                    _plcData.TestInt = short.Parse(WriteData);
                    break;
                case "DINT":
                    _plcData.TestDint = int.Parse(WriteData);
                    break;
                case "REAL":
                    _plcData.TestReal = float.Parse(WriteData);
                    break;
                case "STRING":
                    _plcData.TestString = WriteData;
                    break;
            }
        }

        private async Task SendToWebApi()
        {
            try
            {
                // 使用 HttpClient 發送 POST 請求到你的 Web API
                using (var client = new HttpClient())
                {
                    var apiUrl = "https://462c5fc3-a758-469b-a95a-045ab639edd3.mock.pstmn.io/api/plc/send";
                    var jsonContent = JsonConvert.SerializeObject(_plcData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    Logger.Info($"轉傳Web Api:{jsonContent}");
                    var response = await client.PostAsync(apiUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var apiReply = JsonConvert.DeserializeObject<PlcReply>(responseContent);
                        Logger.Info($"資料成功寫入！ Code : {apiReply.Code}, Desc : {apiReply.Desc}");
                    }
                    else
                    {
                        Logger.Error($"API 寫入失敗：狀態碼 {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"發送 API 請求失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 啟動 PLC 循環讀寫測試
        /// </summary>
        private async Task RunContinuousTest()
        {
            // 確保連線正常
            if (!_plcService.IsConnected)
            {
                Logger.Warn("PLC 未連線，無法啟動循環測試。");
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            IsTestRunning = true;
            Logger.Info("--- 開始執行 PLC 循環讀寫測試 ---");

            try
            {
                string writeValue = "10";
                int db = 1000;
                string dataType = "INT"; 
                string byteAdr = "2";
                Stopwatch stopwatch = new Stopwatch();

                while (!token.IsCancellationRequested)
                {
                    stopwatch.Start();
                    await _plcService.WriteDataAsync(db, byteAdr, dataType, writeValue);
                    count += 1;
                    // object readValue = await _plcService.ReadDataAsync(db, byteAdr, dataType);
                    stopwatch.Stop();
                    long elapsedMs = stopwatch.ElapsedMilliseconds;
                    times += elapsedMs;
                    Logger.Info($"測試：寫入值 {writeValue}, 次數 {count},耗時 {elapsedMs}");

                    await Task.Delay(50, token);
                }
                
            }
            catch (OperationCanceledException)
            {
                Logger.Info("--- PLC 循環讀寫壓力測試已終止 ---");
            }
            catch (Exception ex)
            {
                Logger.Error($"PLC 循環讀寫測試發生錯誤: {ex.Message}");
            }
            finally
            {
                float agvtime = times / count;
                Logger.Info($"平均時間 {agvtime}");
                IsTestRunning = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 停止 PLC 循環讀寫測試
        /// </summary>
        private Task StopContinuousTest()
        {
            Logger.Warn("收到終止 PLC 循環讀寫測試的請求...");
            _cts?.Cancel(); // 發出取消信號
            return Task.CompletedTask;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Event
        private void OnPlcConnectionStatusChanged(object sender, bool isConnected)
        {
            // 確保所有 UI 更新都在 WPF 的主執行緒上執行
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
            });
        } 
        #endregion

        // 實現 ICommand 
        public class AsyncRelayCommand : ICommand
        {
            private readonly Func<Task> _execute;
            private readonly Func<bool> _canExecute;
            private bool _isExecuting;
            public event EventHandler CanExecuteChanged;

            public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public bool CanExecute(object parameter)
            {
                return !_isExecuting && (_canExecute?.Invoke() ?? true);
            }
            public async void Execute(object parameter)
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                try
                {
                    await _execute();
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"執行操作失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _isExecuting = false;
                    RaiseCanExecuteChanged();
                }
            }
            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}


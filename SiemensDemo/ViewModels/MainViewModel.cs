using Newtonsoft.Json;
using NLog;
using S7.Net;
using SiemensDemo.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace SiemensDemo.ViewModels
{
    public class MainViewModel:INotifyPropertyChanged
    {
        #region Fields
        private readonly PlcService _plcService;
        private string _ipAddress;
        private PlcDataModel _plcData = new PlcDataModel();
        private bool _isConnected;
        private string _connectButtonContent = "連線";
        private string _dbNumber;
        private string _startByteAddress;
        private string _bitAddress;
        private object _lastReadValue;
        private string _selectedDataType;
        private string _writeData;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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

        public string DbNumber
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

        public object LastReadValue
        {
            get => _lastReadValue;
            set
            {
                _lastReadValue = value;
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
        #endregion

        #region ICommand
        public ICommand ConnectCommand { get; }
        public ICommand ReadCommand { get; }
        public ICommand WriteCommand { get; }
        public ICommand SendApiCommand { get; }
        #endregion

        public MainViewModel(PlcService plcService)
        {
            _plcService = plcService;

            // 初始化命令
            ConnectCommand = new AsyncRelayCommand(ConnectToPlc);
            ReadCommand = new AsyncRelayCommand(ReadFromPlc, () => _plcService.IsConnected);
            WriteCommand = new AsyncRelayCommand(WriteToPlc, () => _plcService.IsConnected);
            SendApiCommand = new AsyncRelayCommand(SendToWebApi, () => _plcService.IsConnected);

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
            bool success = await _plcService.ConnectAsync(IpAddress);
            IsConnected = success;
            if (success)
            {
                Logger.Info("連線成功！");
            }
            else
            {
                Logger.Warn("連線失敗，請檢查 IP 相關設定。");
            }
            return success;
        }

        private void DisconnectFromPlc()
        {
            // 呼叫服務層的斷線方法
            _plcService.Disconnect();
            IsConnected = false;
        }

        private async Task ReadFromPlc()
        {
            // 不再需要 IsConnected 檢查，因為服務層會處理
            if (!int.TryParse(DbNumber, out int db))
            {
                Logger.Error("錯誤：DB 編號或數量格式不正確！");
                return;
            }

            int byteAdr = 0;
            byte bitAdr = 0;

            if (StartByteAddress.Contains('.'))
            {
                var parts = StartByteAddress.Split('.');
                if (parts.Length != 2 || !int.TryParse(parts[0], out byteAdr) || !byte.TryParse(parts[1], out bitAdr))
                {
                    Logger.Error("錯誤：起始位址格式不正確。請使用 '位元組位址.位元位址' 的格式。");
                    return;
                }
                if (bitAdr > 7)
                {
                    Logger.Error("錯誤：位元位址必須介於 0 到 7 之間。");
                    return;
                }
            }
            else
            {
                if (!int.TryParse(StartByteAddress, out byteAdr))
                {
                    Logger.Error("錯誤：起始位址格式不正確！");
                    return;
                }
            }

            try
            {
                // 呼叫服務層的讀取方法
                object readResult = await _plcService.ReadDataAsync(db, byteAdr, SelectedDataType, bitAdr);

                // 根據類型將結果賦值給 ViewModel 的屬性
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

                LastReadValue = readResult;
                Logger.Info($"讀取成功！讀取值：{readResult}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"讀取失敗：{ex.Message}");
            }
        }

        private async Task WriteToPlc()
        {
            if (WriteData == null || !int.TryParse(DbNumber, out int db) || !int.TryParse(StartByteAddress, out int byteAdr))
            {
                Logger.Warn("寫入失敗：請輸入所有必要的資訊。");
                return;
            }

            try
            {
                object dataToWrite = null;
                switch (SelectedDataType.ToUpper())
                {
                    case "BOOL":
                        if (!bool.TryParse(WriteData, out bool boolVal))
                        {
                            Logger.Warn("寫入失敗：布林值格式不正確。請輸入 'true' 或 'false'。");
                            return;
                        }
                        dataToWrite = boolVal;
                        break;
                    case "BYTE":
                        if (!byte.TryParse(WriteData, out byte byteVal))
                        {
                            Logger.Warn("寫入失敗：位元組格式不正確。");
                            return;
                        }
                        dataToWrite = byteVal;
                        break;
                    case "WORD":
                        if (!ushort.TryParse(WriteData, out ushort wordVal))
                        {
                            Logger.Warn("寫入失敗：字組格式不正確。");
                            return;
                        }
                        dataToWrite = wordVal;
                        break;
                    case "DWORD":
                        if (!uint.TryParse(WriteData, out uint dwordVal))
                        {
                            Logger.Warn("寫入失敗：雙字組格式不正確。");
                            return;
                        }
                        dataToWrite = dwordVal;
                        break;
                    case "INT":
                        if (!short.TryParse(WriteData, out short intVal))
                        {
                            Logger.Warn("寫入失敗：整數格式不正確。");
                            return;
                        }
                        dataToWrite = intVal;
                        break;
                    case "DINT":
                        if (!int.TryParse(WriteData, out int dintVal))
                        {
                            Logger.Warn("寫入失敗：雙整數格式不正確。");
                            return;
                        }
                        dataToWrite = dintVal;
                        break;
                    case "REAL":
                        if (!float.TryParse(WriteData, out float realVal))
                        {
                            Logger.Warn("寫入失敗：浮點數格式不正確。");
                            return;
                        }
                        dataToWrite = realVal;
                        break;
                    case "STRING":
                        dataToWrite = WriteData;
                        break;
                    default:
                        Logger.Warn($"寫入失敗：不支援的資料型別 '{SelectedDataType}'。");
                        return;
                }

                // 呼叫服務層的寫入方法
                await _plcService.WriteDataAsync(db, byteAdr, SelectedDataType, dataToWrite);

                Logger.Info($"寫入成功！寫入值：{WriteData}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"寫入失敗：{ex.Message}");
            }
        }

        private async Task SendToWebApi()
        {
            try
            {
                // 使用 HttpClient 發送 POST 請求到你的 Web API
                using (var client = new HttpClient())
                {
                    var apiUrl = $"http://localhost:5000/";
                    var jsonContent = JsonConvert.SerializeObject(_plcData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Info("資料成功透過 API 寫入！");
                    }
                    else
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Logger.Error($"API 寫入失敗：狀態碼 {response.StatusCode}，回應：{responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"發送 API 請求失敗：{ex.Message}");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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


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
        private Plc _plc;
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

        public MainViewModel()
        {
            // 初始化命令
            ConnectCommand = new AsyncRelayCommand(ConnectToPlc);
            ReadCommand = new AsyncRelayCommand(ReadFromPlc, () => IsConnected);
            WriteCommand = new AsyncRelayCommand(WriteToPlc, () => IsConnected);
            SendApiCommand = new AsyncRelayCommand(SendToWebApi, () => IsConnected);

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

        private async Task ConnectToPlc()
        {
            // 如果已經連線，則執行斷線邏輯
            if (IsConnected)
            {
                DisconnectFromPlc();
                return;
            }

            if(IpAddress == null)
            {
                Logger.Warn("請輸入 IP 位址！");
                return;
            }
            _plc = new Plc(CpuType.S71200, IpAddress, 0, 1);
            try
            {
                await Task.Run(() => _plc.Open());
                IsConnected = _plc.IsConnected;
                if (IsConnected)
                {
                    Logger.Info("連線成功！");
                }
                else
                {
                    Logger.Warn("連線失敗，請檢查 IP 相關設定。");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"連線失敗：{ex.Message}");
                IsConnected = false;
            }
        }

        private void DisconnectFromPlc()
        {
            if (_plc?.IsConnected == true)
            {
                _plc.Close();
                IsConnected = false;
                Logger.Info("已斷開連線。");
            }
        }

        private async Task ReadFromPlc()
        {
            if (!IsConnected) return;

            if (!int.TryParse(DbNumber, out int db))
            {
                Logger.Error("錯誤：DB 編號或數量格式不正確！");
                return;
            }

            int byteAdr = 0;
            byte bitAdr = 0;

            // 檢查起始位址字串是否包含 '.'
            if (StartByteAddress.Contains('.'))
            {
                // 如果包含，則解析字串，格式為 "byte.bit"
                var parts = StartByteAddress.Split('.');
                if (parts.Length != 2 || !int.TryParse(parts[0], out byteAdr) || !byte.TryParse(parts[1], out bitAdr))
                {
                    Logger.Error("錯誤：起始位址格式不正確。請使用 '位元組位址.位元位址' 的格式。");
                    return;
                }

                // 檢查位元位址是否在 0-7 範圍內
                if (bitAdr > 7)
                {
                    Logger.Error("錯誤：位元位址必須介於 0 到 7 之間。");
                    return;
                }
            }
            else
            {
                // 如果不包含，則解析為一般的位元組位址
                if (!int.TryParse(StartByteAddress, out byteAdr))
                {
                    Logger.Error("錯誤：起始位址格式不正確！");
                    return;
                }
            }

            try
            {
                object readResult = null;
                switch (SelectedDataType)
                {
                    case "BOOL":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Bit, 1, bitAdr));
                        _plcData.TestBool = (bool)readResult;
                        break;

                    case "BYTE":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Byte, 1));
                        _plcData.TestByte = (byte)readResult;
                        break;

                    case "WORD":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Word, 1));
                        _plcData.TestWord = (ushort)readResult;
                        break;

                    case "DWORD":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.DWord, 1));
                        _plcData.TestDWord = (uint)readResult;
                        break;

                    case "INT":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Int, 1));
                        _plcData.TestInt = (short)readResult;
                        break;

                    case "DINT":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.DInt, 1));
                        _plcData.TestDint = (int)readResult;
                        break;

                    case "REAL":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Real, 1));
                        _plcData.TestReal = (float)readResult;
                        break;

                    case "STRING":
                        readResult = await Task.Run(() =>
                            _plc.Read(DataType.DataBlock, db, byteAdr, VarType.S7String, 254));
                        string rawString = readResult as string;

                        if (rawString != null)
                        {
                            // 使用 TrimEnd() 方法來移除字串結尾的 Null 字元
                            _plcData.TestString = rawString.TrimEnd('\u0000');
                        }
                        break;
                    default:
                        Logger.Error("錯誤：請選擇資料型別！");
                        return;
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
            if (!IsConnected) return;

            if (!int.TryParse(DbNumber, out int db))
            {
                Logger.Error("錯誤：DB 編號不正確！");
                return;
            }

            int byteAdr = 0;
            byte bitAdr = 0;

            // 檢查起始位址字串是否包含 '.'
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

            Logger.Info($"正在嘗試寫入 PLC 資料 ({SelectedDataType})...");
            try
            {
                switch (SelectedDataType)
                {
                    case "BOOL":
                        if (!bool.TryParse(WriteData, out bool boolValue))
                        {
                            Logger.Error("錯誤：BOOL 型別寫入值只能是 'true' 或 'false'！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, boolValue, bitAdr));
                        break;
                    case "BYTE":
                        if (!byte.TryParse(WriteData, out byte byteValue))
                        {
                            Logger.Error("錯誤：BYTE 型別寫入值必須是數字！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, byteValue));
                        break;
                    case "WORD":
                        if (!ushort.TryParse(WriteData, out ushort wordValue))
                        {
                            Logger.Error("錯誤：WORD 型別寫入值必須是數字！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, wordValue));
                        break;
                    case "INT":
                        if (!short.TryParse(WriteData, out short intValue))
                        {
                            Logger.Error("錯誤：INT 型別寫入值必須是數字！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, intValue));
                        break;
                    case "DINT":
                        if (!int.TryParse(WriteData, out int dintValue))
                        {
                            Logger.Error("錯誤：DINT 型別寫入值必須是數字！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, dintValue));
                        break;
                    case "DWORD":
                        if (!uint.TryParse(WriteData, out uint dwordValue))
                        {
                            Logger.Error("錯誤：DWORD 型別寫入值必須是數字！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, dwordValue));
                        break;
                    case "REAL":
                        if (!float.TryParse(WriteData, out float realValue))
                        {
                            Logger.Error("錯誤：REAL 型別寫入值必須是數字！");
                            return;
                        }
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, realValue));
                        break;
                    case "STRING":
                        // 假設 PLC STRING 類型最大長度是 254
                        int maxLength = 254;

                        // 獲取要寫入的實際字串的位元組
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(WriteData);

                        // 確保寫入的字串長度不超過最大長度
                        if (asciiBytes.Length > maxLength)
                        {
                            Logger.Error("錯誤：寫入字串長度超過 PLC 定義的最大長度！");
                            return;
                        }

                        // 建立一個完整的位元組陣列，包含長度資訊和字串內容
                        // 長度 = 最大長度 (1 位元組) + 實際長度 (1 位元組) + 內容 (N 個位元組)
                        byte[] stringBuffer = new byte[maxLength + 2];

                        // 將最大長度和實際長度寫入緩衝區的前兩個位元組
                        stringBuffer[0] = (byte)maxLength;
                        stringBuffer[1] = (byte)asciiBytes.Length;

                        // 將實際字串內容複製到緩衝區
                        Array.Copy(asciiBytes, 0, stringBuffer, 2, asciiBytes.Length);

                        // 將整個包含長度資訊的位元組陣列寫入 PLC
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, stringBuffer));

                        Logger.Info($"寫入 STRING 成功，值為: {WriteData}");

                        break;
                    default:
                        Logger.Error("錯誤：請選擇資料型別！");
                        return;
                }

                Logger.Info($"寫入成功！寫入值：{WriteData}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"寫入失敗：{ex.Message}");
            }
        }

        // Web API 傳輸方法
        private async Task SendToWebApi()
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(_plcData);
                Logger.Info($"Web API 傳送資料：{jsonData}");

                using (var client = new HttpClient())
                {
                    var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://your_api_url/api/data", content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResult = JsonConvert.DeserializeObject<ApiObjects>(responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        if (apiResult != null)
                        {
                            Logger.Info($"API 回應 - 狀態碼: {apiResult.Code}, 描述: {apiResult.Desc}");
                        }
                    }
                    else
                    {
                        Logger.Warn($"API 傳送失敗 - 狀態碼：{response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Web API 傳送失敗：{ex.Message}");
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


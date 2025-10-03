using NLog;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace SiemensDemo.ViewModels
{
    public class PlcService
    {
        #region Fields
        private string _ipAddress;
        private Plc _plc;
        public event EventHandler<bool> ConnectionStatusChanged;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool IsConnected => _plc != null && _plc.IsConnected; 
        #endregion

        public PlcService()
        {
            //_ipAddress = ipAddress;
            //Task.Run(() => ConnectAsync(ipAddress));
        }

        public async Task<bool> ConnectAsync(string ipAddress, CpuType cpuType = CpuType.S71200, short rack = 0, short slot = 1)
        {
            if (_plc?.IsConnected == true)
            {
                Logger.Info("PLC 已連線，無需重複連線。");
                return true;
            }

            try
            {
                // 建立新的 Plc 實例並嘗試連線
                _plc = new Plc(cpuType, ipAddress, rack, slot);
                await Task.Run(() => _plc.Open());

                if (_plc.IsConnected)
                {
                    Logger.Info("PlcService 連線成功！");
                    OnConnectionStatusChanged(true);
                    return true;
                }
                else
                {
                    Logger.Error("連線失敗，請檢查 IP 相關設定。");
                    OnConnectionStatusChanged(false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"連線失敗：{ex.Message}");
                _plc = null;
                OnConnectionStatusChanged(false);
                return false;
            }
        }

        public void Disconnect()
        {
            if (_plc?.IsConnected == true)
            {
                _plc.Close();
                _plc = null;
                Logger.Info("PlcService 已斷開連線。");
            }
        }

        public async Task<object> ReadDataAsync(int DbNumber, string StartByteAddress, string dataType)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("未連線到 PLC。");
            }
            if (StartByteAddress == null || DbNumber == 0 || dataType == null)
            {
                throw new InvalidOperationException("讀取失敗：請輸入所有必要的資訊。");
            }
            int byteAdr = 0;
            byte bitAdr = 0;

            if (StartByteAddress.Contains('.'))
            {
                var parts = StartByteAddress.Split('.');
                if (parts.Length != 2 || !int.TryParse(parts[0], out byteAdr) || !byte.TryParse(parts[1], out bitAdr))
                {
                    throw new ArgumentException("起始位址格式不正確。請使用 '位元組位址.位元位址' 的格式。");
                }
                if (bitAdr > 7)
                {
                    throw new ArgumentException("位元位址必須介於 0 到 7 之間。");
                }
            }
            else
            {
                if (!int.TryParse(StartByteAddress, out byteAdr))
                {
                    throw new ArgumentException("起始位址格式不正確。");
                }
            }

            try
            {
                object readResult = null;
                switch (dataType.ToUpper())
                {
                    case "BOOL":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.Bit, 1, bitAdr));
                        break;
                    case "BYTE":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.Byte, 1));
                        break;
                    case "WORD":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.Word, 1));
                        break;
                    case "DWORD":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.DWord, 1));
                        break;
                    case "INT":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.Int, 1));
                        break;
                    case "DINT":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.DInt, 1));
                        break;
                    case "REAL":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.Real, 1));
                        break;
                    case "STRING":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, DbNumber, byteAdr, VarType.String, 254));
                        if (readResult is string rawString)
                        {
                            readResult = rawString.TrimEnd('\u0000');
                        }
                        else
                        {
                            throw new InvalidOperationException("讀取 String 時回傳了非String型別。");
                        }
                        break;
                    default:
                        throw new ArgumentException("不支援的資料型別。");
                }
                Logger.Info($"PLC 讀取成功！讀取值：{readResult}");
                return readResult;
            }
            catch (Exception ex)
            {
                Logger.Error($"PLC 讀取失敗：{ex.Message}");
                throw;
            }
        }

        public async Task WriteDataAsync(int DbNumber, string StartByteAddress, string dataType, string data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("未連線到 PLC。");
            }
            if (data == null || DbNumber == 0 || StartByteAddress == null || dataType == null)
            {
                throw new InvalidOperationException("寫入失敗：請輸入所有必要的資訊。");
            }
            int byteAdr = 0;
            int bitAdr = 0;

            if (StartByteAddress.Contains('.'))
            {
                var parts = StartByteAddress.Split('.');
                if (parts.Length != 2 || !int.TryParse(parts[0], out byteAdr) || !int.TryParse(parts[1], out bitAdr))
                {
                    throw new ArgumentException("起始位址格式不正確。請使用 '位元組位址.位元位址' 的格式。");
                }
                if (bitAdr > 7)
                {
                    throw new ArgumentException("位元位址必須介於 0 到 7 之間。");
                }
            }
            else
            {
                if (!int.TryParse(StartByteAddress, out byteAdr))
                {
                    throw new ArgumentException("起始位址格式不正確。");
                }
            }

            try
            {
                switch (dataType.ToUpper())
                {
                    case "BOOL":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, bool.Parse(data), bitAdr));
                        break;
                    case "BYTE":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, byte.Parse(data)));
                        break;
                    case "WORD":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, ushort.Parse(data)));
                        break;
                    case "INT":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, short.Parse(data)));
                        break;
                    case "DINT":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, int.Parse(data)));
                        break;
                    case "DWORD":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, uint.Parse(data)));
                        break;
                    case "REAL":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, float.Parse(data)));
                        break;
                    case "STRING":
                        if (string.IsNullOrEmpty(data))
                        {
                            data = "";
                        }

                        // 陣列開頭包含 Max Length (1 byte) 和 Actual Length (1 byte)
                        byte[] stringBytes = S7.Net.Types.String.ToByteArray(data, data.Length);

                        byte[] bytesToWrite = new byte[256];

                        // 將 S7.Net 生成的 stringBytes 複製到新陣列的開頭
                        Buffer.BlockCopy(stringBytes, 0, bytesToWrite, 0, stringBytes.Length);

                        await Task.Run(() => _plc.Write(DataType.DataBlock, DbNumber, byteAdr, bytesToWrite));
                        break;
                    default:
                        throw new ArgumentException("不支援的資料型別。");
                }
                Logger.Info($"PLC 寫入成功！寫入值：{data}");
            }
            catch (Exception ex)
            {
                Logger.Error($"PLC 寫入失敗：{ex.Message}");
                throw;
            }
        }

        protected void OnConnectionStatusChanged(bool isConnected)
        {
            // 如果事件有訂閱者，則觸發事件
            ConnectionStatusChanged?.Invoke(this, isConnected);
        }
    }
}


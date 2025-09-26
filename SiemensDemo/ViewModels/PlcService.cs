using NLog;
using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiemensDemo.ViewModels
{
    public class PlcService
    {
        #region Fields
        private Plc _plc;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool IsConnected => _plc != null && _plc.IsConnected; 
        #endregion

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
                    return true;
                }
                else
                {
                    Logger.Warn("PlcService 連線失敗，請檢查 IP 相關設定。");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"PlcService 連線失敗：{ex.Message}");
                _plc = null;
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

        // 以下是從你的 ViewModel 程式碼中重構出來的讀取方法
        public async Task<object> ReadDataAsync(int db, int byteAdr, string dataType, byte bitAdr = 0)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("未連線到 PLC。");
            }

            try
            {
                object readResult = null;
                switch (dataType.ToUpper())
                {
                    case "BOOL":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Bit, 1, bitAdr));
                        break;
                    // ... 在這裡填入你的所有讀寫 case
                    case "BYTE":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Byte, 1));
                        break;
                    case "WORD":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Word, 1));
                        break;
                    case "DWORD":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.DWord, 1));
                        break;
                    case "INT":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Int, 1));
                        break;
                    case "DINT":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.DInt, 1));
                        break;
                    case "REAL":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.Real, 1));
                        break;
                    case "STRING":
                        readResult = await Task.Run(() => _plc.Read(DataType.DataBlock, db, byteAdr, VarType.String, 254));
                        string rawString = readResult as string;
                        if (rawString != null)
                        {
                            readResult = rawString.TrimEnd('\u0000');
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

        public async Task WriteDataAsync(int db, int byteAdr, string dataType, object data, byte bitAdr = 0)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("未連線到 PLC。");
            }

            try
            {
                switch (dataType.ToUpper())
                {
                    case "BOOL":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (bool)data, bitAdr));
                        break;
                    case "BYTE":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (byte)data));
                        break;
                    case "WORD":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (ushort)data));
                        break;
                    case "INT":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (short)data));
                        break;
                    case "DINT":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (int)data));
                        break;
                    case "DWORD":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (uint)data));
                        break;
                    case "REAL":
                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, (float)data));
                        break;
                    case "STRING":
                        string stringValue = data as string;
                        if (string.IsNullOrEmpty(stringValue))
                        {
                            stringValue = "";
                        }

                        // 1. 轉換為 S7.Net 要求的 byte 陣列格式
                        // 陣列開頭包含 Max Length (1 byte) 和 Actual Length (1 byte)
                        byte[] stringBytes = S7.Net.Types.String.ToByteArray(stringValue, stringValue.Length);

                        byte[] bytesToWrite = new byte[256];

                        // 將 S7.Net 生成的 stringBytes 複製到新陣列的開頭
                        Buffer.BlockCopy(stringBytes, 0, bytesToWrite, 0, stringBytes.Length);

                        await Task.Run(() => _plc.Write(DataType.DataBlock, db, byteAdr, bytesToWrite));
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
    }
}


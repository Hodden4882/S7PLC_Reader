using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiemensDemo.Models
{
    public class Config
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [JsonProperty("PlcIpAddress")]
        public string PlcIpAddress { get; set; }

        [JsonProperty("ApiBaseUrl")]
        public string ApiBaseUrl { get; set; }

        /// <summary>
        /// 取得預設的 Config 實例。
        /// </summary>
        private static Config GetDefaultConfig()
        {
            // 直接回傳一個帶有預設值的實例
            return new Config
            {
                PlcIpAddress = "192.168.0.10",
                ApiBaseUrl = "http://localhost:5000/",
            };
        }

        /// <summary>
        /// 載入配置檔，如果不存在則生成預設檔案。
        /// </summary>
        /// <returns>Config 實例</returns>
        public static Config LoadConfiguration()
        {
            // 決定檔案路徑
            string configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            string configPath = Path.Combine(configDir, "config.json");

            if (!File.Exists(configPath))
            {
                // 1. 檔案不存在：建立預設配置
                Config defaultConfig = GetDefaultConfig();

                try
                {
                    // 確保 config 目錄存在
                    if (!Directory.Exists(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }

                    // 序列化並寫入檔案 (使用 WriteAllText 避免手動處理 StreamWriter)
                    string jsonContent = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                    File.WriteAllText(configPath, jsonContent, Encoding.UTF8);

                    Logger.Warn($"找不到設定檔，已自動生成預設檔案於: {configPath}", configPath);
                    return defaultConfig;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"無法創建預設設定檔。錯誤: {ex.Message}", ex);
                }
            }

            // 2. 檔案存在：正常讀取
            try
            {
                string jsonContent = File.ReadAllText(configPath, Encoding.UTF8);
                Logger.Info("成功載入設定檔");
                return JsonConvert.DeserializeObject<Config>(jsonContent);
            }
            catch (Exception ex)
            {
                // 如果檔案損壞無法解析，則拋出錯誤
                throw new InvalidOperationException($"無法解析設定檔。請檢查 config.json 格式是否正確。錯誤: {ex.Message}");
            }
        }
    }
}


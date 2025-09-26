using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiemensDemo.Models
{
    public class PlcReply
    {
        /// <summary>
		/// 取得或設定執行結果
		/// <list type="bullet">
		/// <item>(0) 成功</item>
		/// <item>(!=0) 失敗，請在 <see cref="Desc"/> 描述錯誤</item>
		/// </list>
		/// </summary>
		[JsonProperty("code")]
        public int Code { get; set; }
        /// <summary>取得或設定錯誤描述</summary>
        [JsonProperty("description")]
        public string Desc { get; set; }
    }

    public class ReadReply<T> : PlcReply
    {
        /// <summary>取得或設定回傳的資料內容</summary>
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    /// <summary>
    /// PLC 讀取請求的資料傳輸物件
    /// </summary>
    public class ReadRequest
    {
        [JsonProperty("db")]
        public int Db { get; set; }

        // 注意：為了處理位元位址 (如 0.0)，我們讓它保持字串
        [JsonProperty("byteAdr")]
        public int ByteAdr { get; set; }

        [JsonProperty("dataType")]
        public string DataType { get; set; }
    }

    public class WriteRequest
    {
        [JsonProperty("db")]
        public int Db { get; set; }

        // 注意：為了處理位元位址 (如 0.0)，我們讓它保持字串
        [JsonProperty("byteAdr")]
        public int ByteAdr { get; set; }

        [JsonProperty("dataType")]
        public string DataType { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}

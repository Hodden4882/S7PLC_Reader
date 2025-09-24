using SiemensDemo.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SiemensDemo.Controllers
{
    [RoutePrefix("api/plc")]
    public class PlcController : ApiController
    {
        private readonly PlcService _plcService;

        // 建構子注入，將 PlcService 實例傳入
        public PlcController(PlcService plcService)
        {
            _plcService = plcService;
        }

        // Web API 端點：讀取資料
        [HttpGet]
        [Route("read")]
        public async Task<IHttpActionResult> ReadData(int db, int byteAdr, string dataType)
        {
            try
            {
                object result = await _plcService.ReadDataAsync(db, byteAdr, dataType);
                return Ok(new { success = true, value = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // Web API 端點：寫入資料
        [HttpPost]
        [Route("write")]
        public async Task<IHttpActionResult> WriteData(int db, int byteAdr, string dataType, [FromBody] object data)
        {
            try
            {
                await _plcService.WriteDataAsync(db, byteAdr, dataType, data);
                return Ok(new { success = true, message = "寫入成功" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}


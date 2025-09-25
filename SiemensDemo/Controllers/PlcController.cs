using SiemensDemo.Models;
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

        public PlcController(PlcService plcService)
        {
            _plcService = plcService;
        }

        [HttpPost]
        [Route("read")]
        public async Task<IHttpActionResult> ReadData(int db, int byteAdr, string dataType)
        {
            try
            {
                object result = await _plcService.ReadDataAsync(db, byteAdr, dataType);
                var response = new ReadReply<object>
                {
                    Code = 0,
                    Desc = "讀取成功",
                    Data = result,
                };

                return Ok(response);
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

        [HttpPost]
        [Route("write")]
        public async Task<IHttpActionResult> WriteData(int db, int byteAdr, string dataType, [FromBody] object data)
        {
            try
            {
                await _plcService.WriteDataAsync(db, byteAdr, dataType, data);
                var response = new PlcReply
                {
                    Code = 0,
                    Desc = "寫入成功",
                };
                return Ok(response);
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


using NLog;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public PlcController(PlcService plcService)
        {
            _plcService = plcService;
        }

        [HttpPost]
        [Route("read")]
        public async Task<IHttpActionResult> ReadData(ReadRequest request)
        {
            Logger.Info($"收到讀取請求: DB{request.Db}, Address: {request.ByteAdr}, Type: {request.DataType}");
            try
            {
                object result = await _plcService.ReadDataAsync(request.Db, request.ByteAdr, request.DataType);
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
                Logger.Error(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("write")]
        public async Task<IHttpActionResult> WriteData(WriteRequest request)
        {
            Logger.Info($"收到寫入請求: DB{request.Db}, Address: {request.ByteAdr}, Type: {request.DataType}, Data: {request.Data}");
            try
            {
                await _plcService.WriteDataAsync(request.Db, request.ByteAdr, request.DataType, request.Data);
                var response = new PlcReply
                {
                    Code = 0,
                    Desc = "寫入成功",
                };
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return InternalServerError(ex);
            }
        }
    }
}


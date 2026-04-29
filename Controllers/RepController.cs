using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace KhanLogistics.Controllers
{
    public class RepController : Controller
    {
        private readonly TransportMgmtContext _context;
        public RepController(TransportMgmtContext _ctx)
        {
            this._context = _ctx;
        }

        public IEnumerable<BillTable> ExportAllBills()
        {
            return _context.BillTables.Include(b => b.tblDispatches).ToList();
        }


        [HttpGet]
        public IActionResult BillDetailExport()
        {
            var model = new List<BillDispatchViewModel>(); // Initially an empty list
            return View(model);
        }


        
        [HttpPost]
        public JsonResult LoadBillsToExport(DateTime fromDate, DateTime toDate, int? factoryId)
        {
            try
            {
                var pFrom = new Microsoft.Data.SqlClient.SqlParameter("@FromDate", fromDate);
                var pTo = new Microsoft.Data.SqlClient.SqlParameter("@ToDate", toDate);
                var pFact = new Microsoft.Data.SqlClient.SqlParameter("@FactoryId", (object)factoryId ?? DBNull.Value);

                var blst = _context.SpExpoBilldetail
                    .FromSqlRaw("EXEC [dbo].[SpExpoBilldetail] @FromDate, @ToDate, @FactoryId", pFrom, pTo, pFact)
                    .ToList();

                return Json(blst);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        public JsonResult GetFactories()
        {
            var factories = _context.TblFactories.Select(f => new { id = f.FID, name = f.FactoryName }).ToList();
            return Json(factories);
        }


        [HttpGet]
        public IActionResult PaymentDetails()
        {
            var model = new List<SpCheckPaymentDetails>(); // Initially an empty list
            return View(model);
        }




        [HttpPost]
        public JsonResult LoadPaymentDetails(DateTime startDate, DateTime endDate, int? factoryId)
        {
            var paymentDetails = _context.SpCheckPaymentDetails
                .FromSqlRaw("EXEC [dbo].[SpCheckPaymentDetails] @StartDate = {0}, @EndDate = {1}, @FactoryId = {2}", startDate, endDate, factoryId)
                .ToList();

            return Json(paymentDetails);
        }

        [HttpGet]
        public IActionResult SpPaymentByDate()
        {
            var model = new List<SpPaymentByDate>(); // Initially an empty list
            return View(model);
        }




        [HttpPost]
        public JsonResult LoadPaymentDetailsByDate(DateTime startDate, DateTime endDate, int? factoryId)
        {
            var paymentByDate = _context.SpPaymentByDate
                .FromSqlRaw("EXEC [dbo].[SpPaymentByDate] @StartDate = {0}, @EndDate = {1}, @FactoryId = {2}", startDate, endDate, factoryId)
                .ToList();

            return Json(paymentByDate);
        }


        [HttpGet]
        public IActionResult OutstandingDetails()
        {
            var model = new List<SpOutStanding>(); // Initially an empty list
            return View(model);
        }



        [HttpPost]
        public JsonResult LoadOutstandingDetails(DateTime fromDate, DateTime toDate, int? factoryId)
        {
            try
            {
                var pFrom = new Microsoft.Data.SqlClient.SqlParameter("@FromDate", fromDate);
                var pTo = new Microsoft.Data.SqlClient.SqlParameter("@ToDate", toDate);
                var pFact = new Microsoft.Data.SqlClient.SqlParameter("@FactoryId", (object)factoryId ?? DBNull.Value);

                var outstandingDetails = _context.SpOutStanding
                    .FromSqlRaw("EXEC [dbo].[SpOutStanding] @FromDate, @ToDate, @FactoryId", pFrom, pTo, pFact)
                    .ToList();

                return Json(outstandingDetails);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

    }
}


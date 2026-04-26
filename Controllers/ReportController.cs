using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;



namespace KhanLogistics.Controllers
{

    public class ReportController : Controller
    {
        private readonly TransportMgmtContext _context;
        public ReportController(TransportMgmtContext _ctx)
        {
            this._context = _ctx;
        }




        public IActionResult Index(int id)
        {
            // var factory = _context.TblFactories.FirstOrDefault(f=>f.FID==id).FactoryName;
            var result = _context.Sp_QtyByMonth.FromSqlRaw($"EXECUTE [dbo].[Sp_QtyByMonth] ");
            return View(result);
        }






        public IActionResult CheckBills(DateTime startDate, DateTime endDate, int? factoryId, bool exportExcel = false)
        {
            // Set the license context before using EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var factories = _context.TblFactories.ToList();
            ViewBag.Factories = factories;

            // Fetch the data
            var result = _context.SpCheckBill
                .FromSqlRaw("EXECUTE [dbo].[SpCheckBill] @StartDate = {0}, @EndDate = {1}, @FactoryId = {2}", startDate, endDate, factoryId)
                .ToList();

            // Set ViewData for the filters
            ViewData["StartDate"] = startDate.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.ToString("yyyy-MM-dd");
            ViewData["FactoryId"] = factoryId ?? 0;

            // If the exportExcel flag is set, return the Excel file
            if (exportExcel)
            {
                // Export logic (already implemented in your code)
                return ExportToExcel(result, startDate, endDate, factoryId);
            }

            // Return the normal view with the data
            return View(result);
        }


        private IActionResult ExportToExcel(IEnumerable<KhanLogistics.Models.ViewModel.SpCheckBill> result, DateTime startDate, DateTime endDate, int? factoryId)
        {
            // Set the license context before using EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Debugging: Check if result contains data before attempting export
            Console.WriteLine($"Export data count: {result.Count()}");

            // Check if result contains data
            if (result == null || !result.Any())
            {
                Console.WriteLine("No data available for export.");
                return Content("No data available for export.");
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Bills");

                // Add header row
                worksheet.Cells[1, 1].Value = "Factory Name";
                worksheet.Cells[1, 2].Value = "Challan No";
                worksheet.Cells[1, 3].Value = "Destination";
                worksheet.Cells[1, 4].Value = "Vehicle No";
                worksheet.Cells[1, 5].Value = "Dispatch Date";
                worksheet.Cells[1, 6].Value = "Dispatch Quantity";
                worksheet.Cells[1, 7].Value = "Bill Num";

                // Add data rows
                int row = 2;
                foreach (var item in result)
                {
                    // Log values to ensure we're processing the data correctly
                    Console.WriteLine($"FactoryName: {item.FactoryName}, ChallanNo: {item.ChallanNo}, Destination: {item.Destination}");

                    // Add data to cells, using fallback values for potentially null fields
                    worksheet.Cells[row, 1].Value = item.FactoryName ?? "N/A"; // Fallback if null
                    worksheet.Cells[row, 2].Value = item.ChallanNo ?? "N/A";  // Fallback if null
                    worksheet.Cells[row, 3].Value = item.Destination ?? "N/A"; // Fallback if null
                    worksheet.Cells[row, 4].Value = item.VehicleNo ?? "N/A"; // Fallback if null
                    worksheet.Cells[row, 5].Value = item.DispatchDate?.ToString("yyyy-MM-dd") ?? "N/A"; // Fallback if null
                    worksheet.Cells[row, 6].Value = item.DispatchQuantity.HasValue ? Math.Round(item.DispatchQuantity.Value, 2) : 0; // Handle nullable decimal
                    worksheet.Cells[row, 7].Value = item.BillNum ?? "N/A"; // Fallback if null
                    row++;
                }

                // Set content type and file name
                var fileContent = package.GetAsByteArray();
                var fileName = $"Bills_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}_{factoryId}.xlsx";

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }




        //public IActionResult CheckBills(DateTime startDate, DateTime endDate, int? factoryId)
        //{
        //    var factories = _context.TblFactories.ToList();
        //    ViewBag.Factories = factories;

        //    var result = _context.SpCheckBill
        //        .FromSqlRaw("EXECUTE [dbo].[SpCheckBill] @StartDate = {0}, @EndDate = {1}, @FactoryId = {2}", startDate, endDate, factoryId)
        //        .ToList();

        //    return View(result);
        //}



        [HttpGet]
        public IActionResult BillDetail()
        {
            try
            {
                var factories = _context.TblFactories.ToList();
                ViewBag.Factories = factories;

                if (factories == null || !factories.Any())
                {
                    return View("Error", new ErrorViewModel { Message = "Factories could not be loaded." });
                }

                ViewBag.Message = "Please provide FromDate, ToDate, and FactoryId to view bill details.";
                return View(new List<SpBilldetail>());
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = "An error occurred while processing your request." });
            }
        }

        //[HttpPost]
        //public IActionResult BillDetail(DateTime? fromDate, DateTime? toDate, int? factoryId)
        //    {
        //    try
        //    {
        //        var factories = _context.TblFactories.ToList();
        //        ViewBag.Factories = factories;

        //        if (factories == null || !factories.Any())
        //        {
        //            return View("Error", new ErrorViewModel { Message = "Factories could not be loaded." });
        //        }

        //        if (fromDate.HasValue && toDate.HasValue)
        //        {
        //            // Create parameters for the stored procedure execution
        //            SqlParameter[] parameters = new SqlParameter[]
        //            {
        //        new SqlParameter("@FromDate", fromDate.Value),
        //        new SqlParameter("@ToDate", toDate.Value),
        //        new SqlParameter("@FactoryId", factoryId.HasValue ? (object)factoryId.Value : DBNull.Value)
        //            };

        //            // Execute stored procedure and get the result
        //            var result = _context.SpBilldetails
        //                .FromSqlRaw("EXECUTE [dbo].[SpBilldetail] @FromDate, @ToDate, @FactoryId", parameters)
        //                .ToList();

        //            return View(result);
        //        }
        //        else
        //        {
        //            ViewBag.Message = "No data to display. Please provide valid FromDate and ToDate.";
        //            return View(new List<SpBilldetail>());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception details for debugging purposes
        //        // Logging.LogException(ex); // Example: Replace with your actual logging mechanism

        //        return View("Error", new ErrorViewModel { Message = "An error occurred while processing your request." });
        //    }
        //}



        public async Task<IActionResult> Details(int? id)
        {
            SpBilldetail bd = new SpBilldetail();
            var tblBill = _context.BillTables.FirstOrDefault(b => b.BillID == id);
            if (tblBill == null)
            {
                return NotFound();
            }
            bd.BillNum = tblBill.BillNum;
            bd.BillDate = Convert.ToDateTime(tblBill.BillDate);
            bd.BillType = tblBill.BillType;
            bd.FactoryName = _context.TblFactories.FirstOrDefault(f => f.FID == bd.FID)?.FactoryName;

            return View(bd);
        }


        public IEnumerable<BillTable> GetAllBills()
        {
            return _context.BillTables.ToList(); // Change AsEnumerable() to ToList()
        }

        [HttpPost]
        public JsonResult LoadGridDataTableByAjax()
        {
            var blst = GetAllBills();
            var billVm = blst.Select(b => new SpBilldetail()
            {
                FactoryName = _context.TblFactories.FirstOrDefault(f => f.FID == b.FID)?.FactoryName,
                BillNum = b.BillNum,
                BillDate = b.BillDate,
                BillType = b.BillType,


            }).ToList();

            return Json(billVm);
        }



        public IEnumerable<BillTable> ExportAllBills()
        {
            return _context.BillTables.Include(b => b.tblDispatches).ToList();
        }

        [HttpGet]
        public IActionResult BillExport()
        {
            var model = new List<BillDispatchViewModel>(); // Initially an empty list
            return View(model);
        }




        [HttpPost]
        public JsonResult LoadBillsToExport(DateTime fromDate, DateTime toDate, int factoryId)
        {
            var blst = _context.BillTables
                .Include(b => b.tblDispatches.Where(d => d.DispatchDate >= fromDate && d.DispatchDate <= toDate))
                .Where(b => b.FID == factoryId && b.tblDispatches.Any(d => d.DispatchDate >= fromDate && d.DispatchDate <= toDate))
                .ToList();

            if (blst == null)
            {
                return Json(new List<BillDispatchViewModel>()); // or handle accordingly
            }

            var billVm = blst
                .Where(b => b.tblDispatches != null) // Ensure tblDispatches is not null
                .SelectMany(b => b.tblDispatches.Select(d => new BillDispatchViewModel
                {
                    FactoryName = _context.TblFactories.FirstOrDefault(f => f.FID == b.FID)?.FactoryName,
                    BillNum = b.BillNum,
                    BillDate = b.BillDate,
                    Destination = d.Destination,
                    DispatchDate = d.DispatchDate,
                    BillType = b.BillType,
                    DispatchQuantity = d.DispatchQuantity,
                    VehicleNo = d.VehicleNo,
                    UnitPrice = d.UnitPrice,
                    FinalPrice = d.FinalPrice,
                    ChallanNo = d.ChallanNo,
                    TotalPrice = Convert.ToDouble(d.DispatchQuantity * d.UnitPrice), // Explicit casting
                    Difference = d.FinalPrice > 0
                        ? Convert.ToDouble(d.DispatchQuantity * d.UnitPrice) - Convert.ToDouble(d.FinalPrice)
                        : 0 // Explicit casting and conditional subtraction
                }))
                .ToList();

            return Json(billVm);
        }



        public JsonResult GetFactories()
        {
            var factories = _context.TblFactories.Select(f => new { id = f.FID, name = f.FactoryName }).ToList();
            return Json(factories);
        }


        [HttpGet]
        public IActionResult ShowDayQty()
        {
            var months = GetMonthsList();
            var factories = _context.TblFactories.Select(f => f.FactoryName).ToList();

            ViewBag.Months = months.Select(m => new SelectListItem { Value = m, Text = m }).ToList();
            ViewBag.Factories = factories.Select(f => new SelectListItem { Value = f, Text = f }).ToList();

            return View(new List<Sp_QtyByDay>());
        }



        [HttpPost]
        public IActionResult ShowDayQty(string yearMonth, string factoryName = null)
        {
            if (string.IsNullOrEmpty(yearMonth) || yearMonth.Length != 7 || yearMonth[4] != '/')
            {
                return BadRequest("Invalid yearMonth parameter. Expected format is yyyy/mm.");
            }

            var result = _context.Sp_QtyByDay
                .FromSqlRaw($"EXECUTE [dbo].[Sp_QtyByDay] @YearMonth = '{yearMonth}', @FactoryName = '{factoryName ?? "NULL"}'")
                .ToList();

            result = result ?? new List<Sp_QtyByDay>();

            var months = GetMonthsList();
            var factories = _context.TblFactories.Select(f => f.FactoryName).ToList();

            ViewBag.Months = months.Select(m => new SelectListItem { Value = m, Text = m }).ToList();
            ViewBag.Factories = factories.Select(f => new SelectListItem { Value = f, Text = f }).ToList();

            return View(result);
        }

        private List<string> GetMonthsList()
        {
            var months = new List<string>();
            for (int year = DateTime.Now.Year - 5; year <= DateTime.Now.Year; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    months.Add($"{year}/{month:D2}");
                }
            }
            return months;
        }
    }

    public class ErrorViewModel
    {
        public string Message { get; set; }
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }


}











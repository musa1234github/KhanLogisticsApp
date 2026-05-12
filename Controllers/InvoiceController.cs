using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using ExcelDataReader;
using System.Data;
using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace KhanLogistics.Controllers
{
    public class InvoiceController : Controller
    {
        TransportMgmtContext _transportMgmtContext;
        IConfiguration _configuration;
        IWebHostEnvironment _hostingEnvironment;
        IExcelDataReader _excelDataReader;

        public InvoiceController(TransportMgmtContext context, IWebHostEnvironment webHostEnvironment)
        {
            this._transportMgmtContext = context;
            //this._excelDataReader = excelDataReader;
            //this._configuration  = configuration;
            this._hostingEnvironment = webHostEnvironment;
            //this._hostingEnvironment = hostingEnvironment;

        }




        private double SafeNum(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            string s = value.ToString().Replace(",", "").Trim();
            if (double.TryParse(s, out double n)) return n;
            return 0;
        }

        private DateTime? ParseExcelDate(object value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return dt;
            string s = value.ToString().Trim();
            if (double.TryParse(s, out double d))
            {
                try { return DateTime.FromOADate(d); } catch { }
            }

            // Priority 1: Exact Indian/UK formats (dots, dashes, slashes)
            string[] formats = { 
                "dd.MM.yyyy", "dd.MM.yy", 
                "dd-MM-yyyy", "dd-MM-yy", 
                "dd/MM/yyyy", "dd/MM/yy", 
                "d.M.yyyy", "d.M.yy",
                "dd-MMM-yy", "dd-MMM-yyyy",
                "yyyy-MM-dd"
            };
            
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exactRes)) 
            {
                return exactRes;
            }

            // Priority 2: General parsing with Indian culture (dd/MM/yyyy)
            if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("en-IN"), DateTimeStyles.None, out DateTime res)) 
            {
                return res;
            }
            
            return null;
        }

        private string GetBillType(string value, string factory)
        {
            if (factory.Contains("JSW", StringComparison.OrdinalIgnoreCase)) return "Regular";
            if (factory.Contains("MP BIRLA", StringComparison.OrdinalIgnoreCase)) return value.Replace(".0", "").Trim();
            return value.Trim();
        }










public IActionResult UploadBill()
 {
     DispatchVm model = new DispatchVm();

     // Load only factory dropdown, no dispatch data
     model.ddlFactory = _transportMgmtContext.TblFactories
         .Select(a => new SelectListItem()
         {
             Text = a.FactoryName,
             Value = a.FID.ToString()
         }).ToList();

     // Keep dispatchVm empty so nothing loads initially
     model.dispatchVm = Enumerable.Empty<DispatchViewModel>();

     return View("UploadBill", model);
 }




        

        [HttpPost]
        public async Task<IActionResult> UploadBill(IFormFile file)
        {
            try
            {
                List<TblDispatch> updatedDispatches = new List<TblDispatch>(); // List to store updated dispatch records
                List<string> failedRecords = new List<string>(); // List to store failed record details
                Dictionary<string, BillTable> billDictionary = new Dictionary<string, BillTable>(); // Dictionary to store unique bill records mapped by bill number

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = file.OpenReadStream())
                {
                    _excelDataReader = ExcelReaderFactory.CreateReader(stream);
                    DataSet dataSet = _excelDataReader.AsDataSet();
                    _excelDataReader.Close();

                    if (dataSet != null && dataSet.Tables.Count > 0)
                    {
                        DataTable dataTable = dataSet.Tables[0];
                        if (dataTable.Rows.Count < 2)
                        {
                            return BadRequest("File is empty or missing data rows.");
                        }

                        // --- DYNAMIC HEADER MAPPING (Copied from BillUpload for consistency) ---
                        DataRow headerRow = dataTable.Rows[0];
                        int colChallan = -1, colQty = -1, colUnitPrice = -1, colFinalPrice = -1;
                        int colBillNum = -1, colBillDate = -1, colBillType = -1, colDeliveryNum = -1;

                        for (int c = 0; c < dataTable.Columns.Count; c++)
                        {
                            string header = headerRow[c]?.ToString()?.Trim().ToLower() ?? "";
                            if (header.Contains("challanno")) colChallan = c;
                            else if (header.Contains("quantity")) colQty = c;
                            else if (header.Contains("unitprice")) colUnitPrice = c;
                            else if (header.Contains("finalprice")) colFinalPrice = c;
                            else if (header.Contains("billnum")) colBillNum = c;
                            else if (header.Contains("billdate")) colBillDate = c;
                            else if (header.Contains("billtype") || header.Contains("lr")) colBillType = c;
                            else if (header.Contains("deliverynum")) colDeliveryNum = c;
                        }

                        // Fallbacks
                        if (colChallan == -1) colChallan = 0;
                        if (colQty == -1) colQty = 4;
                        if (colUnitPrice == -1) colUnitPrice = 6;
                        if (colFinalPrice == -1) colFinalPrice = 7;
                        if (colBillNum == -1) colBillNum = 8;
                        if (colBillDate == -1) colBillDate = 9;
                        if (colBillType == -1) colBillType = 10;
                        if (colDeliveryNum == -1) colDeliveryNum = 11;

                        foreach (DataRow row in dataTable.Rows)
                        {
                            // Skip header row
                            if (dataTable.Rows.IndexOf(row) == 0) continue;
                            if (row.ItemArray.All(v => v == null || string.IsNullOrWhiteSpace(v.ToString()))) continue;

                            var dispatchId = row[colChallan]?.ToString()?.Trim();

                            if (!string.IsNullOrWhiteSpace(dispatchId))
                            {
                                // Clean up MP BIRLA challan number
                                if (dispatchId.EndsWith(".0")) dispatchId = dispatchId.Substring(0, dispatchId.Length - 2);

                                var existingDispatch = _transportMgmtContext.TblDispatches
                                    .Include(d => d.bill)
                                    .FirstOrDefault(c => c.ChallanNo == dispatchId);

                                if (existingDispatch != null)
                                {
                                    try
                                    {
                                        var billNumber = row[colBillNum]?.ToString()?.Trim();

                                        if (existingDispatch.BillID != null && existingDispatch.bill?.BillNum != billNumber)
                                        {
                                            failedRecords.Add($"Dispatch ID: {dispatchId} already linked with Bill {existingDispatch.bill?.BillNum}.");
                                            continue;
                                        }

                                        var factoryId = existingDispatch.DisVid;
                                        string factoryName = _transportMgmtContext.TblFactories.Find(factoryId)?.FactoryName?.ToUpper() ?? "";

                                        double unitPrice = SafeNum(row[colUnitPrice]);
                                        double finalPrice = SafeNum(row[colFinalPrice]);
                                        double quantity = SafeNum(row[colQty]);
                                        DateTime? billDate = ParseExcelDate(row[colBillDate]);

                                        existingDispatch.UnitPrice = unitPrice;
                                        existingDispatch.FinalPrice = finalPrice;
                                        existingDispatch.DispatchQuantity = quantity;
                                        existingDispatch.TotalValue = unitPrice * quantity;


                                        if (!string.IsNullOrWhiteSpace(billNumber))
                                        {
                                            if (!billDictionary.ContainsKey(billNumber))
                                            {
                                                var existingBill = _transportMgmtContext.BillTables.FirstOrDefault(b => b.BillNum == billNumber && b.FID == factoryId);
                                                if (existingBill == null)
                                                {
                                                    var newBill = new BillTable
                                                    {
                                                        BillNum = billNumber,
                                                        BillDate = billDate ?? DateTime.Now,
                                                        BillType = GetBillType(row[colBillType]?.ToString() ?? "", factoryName),
                                                        FID = factoryId
                                                    };

                                                    _transportMgmtContext.BillTables.Add(newBill);
                                                    await _transportMgmtContext.SaveChangesAsync();
                                                    billDictionary.Add(billNumber, newBill);
                                                }
                                                else
                                                {
                                                    // Update date if bill already exists to allow corrections via re-upload
                                                    if (billDate.HasValue)
                                                    {
                                                        existingBill.BillDate = billDate.Value;
                                                    }
                                                    billDictionary.Add(billNumber, existingBill);
                                                }
                                            }
                                            existingDispatch.BillID = billDictionary[billNumber].BillID;
                                        }

                                        // Handle LR and Delivery Number
                                        existingDispatch.DeliveryNum = colDeliveryNum != -1 ? row[colDeliveryNum]?.ToString()?.Trim() : "";
                                        if (factoryName.Contains("JSW") && colBillType != -1)
                                        {
                                            existingDispatch.Lr = row[colBillType]?.ToString()?.Trim();
                                        }

                                        updatedDispatches.Add(existingDispatch);
                                    }
                                    catch (Exception ex)
                                    {
                                        failedRecords.Add($"Dispatch ID: {dispatchId}, Error: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    failedRecords.Add($"Dispatch ID: {dispatchId} not found.");
                                }
                            }
                        }

                        // Update all dispatch records with new data
                        if (updatedDispatches.Count > 0)
                        {
                            _transportMgmtContext.TblDispatches.UpdateRange(updatedDispatches);
                            await _transportMgmtContext.SaveChangesAsync();
                        }

                        return Ok(new { SuccessCount = updatedDispatches.Count, FailedRecords = failedRecords });
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Handle specific database update exceptions
                return BadRequest($"An error occurred while saving the entity changes: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                return BadRequest($"An error occurred while processing the invoice: {ex.Message}");
            }

            // Add a default return statement in case all other code paths are not executed
            return BadRequest("An error occurred while processing the invoice.");
        }




        public IActionResult ShowBillToDelete(int? factoryId = null, string billNumber = null)
        {
            DispatchVm model = new DispatchVm();

            // Populate Factory dropdown
            model.ddlFactory = _transportMgmtContext.TblFactories.ToList().Select(a => new SelectListItem()
            {
                Text = a.FactoryName,
                Value = Convert.ToString(a.FID)
            }).ToList();

            // Initialize dispatch view model as an empty list
            var dispatches = Enumerable.Empty<TblDispatch>().AsQueryable();

            // Fetch filtered dispatch records only if a filter is provided
            if (factoryId.HasValue || !string.IsNullOrWhiteSpace(billNumber))
            {
                dispatches = _transportMgmtContext.TblDispatches.AsQueryable();

                if (factoryId.HasValue)
                {
                    dispatches = dispatches.Where(d => d.DisVid == factoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(billNumber))
                {
                    dispatches = dispatches.Where(d => d.bill.BillNum == billNumber);
                }
            }

            // Populate dispatch view model
            model.dispatchVm = dispatches.ToList().Select(a => new DispatchViewModel()
            {
                FID = a.DisVid,
                DispId = a.DispId,
                DispatchDate = Convert.ToDateTime(a.DispatchDate),
                DispatchQuantity = a.DispatchQuantity,
                VehicleNo = a.VehicleNo,
                ChallanNo = a.ChallanNo,
                DeliveryNum = a.DeliveryNum,
                Lr = a.Lr,
                TotalValue = a.TotalValue,
                UnitPrice = a.UnitPrice,
                Destination = a.Destination,
                FactoryName = _transportMgmtContext.TblFactories.FirstOrDefault(f => f.FID == a.DisVid)?.FactoryName,
            }).ToList();

            return View("ShowBillToDelete", model);
        }




        [HttpPost]
        public async Task<IActionResult> DeleteSingleOrMultiple([FromBody] int[] ids)
        {
            string result;
            try
            {
                if (ids != null && ids.Any())
                {
                    var dispatchesToDelete = await _transportMgmtContext.TblDispatches
                        .Where(d => ids.Contains(d.DispId))
                        .ToListAsync();

                    if (dispatchesToDelete.Any())
                    {
                        _transportMgmtContext.TblDispatches.RemoveRange(dispatchesToDelete);
                        await _transportMgmtContext.SaveChangesAsync();
                        TempData["success"] = "Selected dispatch records deleted successfully.";
                        result = "success";
                    }
                    else
                    {
                        result = "No matching records found to delete.";
                    }
                }
                else
                {
                    result = "No records selected for deletion.";
                }
            }
            catch (Exception ex)
            {
                result = $"Error occurred during deletion: {ex.Message}";
            }
            return Json(result);
        }




        public IActionResult FetchDispatch(DateTime startDate, DateTime endDate, int? factoryId)
        {
            var factories = _transportMgmtContext.TblFactories.ToList();
            ViewBag.Factories = factories;

            var result = _transportMgmtContext.SpGetDispatches
                .FromSqlRaw("EXECUTE [dbo].[SpGetDispatch] @StartDate = {0}, @EndDate = {1}, @FactoryId = {2}",
                             startDate, endDate, factoryId)
                .ToList();

            ViewData["StartDate"] = startDate.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.ToString("yyyy-MM-dd");
            ViewData["FactoryId"] = factoryId ?? 0;

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateBill([FromBody] BillRequestModel model)
        {
            try
            {
                if (model == null || model.SelectedDispatchIds == null || !model.SelectedDispatchIds.Any())
                {
                    return BadRequest("No records selected.");
                }

                foreach (var dispId in model.SelectedDispatchIds)
                {
                    var dispatch = await _transportMgmtContext.TblDispatches.FirstOrDefaultAsync(d => d.DispId == dispId);
                    if (dispatch != null)
                    {
                        var existingBill = await _transportMgmtContext.BillTables.FirstOrDefaultAsync(b => b.BillNum == model.BillNumber);

                        if (existingBill == null)
                        {
                            var newBill = new BillTable
                            {
                                BillNum = model.BillNumber,
                                BillDate = model.BillDate,
                                BillType = "Invoice",
                                FID = 10
                            };

                            _transportMgmtContext.BillTables.Add(newBill);
                            await _transportMgmtContext.SaveChangesAsync();
                            dispatch.BillID = newBill.BillID;
                        }
                        else
                        {
                            dispatch.BillID = existingBill.BillID;
                        }
                    }
                }

                await _transportMgmtContext.SaveChangesAsync();
                return Ok(new { message = "Bill updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Error updating bill: {ex.Message}" });
            }
        }

        // ViewModel for Bill Creation
        public class BillRequestModel
        {
            public List<int> SelectedDispatchIds { get; set; }
            public string BillNumber { get; set; }
            public DateTime BillDate { get; set; }
        }
    }
}

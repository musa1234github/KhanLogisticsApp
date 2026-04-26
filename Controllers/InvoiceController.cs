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


        public IActionResult UploadBill()
        {
            DispatchVm model = new DispatchVm
            {
                ddlFactory = _transportMgmtContext.TblFactories
                    .Select(a => new SelectListItem
                    {
                        Text = a.FactoryName,
                        Value = a.FID.ToString()
                    })
                    .ToList()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UploadBill(IFormFile file, int selectedFactoryId)
        {
            int successCount = 0;
            int failureCount = 0;
            List<string> failedRecords = new List<string>();
            List<string> successfulRecords = new List<string>();

            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file.";
                    return RedirectToAction("UploadBill");
                }

                var factory = _transportMgmtContext.TblFactories.Find(selectedFactoryId);
                string factoryName = factory?.FactoryName?.ToUpper() ?? "";

                string dirPath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                string fileName = Path.GetFileName(file.FileName);
                string filePath = Path.Combine(dirPath, fileName);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                List<ParsedRow> parsedRows = new List<ParsedRow>();
                HashSet<string> uniqueChallansInFile = new HashSet<string>();

                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    _excelDataReader = ExcelReaderFactory.CreateReader(stream);
                    DataSet dataSet = _excelDataReader.AsDataSet();
                    _excelDataReader.Close();

                    if (dataSet != null && dataSet.Tables.Count > 0)
                    {
                        DataTable dataTable = dataSet.Tables[0];
                        if (dataTable.Rows.Count < 2)
                        {
                            TempData["ErrorMessage"] = "File is empty or missing data rows.";
                            return RedirectToAction("UploadBill");
                        }

                        // --- DYNAMIC HEADER MAPPING ---
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

                        // Fallback to defaults if headers not found (using the JSW/Ultra standard seen in screenshot)
                        if (colChallan == -1) colChallan = 0;
                        if (colQty == -1) colQty = 4;
                        if (colUnitPrice == -1) colUnitPrice = 6;
                        if (colFinalPrice == -1) colFinalPrice = 7;
                        if (colBillNum == -1) colBillNum = 8;
                        if (colBillDate == -1) colBillDate = 9;
                        if (colBillType == -1) colBillType = 10;
                        if (colDeliveryNum == -1) colDeliveryNum = 11;

                        // Skip header row (i = 1)
                        for (int i = 1; i < dataTable.Rows.Count; i++)
                        {
                            DataRow row = dataTable.Rows[i];
                            if (row.ItemArray.All(v => v == null || string.IsNullOrWhiteSpace(v.ToString())))
                                continue;

                            string challanNo = row[colChallan]?.ToString()?.Trim() ?? "";
                            // For MP BIRLA, clean up challan number (remove .0 if present)
                            if (factoryName.Contains("MP BIRLA") && challanNo.EndsWith(".0"))
                            {
                                challanNo = challanNo.Substring(0, challanNo.Length - 2);
                            }

                            if (string.IsNullOrEmpty(challanNo)) continue;

                            if (uniqueChallansInFile.Contains(challanNo))
                            {
                                failedRecords.Add($"{challanNo} (Duplicate in file)");
                                failureCount++;
                                continue;
                            }
                            uniqueChallansInFile.Add(challanNo);

                            ParsedRow pRow = new ParsedRow
                            {
                                OriginalRowIndex = i + 1,
                                ChallanNo = challanNo,
                                Quantity = colQty >= 0 ? SafeNum(row[colQty]) : 0,
                                UnitPrice = colUnitPrice >= 0 ? SafeNum(row[colUnitPrice]) : 0,
                                FinalPrice = colFinalPrice >= 0 ? SafeNum(row[colFinalPrice]) : 0,
                                BillNum = colBillNum >= 0 ? row[colBillNum]?.ToString()?.Trim() ?? "" : "",
                                BillDate = colBillDate >= 0 ? ParseExcelDate(row[colBillDate]) : null,
                                BillTypeOrLR = colBillType >= 0 ? row[colBillType]?.ToString()?.Trim() ?? "" : "",
                                DeliveryNum = colDeliveryNum >= 0 ? row[colDeliveryNum]?.ToString()?.Trim() ?? "" : ""
                            };

                            if (string.IsNullOrEmpty(pRow.BillNum) || pRow.BillDate == null)
                            {
                                failedRecords.Add($"{challanNo} (Missing BillNum or BillDate)");
                                failureCount++;
                                continue;
                            }

                            parsedRows.Add(pRow);
                        }
                    }
                }

                if (parsedRows.Count == 0)
                {
                    TempData["ErrorMessage"] = "No valid data found in the file.";
                    return RedirectToAction("UploadBill");
                }

                // --- PRE-AGGREGATE BILL TOTALS ---
                var billTotalsMap = new Dictionary<string, BillTotals>();
                foreach (var row in parsedRows)
                {
                    if (!billTotalsMap.ContainsKey(row.BillNum))
                    {
                        billTotalsMap[row.BillNum] = new BillTotals();
                    }

                    var bt = billTotalsMap[row.BillNum];
                    double taxable = row.UnitPrice * row.Quantity;
                    bool fpValid = row.FinalPrice > 0 && (taxable == 0 || row.FinalPrice <= taxable);

                    bt.BillQuantity += row.Quantity;
                    bt.TaxableAmount += taxable;
                    bt.TotalFinalPrice += row.FinalPrice;
                    bt.DispatchCount += 1;
                    if (fpValid) bt.FpValidCount += 1;
                }

                const double GST_RATE = 0.18;
                const double TDS_RATE = 0.00984;

                // Finalize Bill Totals and sync with DB
                Dictionary<string, BillTable> billDbCache = new Dictionary<string, BillTable>();
                foreach (var entry in billTotalsMap)
                {
                    string bNum = entry.Key;
                    var bt = entry.Value;
                    bool allHaveFP = bt.DispatchCount > 0 && bt.FpValidCount == bt.DispatchCount;

                    double baseAmount = allHaveFP ? bt.TotalFinalPrice : bt.TaxableAmount;
                    if (bt.TaxableAmount == 0 && baseAmount > 0)
                    {
                        bt.TaxableAmount = baseAmount;
                    }

                    bt.CalculatedGST = baseAmount * GST_RATE;
                    bt.CalculatedTDS = baseAmount * TDS_RATE;
                    bt.CalculatedActualAmount = baseAmount + bt.CalculatedGST;

                    // Fetch or Create Bill
                    var existingBill = _transportMgmtContext.BillTables
                        .FirstOrDefault(b => b.BillNum == bNum && b.FID == selectedFactoryId);

                    if (existingBill == null)
                    {
                        existingBill = new BillTable
                        {
                            BillNum = bNum,
                            BillDate = parsedRows.First(r => r.BillNum == bNum).BillDate,
                            BillType = GetBillType(parsedRows.First(r => r.BillNum == bNum).BillTypeOrLR, factoryName),
                            FID = selectedFactoryId
                        };
                        _transportMgmtContext.BillTables.Add(existingBill);
                    }
                    else
                    {
                        // Update BillType and Date for existing bills to allow corrections
                        existingBill.BillType = GetBillType(parsedRows.First(r => r.BillNum == bNum).BillTypeOrLR, factoryName);
                        existingBill.BillDate = parsedRows.First(r => r.BillNum == bNum).BillDate;
                    }

                    existingBill.Gst = bt.CalculatedGST;
                    existingBill.Tds = bt.CalculatedTDS;
                    existingBill.ActualAmount = bt.CalculatedActualAmount;
                    existingBill.TotalValue = bt.TaxableAmount; 

                    billDbCache[bNum] = existingBill;
                }

                // Save bills to get IDs if new
                await _transportMgmtContext.SaveChangesAsync();

                // --- UPDATE DISPATCHES ---
                foreach (var row in parsedRows)
                {
                    var existingDispatch = _transportMgmtContext.TblDispatches
                        .Include(d => d.bill)
                        .FirstOrDefault(d => d.ChallanNo == row.ChallanNo && d.DisVid == selectedFactoryId);

                    if (existingDispatch == null)
                    {
                        failedRecords.Add($"{row.ChallanNo} (Dispatch Not Found)");
                        failureCount++;
                        continue;
                    }

                    if (existingDispatch.BillID != null && existingDispatch.bill?.BillNum != row.BillNum)
                    {
                        failedRecords.Add($"{row.ChallanNo} (Already assigned to Bill {existingDispatch.bill?.BillNum})");
                        failureCount++;
                        continue;
                    }

                    existingDispatch.UnitPrice = row.UnitPrice;
                    existingDispatch.FinalPrice = row.FinalPrice;
                    existingDispatch.DispatchQuantity = row.Quantity;
                    existingDispatch.DeliveryNum = row.DeliveryNum;
                    existingDispatch.BillID = billDbCache[row.BillNum].BillID;
                    existingDispatch.TotalValue = row.UnitPrice * row.Quantity;

                    if (factoryName.Contains("JSW") && !string.IsNullOrEmpty(row.BillTypeOrLR))
                    {
                        existingDispatch.Lr = row.BillTypeOrLR;
                    }

                    successCount++;
                    successfulRecords.Add(row.ChallanNo);
                }

                await _transportMgmtContext.SaveChangesAsync();

                TempData["SuccessCount"] = successCount;
                TempData["FailureCount"] = failureCount;
                TempData["SuccessfulRecords"] = successfulRecords;
                TempData["FailedRecords"] = failedRecords;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("UploadBill");
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
            if (DateTime.TryParse(s, out DateTime parsedDt)) return parsedDt;

            string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "dd-MMM-yy", "dd-MM-yy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MMM-yyyy" };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime exactDt))
                return exactDt;

            return null;
        }

        private string GetBillType(string value, string factory)
        {
            if (factory.Contains("JSW", StringComparison.OrdinalIgnoreCase)) return "Regular";
            if (factory.Contains("MP BIRLA", StringComparison.OrdinalIgnoreCase)) return value.Replace(".0", "").Trim();
            return value.Trim();
        }

        private class ParsedRow
        {
            public int OriginalRowIndex { get; set; }
            public string ChallanNo { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public double FinalPrice { get; set; }
            public string BillNum { get; set; }
            public DateTime? BillDate { get; set; }
            public string BillTypeOrLR { get; set; }
            public string DeliveryNum { get; set; }
        }

        private class BillTotals
        {
            public double BillQuantity { get; set; }
            public double TaxableAmount { get; set; }
            public double TotalFinalPrice { get; set; }
            public int DispatchCount { get; set; }
            public int FpValidCount { get; set; }
            public double CalculatedGST { get; set; }
            public double CalculatedTDS { get; set; }
            public double CalculatedActualAmount { get; set; }
        }




        //[HttpPost]
        //public async Task<IActionResult> UploadBill(IFormFile file, int selectedFactoryId)
        //{
        //    int successCount = 0;
        //    int failureCount = 0;
        //    List<string> failedRecords = new List<string>();
        //    List<string> successfulRecords = new List<string>();

        //    try
        //    {
        //        string dirPath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
        //        if (!Directory.Exists(dirPath))
        //        {
        //            Directory.CreateDirectory(dirPath);
        //        }

        //        string fileName = Path.GetFileName(file.FileName);
        //        string filePath = Path.Combine(dirPath, fileName);

        //        // Save the uploaded file to server
        //        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            file.CopyTo(stream);
        //        }

        //        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        //        using (var stream = new FileStream(filePath, FileMode.Open))
        //        {
        //            _excelDataReader = ExcelReaderFactory.CreateReader(stream);
        //            DataSet dataSet = _excelDataReader.AsDataSet();
        //            _excelDataReader.Close();

        //            if (dataSet != null && dataSet.Tables.Count > 0)
        //            {
        //                List<TblDispatch> updatedDispatches = new List<TblDispatch>();
        //                Dictionary<string, BillTable> billDictionary = new Dictionary<string, BillTable>();

        //                foreach (DataTable dataTable in dataSet.Tables)
        //                {
        //                    foreach (DataRow row in dataTable.Rows)
        //                    {
        //                        try
        //                        {
        //                            string dispatchId = row[0].ToString();
        //                            if (!string.IsNullOrWhiteSpace(dispatchId))
        //                            {
        //                                var existingDispatch = _transportMgmtContext.TblDispatches
        //                                    .FirstOrDefault(c => c.ChallanNo == dispatchId);

        //                                if (existingDispatch != null)
        //                                {
        //                                    // Update dispatch details
        //                                    existingDispatch.UnitPrice = double.TryParse(row[5].ToString(), out var unitPrice) ? unitPrice : 0;
        //                                    existingDispatch.FinalPrice = double.TryParse(row[6].ToString(), out var finalPrice) ? finalPrice : 0;

        //                                    // Handle BillTable updates
        //                                    var billNumber = row[7].ToString(); // Bill number (8th column)
        //                                    if (!string.IsNullOrWhiteSpace(billNumber))
        //                                    {
        //                                        // Check if the bill already exists in the BillTable
        //                                        if (!billDictionary.ContainsKey(billNumber))
        //                                        {
        //                                            var existingBill = _transportMgmtContext.BillTables.FirstOrDefault(b => b.BillNum == billNumber);
        //                                            if (existingBill == null)
        //                                            {
        //                                                var newBill = new BillTable
        //                                                {
        //                                                    BillNum = billNumber,
        //                                                    BillDate = DateTime.TryParse(row[8].ToString(), out DateTime billDate) ? billDate : DateTime.Now, // Bill date (9th column)
        //                                                    BillType = row[9].ToString(), // Bill type (10th column)
        //                                                    FID = selectedFactoryId
        //                                                };

        //                                                _transportMgmtContext.BillTables.Add(newBill);
        //                                                await _transportMgmtContext.SaveChangesAsync();
        //                                                billDictionary.Add(billNumber, newBill);
        //                                            }
        //                                            else
        //                                            {
        //                                                billDictionary.Add(billNumber, existingBill);
        //                                            }
        //                                        }

        //                                        // Retrieve the bill object from the dictionary
        //                                        var billToUpdate = billDictionary[billNumber];

        //                                        // We update the LR and Delivery Number in TblDispatch if the factory is JSW (ID = 10)
        //                                        if (selectedFactoryId == 10) // JSW
        //                                        {
        //                                            string newLr = row[9].ToString(); // LR from the uploaded file (10th column)
        //                                            string newDeliveryNum = row[10].ToString(); // Delivery Number from the uploaded file (11th column)

        //                                            // Update LR and DeliveryNum in dispatch if necessary
        //                                            if (existingDispatch.Lr != newLr)
        //                                            {
        //                                                existingDispatch.Lr = newLr;
        //                                            }
        //                                            if (existingDispatch.DeliveryNum != newDeliveryNum)
        //                                            {
        //                                                existingDispatch.DeliveryNum = newDeliveryNum;
        //                                            }

        //                                            // Save after each dispatch update to ensure the changes are committed
        //                                            await _transportMgmtContext.SaveChangesAsync();
        //                                        }

        //                                        // Update the BillID association with TblDispatch
        //                                        existingDispatch.BillID = billToUpdate.BillID; // Associate Bill with Dispatch
        //                                    }

        //                                    updatedDispatches.Add(existingDispatch); // Add to list for batch update
        //                                    successfulRecords.Add(dispatchId);
        //                                    successCount++;
        //                                }
        //                                else
        //                                {
        //                                    failedRecords.Add(dispatchId);
        //                                    failureCount++;
        //                                }
        //                            }
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            failedRecords.Add(row[0].ToString());
        //                            failureCount++;
        //                        }
        //                    }
        //                }

        //                // Perform a batch update for dispatch records
        //                if (updatedDispatches.Any())
        //                {
        //                    _transportMgmtContext.TblDispatches.UpdateRange(updatedDispatches);
        //                    await _transportMgmtContext.SaveChangesAsync();
        //                }
        //            }
        //        }

        //        // Set success/failure counts in TempData for feedback
        //        TempData["SuccessCount"] = successCount;
        //        TempData["FailureCount"] = failureCount;
        //        TempData["SuccessfulRecords"] = successfulRecords;
        //        TempData["FailedRecords"] = failedRecords;
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
        //    }

        //    return RedirectToAction("UploadBill");
        //}



public IActionResult ShowInvoice()
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

     return View("ShowInvoice", model);
 }




        // public IActionResult ShowInvoice()
        // {
        //     DispatchVm model = new DispatchVm();
        //     var vendors = _transportMgmtContext.TblFactories.ToList();
        //     model.ddlFactory = _transportMgmtContext.TblFactories.ToList().Select(a => new SelectListItem()
        //     {
        //         Text = a.FactoryName,
        //         Value = Convert.ToString(a.FID)
        //     }).ToList();
        //     model.dispatchVm = _transportMgmtContext.TblDispatches.ToList().Select(a => new DispatchViewModel()
        //     {
        //         FID = a.DisVid,
        //         DispId = Convert.ToInt32(a.DispId),
        //         DispatchDate = Convert.ToDateTime(a.DispatchDate),
        //         DispatchQuantity = Convert.ToDouble(a.DispatchQuantity),
        //         VehicleNo = a.VehicleNo,
        //         ChallanNo = a.ChallanNo,
        //         UnitPrice = Convert.ToDouble(a.UnitPrice),
        //         TotalValue = Convert.ToDouble(a.TotalValue),
        //         Destination = Convert.ToString(a.Destination),
        //         Shortage = Convert.ToInt32(a.Shortage),
        //         Lr = a.Lr,
        //         IsReceived = a.IsReceived,
        //         FactoryName = vendors.FirstOrDefault(f => f.FID == a.DisVid)?.FactoryName,

        //     }).AsEnumerable();

        //     return View("ShowInvoice", model);
        // }

        [HttpPost]
        public async Task<IActionResult> ShowInvoice(IFormFile file)
        {
            try
            {
                List<TblDispatch> updatedDispatches = new List<TblDispatch>(); // List to store updated dispatch records
                List<string> failedRecords = new List<string>(); // List to store failed record details
                Dictionary<string, BillTable> billDictionary = new Dictionary<string, BillTable>(); // Dictionary to store unique bill records mapped by bill number

                string filename = $"{_hostingEnvironment.WebRootPath}\files{file.FileName}";
                string dirpath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                string datafilename = Path.GetFileName(file.FileName);
                string savetopath = Path.Combine(dirpath, datafilename);
                string extension = Path.GetExtension(datafilename);

                // Save file to server
                using (FileStream stream = new FileStream(savetopath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Flush();
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = new FileStream(savetopath, FileMode.Open))
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

                        // --- DYNAMIC HEADER MAPPING (Copied from UploadBill for consistency) ---
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

                                var existingDispatch = _transportMgmtContext.TblDispatches.FirstOrDefault(c => c.ChallanNo == dispatchId);
                                if (existingDispatch != null)
                                {
                                    try
                                    {
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

                                        var billNumber = row[colBillNum]?.ToString()?.Trim();
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



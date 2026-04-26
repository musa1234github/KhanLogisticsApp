using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using ExcelDataReader;
using System.Data;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Composition;

namespace KhanLogistics.Controllers
{
    public class DispatchController : Controller
    {
        TransportMgmtContext _transportMgmtContext;
        IConfiguration _configuration;
        IWebHostEnvironment _hostingEnvironment;
        IExcelDataReader _excelDataReader;
        ILogger<DispatchController> _logger; // Inject Logger
       

        public DispatchController(TransportMgmtContext context, IWebHostEnvironment webHostEnvironment, ILogger<DispatchController> logger)
        {
            this._transportMgmtContext = context;
            this._hostingEnvironment = webHostEnvironment;
            _logger = logger;
            

        }


        public IActionResult UploadDispatch()
        {
            DispatchVm model = new DispatchVm
            {
                ddlFactory = _transportMgmtContext.TblFactories.ToList().Select(a => new SelectListItem
                {
                    Text = a.FactoryName,
                    Value = a.FID.ToString()
                }).ToList()
            };

            return View("UploadDispatch", model);
        }



        [HttpPost]
        public async Task<IActionResult> UploadDispatch(IFormFile file, int FID)
        {
            int successfulUploads = 0;
            int updatedRecords = 0;
            int skippedRows = 0;
            int duplicateRows = 0;
            List<string> errors = new List<string>();

            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "File is empty or null.";
                    return RedirectToAction("UploadDispatch");
                }

                if (FID <= 0)
                {
                    TempData["Error"] = "Please select a factory.";
                    return RedirectToAction("UploadDispatch");
                }

                string dirpath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                if (!Directory.Exists(dirpath)) Directory.CreateDirectory(dirpath);

                string datafilename = Path.GetFileName(file.FileName);
                string savetopath = Path.Combine(dirpath, datafilename);

                using (FileStream stream = new FileStream(savetopath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = new FileStream(savetopath, FileMode.Open))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = false }
                        });

                        if (result == null || result.Tables.Count == 0)
                        {
                            TempData["Error"] = "No data found in Excel.";
                            return RedirectToAction("UploadDispatch");
                        }

                        DataTable dt = result.Tables[0];
                        
                        // --- DYNAMIC COLUMN MAPPING ---
            int colChallan = -1, colDate = -1, colQty = -1, colVehicle = -1, colParty = -1, colDest = -1, colExNo = -1, colUnitPrice = -1;
                        int headerRowIndex = -1;
                        string headerNames = "";

                        // Keywords matching the React logic
                        string[] keywords = { "CHALLAN", "LR", "VEHICLE", "TRUCK", "DISPATCH", "SOLD", "QTY", "QUANTITY", "WT", "DEST", "DELIVERY", "INV", "BILL", "SHIPMENT" };

                        // Scan first 50 rows for header (increased range for files with logos/headers)
                        for (int r = 0; r < Math.Min(dt.Rows.Count, 50); r++)
                        {
                            bool isHeader = false;
                            for (int c = 0; c < dt.Columns.Count; c++)
                            {
                                string val = dt.Rows[r][c]?.ToString()?.ToUpper() ?? "";
                                if (keywords.Any(k => val.Contains(k))) { isHeader = true; break; }
                            }

                            if (isHeader)
                            {
                                headerRowIndex = r;
                                for (int c = 0; c < dt.Columns.Count; c++)
                                {
                                    string rawH = dt.Rows[r][c]?.ToString() ?? "";
                                    string h = rawH.ToUpper().Replace(" ", "").Replace(".", "").Replace("-", "").Replace("_", "").Replace("#", "");
                                    if (string.IsNullOrEmpty(h)) continue;

                                    // --- CHALLAN PRIORITY (Strong matches first) ---
                                    bool isStrongChallan = h == "CHALLAN" || h == "CHALLANNO" || h == "DELIVERYNO" || h == "DINO" || h == "LRNO" || h == "SHIPMENTNO" || h == "INVOICENO";
                                    bool isWeakChallan = h.Contains("CHALLAN") || h.Contains("DELIVERY") || h == "DI" || h.Contains("LR") || h.Contains("SHIPMENT") || h.Contains("INV") || h.Contains("BILL") || h.Contains("DOC") || h.Contains("GR") || h.Contains("DC") || h.Contains("DN") || h.Contains("DO") || h.Contains("INTERNAL") || h.Contains("NUMBER");

                                    if (isWeakChallan && !h.Contains("DATE")) 
                                    { 
                                        // If we haven't found a challan yet, OR if this is a "strong" match and the previous one wasn't
                                        if (colChallan == -1 || isStrongChallan)
                                        {
                                            colChallan = c; 
                                            // Update headerNames: remove old mapping if we found a better one
                                            if (isStrongChallan) {
                                                headerNames = string.Join(", ", headerNames.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Where(x => !x.Contains("Challan=")));
                                                if (!string.IsNullOrEmpty(headerNames)) headerNames += ", ";
                                            }
                                            headerNames += $"Challan='{rawH}'(Col {c+1}), "; 
                                        }
                                    }
                                    
                                    // --- VEHICLE PRIORITY ---
                                    if ((h.Contains("VEHICLE") || h.Contains("TRUCK") || h.Contains("LORRY") || h.Contains("REGO")) && colVehicle == -1) 
                                    { 
                                        colVehicle = c; headerNames += $"Vehicle='{rawH}'(Col {c+1}), "; 
                                    }
                                    
                                    // --- DATE PRIORITY (Avoid names/parties) ---
                                    if ((h.Contains("DATE") || h == "DT") && !h.Contains("NAME") && !h.Contains("PARTY")) 
                                    { 
                                        // If we already have a date col, check if this one is "better" (contains dots/slashes in the first data row)
                                        bool isBetter = colDate == -1;
                                        if (!isBetter && r + 1 < dt.Rows.Count)
                                        {
                                            string firstVal = dt.Rows[r + 1][c]?.ToString() ?? "";
                                            if (firstVal.Contains(".") || firstVal.Contains("/") || firstVal.Contains("-")) isBetter = true;
                                        }

                                        if (isBetter)
                                        {
                                            colDate = c; 
                                            headerNames += $"Date='{rawH}'(Col {c+1}), "; 
                                        }
                                    }
                                    else if (h.Contains("DISPATCH") && colDate == -1)
                                    {
                                        colDate = c; headerNames += $"Date='{rawH}'(Col {c+1}), ";
                                    }
                                    
                                    // --- QTY PRIORITY ---
                                    if ((h.Contains("QTY") || h.Contains("QUANTITY") || h == "WT" || h.Contains("WEIGHT")) && colQty == -1) 
                                    { 
                                        colQty = c; headerNames += $"Qty='{rawH}'(Col {c+1}), "; 
                                    }
                                    else if (h.Contains("MT") && colQty == -1)
                                    {
                                        colQty = c; headerNames += $"Qty='{rawH}'(Col {c+1}), ";
                                    }

                                    // --- EX NUMBER ---
                                    if ((h.Contains("EXNUMBER") || h.Contains("EXNO")) && colExNo == -1)
                                    {
                                        colExNo = c; headerNames += $"ExNo='{rawH}'(Col {c+1}), ";
                                    }

                                    // --- PARTY ---
                                    if ((h.Contains("PARTY") || h.Contains("CUSTOMER") || h.Contains("CONSIGNEE") || h.Contains("SOLDTOPARTY")) && colParty == -1 && !h.Contains("DATE"))
                                    {
                                        colParty = c; headerNames += $"Party='{rawH}'(Col {c+1}), ";
                                    }

                                    // --- DESTINATION ---
                                    if ((h.Contains("DESTINATION") || h.Contains("CITY") || h.Contains("TOCITY") || h.Contains("PLANT")) && colDest == -1)
                                    {
                                        colDest = c; headerNames += $"Dest='{rawH}'(Col {c+1}), ";
                                    }

                                    // --- UNIT PRICE ---
                                    if ((h.Contains("UNITPRICE") || h.Contains("RATE")) && colUnitPrice == -1)
                                    {
                                        colUnitPrice = c; headerNames += $"Price='{rawH}'(Col {c+1}), ";
                                    }
                                }
                                break;
                            }
                        }

                        if (colChallan == -1 || colDate == -1 || colQty == -1)
                        {
                            string allDetected = "";
                            for (int c = 0; c < dt.Columns.Count; c++) {
                                string val = dt.Rows[headerRowIndex][c]?.ToString() ?? "EMPTY";
                                if (!string.IsNullOrWhiteSpace(val)) allDetected += $"Col {c+1}: '{val}', ";
                            }
                            TempData["Error"] = "Could not map required columns. Found: " + headerNames + " | All Headers: " + allDetected;
                            return RedirectToAction("UploadDispatch");
                        }

                        int skippedNoChallan = 0;
                        List<TblDispatch> newRecords = new List<TblDispatch>();
                        HashSet<string> processedChallansInFile = new HashSet<string>();

                        string mappingInfo = headerNames.TrimEnd(',', ' ');


                        for (int i = headerRowIndex + 1; i < dt.Rows.Count; i++)
                        {
                            DataRow row = dt.Rows[i];
                            
                            string rawChallan = row[colChallan]?.ToString();
                            if (string.IsNullOrWhiteSpace(rawChallan)) { skippedNoChallan++; continue; } 

                            string challanNo = NormalizeChallan(rawChallan);
                            if (string.IsNullOrWhiteSpace(challanNo)) { skippedNoChallan++; continue; }

                             // Skip duplicates within the same file
                             if (processedChallansInFile.Contains(challanNo)) { duplicateRows++; continue; }
                            processedChallansInFile.Add(challanNo);

                            DateTime? dispatchDate = ParseExcelDate(row[colDate]);
                            double qty = ParseQuantity(row[colQty]);
                            string vehicleNo = colVehicle != -1 ? NormalizeVehicle(row[colVehicle]?.ToString()) : "";
                            string partyName = colParty != -1 ? row[colParty]?.ToString() : "";
                            string destination = colDest != -1 ? row[colDest]?.ToString() : "";
                            string exNo = colExNo != -1 ? row[colExNo]?.ToString() : "";
                            double unitPrice = colUnitPrice != -1 ? ParseQuantity(row[colUnitPrice]) : 0;

                            if (!dispatchDate.HasValue || qty <= 0)
                            {
                                skippedRows++;
                                errors.Add($"Row {i+1}: Invalid Date({row[colDate]}) or Qty({row[colQty]})");
                                continue;
                            }

                            var existing = _transportMgmtContext.TblDispatches.FirstOrDefault(d => d.ChallanNo == challanNo);
                            if (existing != null)
                            {
                                existing.DispatchDate = dispatchDate.Value;
                                existing.DispatchQuantity = qty;
                                existing.VehicleNo = vehicleNo;
                                existing.PartyName = partyName;
                                existing.Destination = destination;
                                existing.ExNo = exNo;
                                if (unitPrice > 0) existing.UnitPrice = unitPrice;
                                existing.DisVid = FID;
                                updatedRecords++;
                            }
                            else
                            {
                                newRecords.Add(new TblDispatch
                                {
                                    ChallanNo = challanNo,
                                    DispatchDate = dispatchDate.Value,
                                    DispatchQuantity = qty,
                                    VehicleNo = vehicleNo,
                                    PartyName = partyName,
                                    Destination = destination,
                                    ExNo = exNo,
                                    UnitPrice = unitPrice,
                                    DisVid = FID
                                });
                                successfulUploads++;
                            }
                        }

                        if (newRecords.Count > 0)
                        {
                            _transportMgmtContext.TblDispatches.AddRange(newRecords);
                        }
                        await _transportMgmtContext.SaveChangesAsync();

                         string msg = $"Success! Uploaded {successfulUploads} new records and updated {updatedRecords} existing ones.";
                         if (duplicateRows > 0) msg += $" Skipped {duplicateRows} duplicate rows.";
                         if (skippedRows > 0) msg += $" Skipped {skippedRows} rows with invalid Date/Qty.";
                         if (skippedNoChallan > 0) msg += $" Skipped {skippedNoChallan} rows with empty Challan column.";
                         TempData["Success"] = msg + " | Columns Mapped: " + mappingInfo;
                    }
                }
                
                if (errors.Count > 0)
                {
                    TempData["Error"] = "Some rows had issues: " + string.Join(" | ", errors.Take(3)) + (errors.Count > 3 ? "..." : "");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Critical Error: {ex.Message}";
            }

            return RedirectToAction("UploadDispatch");
        }

        private string NormalizeChallan(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "";
            
            // Handle Excel numeric format (e.g. "123.0")
            if (v.EndsWith(".0")) v = v.Substring(0, v.Length - 2);
            
            return v.Trim().TrimStart('0').ToUpper();
        }

        private string NormalizeVehicle(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "";
            // Remove spaces and special characters from vehicle number for better matching
            return v.Trim().Replace(" ", "").Replace("-", "").ToUpper();
        }

        private double ParseQuantity(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            if (v is double d) return d;
            
            string s = v.ToString().ToUpper();
            s = s.Replace("MT", "").Replace("TONS", "").Replace("TON", "").Replace(",", "").Trim();
            
            if (double.TryParse(s, out double res)) return res;
            return 0;
        }

        private DateTime? ParseExcelDate(object v)
        {
            if (v == null || v == DBNull.Value) return null;
            if (v is DateTime dt) return dt;
            
            if (v is double d) // Excel serial date
            {
                return DateTime.FromOADate(d);
            }

            string s = v.ToString().Trim();
            if (DateTime.TryParse(s, out DateTime res)) return res;
            
            return null;
        }


        public IActionResult ShowDispatch()
        {
            DispatchVm model = new DispatchVm();
            var vendors = _transportMgmtContext.TblFactories.ToList();
            model.ddlFactory = vendors.Select(a => new SelectListItem()
            {
                Text = a.FactoryName,
                Value = Convert.ToString(a.FID)
            }).ToList();

            model.dispatchVm = new List<DispatchViewModel>();

            return View("ShowDispatch", model);
        }




        [HttpPost]
        public IActionResult FilterDispatch(int factoryId, string challanNo)
        {
            var filteredData = _transportMgmtContext.TblDispatches
            .Where(d => d.DisVid == factoryId && d.ChallanNo == challanNo)
            .Select(a => new DispatchViewModel()
            {
                FID = a.DisVid,
                DispId = a.DispId,
                ChallanNo = a.ChallanNo,
                DispatchDate = Convert.ToDateTime(a.DispatchDate),
                DispatchQuantity = a.DispatchQuantity,
                VehicleNo = a.VehicleNo,
                Destination = a.Destination,
                PartyName = a.PartyName,
                Shortage = a.Shortage,
                ExNo = a.ExNo,
                IsReceived = a.IsReceived ?? false, // Ensure default value to prevent null issues
                FactoryName = _transportMgmtContext.TblFactories.FirstOrDefault(f => f.FID == a.DisVid).FactoryName,
            }).ToList();

            return PartialView("_DispatchTable", filteredData);
        }



        [HttpPost]
        public IActionResult UpdateShortageAndReceived(int dispId, int shortage, bool isReceived)
        {
            var dispatch = _transportMgmtContext.TblDispatches.FirstOrDefault(d => d.DispId == dispId);
            if (dispatch != null)
            {
                dispatch.Shortage = shortage;
                dispatch.IsReceived = isReceived;
                _transportMgmtContext.SaveChanges();

                return Json(new { success = true, isReceived = dispatch.IsReceived });
            }
            return Json(new { success = false });
        }


        [HttpPost]
        public IActionResult ExportReport(int ExportFactoryId, DateTime StartDate, DateTime EndDate, string reportType)
        {
            // Adjust dates to cover entire days
            var startDate = StartDate.Date;
            var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

            var data = _transportMgmtContext.TblDispatches
                .Where(d => d.DisVid == ExportFactoryId &&
                            d.DispatchDate >= startDate &&
                            d.DispatchDate <= endDate)
                .Select(d => new DispatchViewModel
                {
                    FactoryName = _transportMgmtContext.TblFactories.FirstOrDefault(f => f.FID == d.DisVid).FactoryName,
                    DispatchDate = Convert.ToDateTime(d.DispatchDate),
                    ChallanNo = d.ChallanNo,
                    VehicleNo = d.VehicleNo,
                    Destination = d.Destination,
                    PartyName = d.PartyName,
                    DispatchQuantity = d.DispatchQuantity,
                    Shortage = d.Shortage,
                    ExNo = d.ExNo,
                    IsReceived = d.IsReceived
                }).ToList();

            if (reportType == "Shortage")
            {
                data = data.Where(d => d.Shortage.HasValue && d.Shortage > 0).ToList();
            }
            else if (reportType == "Receipt")
            {
                data = data.Where(d => d.IsReceived == true).ToList();
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(reportType + " Report");

                // Headers
                worksheet.Cells[1, 1].Value = "Factory Name";
                worksheet.Cells[1, 2].Value = "Dispatch Date";
                worksheet.Cells[1, 3].Value = "Challan No";
                worksheet.Cells[1, 4].Value = "Vehicle No";
                worksheet.Cells[1, 5].Value = "Destination";
                worksheet.Cells[1, 6].Value = "Party Name";
                worksheet.Cells[1, 7].Value = "Dispatch Quantity";
                worksheet.Cells[1, 8].Value = "Ex. Number";
                worksheet.Cells[1, 9].Value = reportType == "Shortage" ? "Shortage" : "Is Received";

                // Data
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.FactoryName;
                    worksheet.Cells[row, 2].Value = item.DispatchDate.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 3].Value = item.ChallanNo;
                    worksheet.Cells[row, 4].Value = item.VehicleNo;
                    worksheet.Cells[row, 5].Value = item.Destination;
                    worksheet.Cells[row, 6].Value = item.PartyName;
                    worksheet.Cells[row, 7].Value = item.DispatchQuantity;
                    worksheet.Cells[row, 8].Value = item.ExNo;
                    worksheet.Cells[row, 9].Value = reportType == "Shortage" ? item.Shortage : (item.IsReceived == true ? "Yes" : "No");
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"{reportType}Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }








        // [HttpPost]
        //public IActionResult ExportReport(int ExportFactoryId, DateTime StartDate, DateTime EndDate, string reportType)
        //{
        //    // Adjust dates to cover entire days
        //    var startDate = StartDate.Date;
        //    var endDate = EndDate.Date.AddDays(1).AddTicks(-1);

        //    var data = _transportMgmtContext.TblDispatches
        //        .Where(d => d.DisVid == ExportFactoryId &&
        //                    d.DispatchDate >= startDate &&
        //                    d.DispatchDate <= endDate)
        //        .Select(d => new DispatchViewModel
        //        {
        //            FactoryName = _transportMgmtContext.TblFactories.FirstOrDefault(f => f.FID == d.DisVid).FactoryName,
        //            DispatchDate = Convert.ToDateTime(d.DispatchDate),
        //            ChallanNo = d.ChallanNo,
        //            VehicleNo = d.VehicleNo,
        //            Destination = d.Destination,
        //            PartyName = d.PartyName,
        //            DispatchQuantity = d.DispatchQuantity,
        //            Shortage = d.Shortage,
        //            IsReceived = d.IsReceived
        //        }).ToList();

        //    if (reportType == "Shortage")
        //    {
        //        data = data.Where(d => d.Shortage.HasValue && d.Shortage > 0).ToList();
        //    }
        //    else if (reportType == "Receipt")
        //    {
        //        data = data.Where(d => d.IsReceived == true).ToList();
        //    }

        //    using (var package = new ExcelPackage())
        //    {
        //        var worksheet = package.Workbook.Worksheets.Add(reportType + " Report");

        //        // Headers
        //        worksheet.Cells[1, 1].Value = "Factory Name";
        //        worksheet.Cells[1, 2].Value = "Dispatch Date";
        //        worksheet.Cells[1, 3].Value = "Challan No";
        //        worksheet.Cells[1, 4].Value = "Vehicle No";
        //        worksheet.Cells[1, 5].Value = "Destination";
        //        worksheet.Cells[1, 6].Value = "Party Name";
        //        worksheet.Cells[1, 7].Value = "Dispatch Quantity";
        //        worksheet.Cells[1, 8].Value = reportType == "Shortage" ? "Shortage" : "Is Received";

        //        // Data
        //        int row = 2;
        //        foreach (var item in data)
        //        {
        //            worksheet.Cells[row, 1].Value = item.FactoryName;
        //            worksheet.Cells[row, 2].Value = item.DispatchDate.ToString("dd/MM/yyyy");
        //            worksheet.Cells[row, 3].Value = item.ChallanNo;
        //            worksheet.Cells[row, 4].Value = item.VehicleNo;
        //            worksheet.Cells[row, 5].Value = item.Destination;
        //            worksheet.Cells[row, 6].Value = item.PartyName;
        //            worksheet.Cells[row, 7].Value = item.DispatchQuantity;
        //            worksheet.Cells[row, 8].Value = reportType == "Shortage" ? item.Shortage : ((bool)item.IsReceived ? "Yes" : "No");
        //            row++;
        //        }

        //        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //        var stream = new MemoryStream();
        //        package.SaveAs(stream);
        //        stream.Position = 0;

        //        string fileName = $"{reportType}Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        //        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //    }

        //}

    }
}



    


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
using System.Text.RegularExpressions;

namespace KhanLogistics.Controllers
{
    public class DispatchController : Controller
    {
        TransportMgmtContext _transportMgmtContext;
        IConfiguration _configuration;
        IExcelDataReader _excelDataReader;
        ILogger<DispatchController> _logger; // Inject Logger
       

        public DispatchController(TransportMgmtContext context, ILogger<DispatchController> logger)
        {
            this._transportMgmtContext = context;
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
                    return RedirectToAction("DispatchDetails", "Report");
                }

                if (FID <= 0)
                {
                    TempData["Error"] = "Please select a factory.";
                    return RedirectToAction("DispatchDetails", "Report");
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                var factoryObj = await _transportMgmtContext.TblFactories.FirstOrDefaultAsync(f => f.FID == FID);
                string currentFactoryName = factoryObj?.FactoryName?.ToUpper() ?? "";

                using (var stream = file.OpenReadStream())
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
                            return RedirectToAction("DispatchDetails", "Report");
                        }

                        DataTable dt = result.Tables[0];
                        
                        // --- DYNAMIC COLUMN MAPPING ---
                        int colChallan = -1, colDate = -1, colQty = -1, colVehicle = -1, colParty = -1, colDest = -1, colExNo = -1, colUnitPrice = -1;
                        int headerRowIndex = -1;
                        Dictionary<string, string> mappedCols = new Dictionary<string, string>();

                        // Keywords matching the logic
                        string[] keywords = { "CHALLAN", "LR", "VEHICLE", "TRUCK", "DISPATCH", "SOLD", "QTY", "QUANTITY", "WT", "DEST", "DELIVERY", "INV", "BILL", "SHIPMENT", "ROUTE", "CITY", "EXNO" };

                        // Scan first 50 rows for header
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
                                    string h = rawH.ToUpper().Replace(" ", "").Replace(".", "").Replace("-", "").Replace("_", "").Replace("#", "").Replace("\n", "").Replace("\r", "");
                                    if (string.IsNullOrEmpty(h)) continue;

                                    // --- CHALLAN PRIORITY ---
                                    bool isStrongChallan = h == "CHALLAN" || h == "CHALLANNO" || h == "DELIVERYNO" || h == "DINO" || h == "LRNO" || h == "SHIPMENTNO" || h == "INVOICENO" || h == "INTERNALNO";
                                    bool isWeakChallan = h.Contains("CHALLAN") || h.Contains("DELIVERY") || h == "DI" || h.Contains("LR") || h.Contains("SHIPMENT") || h.Contains("INV") || h.Contains("BILL") || h.Contains("DOC") || h.Contains("GR") || h.Contains("DC") || h.Contains("DN") || h.Contains("DO") || h.Contains("INTERNAL");

                                    if (isWeakChallan && !h.Contains("DATE")) 
                                    { 
                                        if (colChallan == -1 || isStrongChallan)
                                        {
                                            colChallan = c; 
                                            mappedCols["Challan"] = $"Challan='{rawH}'(Col {c+1})";
                                        }
                                    }
                                    
                                    // --- VEHICLE ---
                                    if ((h.Contains("VEHICLE") || h.Contains("TRUCK") || h.Contains("LORRY") || h.Contains("REGO")) && colVehicle == -1) 
                                    { 
                                        colVehicle = c; mappedCols["Vehicle"] = $"Vehicle='{rawH}'(Col {c+1})"; 
                                    }
                                    
                                    // --- DATE ---
                                    if ((h.Contains("DATE") || h == "DT") && !h.Contains("NAME") && !h.Contains("PARTY")) 
                                    { 
                                        bool isBetter = colDate == -1;
                                        if (!isBetter && r + 1 < dt.Rows.Count)
                                        {
                                            string firstVal = dt.Rows[r + 1][c]?.ToString() ?? "";
                                            if (firstVal.Contains(".") || firstVal.Contains("/") || firstVal.Contains("-")) isBetter = true;
                                        }

                                        if (isBetter)
                                        {
                                            colDate = c; 
                                            mappedCols["Date"] = $"Date='{rawH}'(Col {c+1})"; 
                                        }
                                    }
                                    else if (h.Contains("DISPATCH") && colDate == -1)
                                    {
                                        colDate = c; mappedCols["Date"] = $"Date='{rawH}'(Col {c+1})";
                                    }
                                    
                                    // --- QTY (Check before Vehicle to avoid 'Truck Qty' issues) ---
                                    if ((h.Contains("QTY") || h.Contains("QUANTITY") || h == "WT" || h.Contains("WEIGHT") || (h.Contains("MT") && !h.Contains("SHIPMENT"))) && colQty == -1) 
                                    { 
                                        colQty = c; mappedCols["Qty"] = $"Qty='{rawH}'(Col {c+1})"; 
                                    }
                                    // --- VEHICLE ---
                                    else if ((h.Contains("VEHICLE") || h.Contains("TRUCK") || h.Contains("LORRY") || h.Contains("REGO")) && colVehicle == -1) 
                                    { 
                                        colVehicle = c; mappedCols["Vehicle"] = $"Vehicle='{rawH}'(Col {c+1})"; 
                                    }

                                    // --- EX NUMBER (Universal) ---
                                    if ((h.Contains("EXNUMBER") || h.Contains("EXNO") || h.Contains("EXTRANUMBER")) && colExNo == -1)
                                    {
                                        colExNo = c; mappedCols["ExNo"] = $"ExNo='{rawH}'(Col {c+1})";
                                    }

                                    // --- PARTY ---
                                    if ((h.Contains("PARTY") || h.Contains("CUSTOMER") || h.Contains("CONSIGNEE") || h.Contains("SOLDTOPARTY") || h.Contains("SOLD")) && colParty == -1 && !h.Contains("DATE"))
                                    {
                                        colParty = c; mappedCols["Party"] = $"Party='{rawH}'(Col {c+1})";
                                    }

                                    // --- DESTINATION ---
                                    bool isDestMatch = false;
                                    bool isExactDest = false;
                                    string normFactory = currentFactoryName.Replace(" ", "");

                                    if (normFactory.Contains("ULTRATECH"))
                                    {
                                        if (h.Contains("CITYCODEDESCRIPTION")) { isDestMatch = true; isExactDest = true; }
                                        else if (colDest == -1 && (h.Contains("DESTINATION") || (h.Contains("CITY") && h != "CITYCODE") || h.Contains("TOCITY"))) { isDestMatch = true; }
                                    }
                                    else if (normFactory.Contains("MANIGAR") || normFactory.Contains("MANIKGARH"))
                                    {
                                        if (h.Contains("ROUTENAME")) { isDestMatch = true; isExactDest = true; }
                                        else if (colDest == -1 && (h.Contains("DESTINATION") || (h.Contains("CITY") && h != "CITYCODE"))) { isDestMatch = true; }
                                    }
                                    else if (normFactory.Contains("JSW"))
                                    {
                                        if (h == "DESTINATION") { isDestMatch = true; isExactDest = true; }
                                        else if (colDest == -1 && h.Contains("DEST")) { isDestMatch = true; }
                                    }
                                    else
                                    {
                                        if (h.Contains("CITYCODEDESCRIPTION")) { isDestMatch = true; isExactDest = true; }
                                        else if ((h.Contains("DESTINATION") || (h.Contains("CITY") && h != "CITYCODE") || h.Contains("TOCITY") || h.Contains("PLANT") || h.Contains("DEST") || h.Contains("DELIVERY")) && !h.Contains("DELIVERYNO"))
                                        {
                                            isDestMatch = true;
                                        }
                                    }

                                    if (isDestMatch && (colDest == -1 || isExactDest))
                                    {
                                        colDest = c; mappedCols["Dest"] = $"Dest='{rawH}'(Col {c+1})";
                                    }

                                    // --- UNIT PRICE ---
                                    if ((h.Contains("UNITPRICE") || h.Contains("RATE")) && colUnitPrice == -1)
                                    {
                                        colUnitPrice = c; mappedCols["Price"] = $"Price='{rawH}'(Col {c+1})";
                                    }
                                }

                                // --- FACTORY SPECIFIC OVERRIDES ---
                                for (int c = 0; c < dt.Columns.Count; c++)
                                {
                                    string hFix = (dt.Rows[headerRowIndex][c]?.ToString() ?? "").ToUpper().Replace(" ", "").Replace(".", "").Replace("-", "").Replace("_", "").Replace("#", "").Replace("\n", "").Replace("\r", "");
                                    
                                    if (currentFactoryName.Contains("MANIGAR") || currentFactoryName.Contains("MANIKGARH"))
                                    {
                                        if (hFix == "DELIVERYNO" || hFix == "CHALLANNO") { colChallan = c; mappedCols["Challan"] = $"Challan='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("CHALLAN") || hFix.Contains("DELIVERY") || hFix == "DINO") { if (colChallan == -1) { colChallan = c; mappedCols["Challan"] = $"Challan='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; } }
                                        else if (hFix.Contains("QTY") || hFix.Contains("QUANTITY")) { colQty = c; mappedCols["Qty"] = $"Qty='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("TRUCK") || hFix.Contains("VEHICLE")) { colVehicle = c; mappedCols["Vehicle"] = $"Vehicle='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("EXNUMBER") || hFix.Contains("EXNO")) { colExNo = c; mappedCols["ExNo"] = $"ExNo='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                    }
                                    else if (currentFactoryName.Contains("ULTRATECH"))
                                    {
                                        if (hFix == "DELIVERYNO" || hFix == "SHIPMENTNO") { colChallan = c; mappedCols["Challan"] = $"Challan='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("DELIVERY") || hFix.Contains("CHALLAN") || hFix.Contains("SHIPMENT")) { if (colChallan == -1) { colChallan = c; mappedCols["Challan"] = $"Challan='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; } }
                                        else if (hFix.Contains("QTY") || hFix.Contains("QUANTITY")) { colQty = c; mappedCols["Qty"] = $"Qty='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("TRUCK") || hFix.Contains("VEHICLE")) { colVehicle = c; mappedCols["Vehicle"] = $"Vehicle='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("EXNUMBER") || hFix.Contains("EXNO")) { colExNo = c; mappedCols["ExNo"] = $"ExNo='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                    }
                                    else if (currentFactoryName.Contains("JSW"))
                                    {
                                        if (hFix == "INTERNALNO" || hFix == "DELIVERYNO") { colChallan = c; mappedCols["Challan"] = $"Challan='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("INTERNAL") || hFix.Contains("DELIVERY") || hFix.Contains("CHALLAN")) { if (colChallan == -1) { colChallan = c; mappedCols["Challan"] = $"Challan='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; } }
                                        else if (hFix.Contains("QTY") || hFix.Contains("QUANTITY")) { colQty = c; mappedCols["Qty"] = $"Qty='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("VEHICLE") || hFix.Contains("TRUCK")) { colVehicle = c; mappedCols["Vehicle"] = $"Vehicle='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                        else if (hFix.Contains("EXNUMBER") || hFix.Contains("EXNO")) { colExNo = c; mappedCols["ExNo"] = $"ExNo='{dt.Rows[headerRowIndex][c]}'(Col {c+1})"; }
                                    }
                                }
                                break;
                            }
                        }

                        // --- VALIDATE CHALLAN COLUMN ---
                        if (colChallan != -1 && headerRowIndex + 1 < dt.Rows.Count)
                        {
                            int checkUpTo = Math.Min(5, dt.Rows.Count - headerRowIndex - 1);
                            bool challanHasData = Enumerable.Range(headerRowIndex + 1, checkUpTo)
                                .Any(r => !string.IsNullOrWhiteSpace(dt.Rows[r][colChallan]?.ToString()));

                            if (!challanHasData)
                            {
                                for (int c = 0; c < dt.Columns.Count; c++)
                                {
                                    if (c == colChallan || c == colDate || c == colQty || c == colVehicle || c == colParty || c == colDest) continue;
                                    string hFb = (dt.Rows[headerRowIndex][c]?.ToString() ?? "").ToUpper().Replace(" ", "").Replace(".", "").Replace("-", "").Replace("_", "").Replace("#", "").Replace("\n", "").Replace("\r", "");
                                    if (string.IsNullOrEmpty(hFb)) continue;

                                    if ((hFb.Contains("DELIVERY") || hFb.Contains("CHALLAN") || hFb.Contains("INVOICE") || hFb.Contains("SHIPMENT") || hFb.Contains("BILLDOC")) && !hFb.Contains("DATE"))
                                    {
                                        if (Enumerable.Range(headerRowIndex + 1, checkUpTo).Any(r => !string.IsNullOrWhiteSpace(dt.Rows[r][c]?.ToString())))
                                        {
                                            colChallan = c;
                                            mappedCols["ChallanFix"] = $"Challan(AutoFixed)='{dt.Rows[headerRowIndex][c]}'(Col {c + 1})";
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (colChallan == -1 || colDate == -1 || colQty == -1)
                        {
                            string allDetected = "";
                            for (int c = 0; c < dt.Columns.Count; c++) {
                                string val = dt.Rows[headerRowIndex][c]?.ToString() ?? "EMPTY";
                                if (!string.IsNullOrWhiteSpace(val)) allDetected += $"Col {c+1}: '{val}', ";
                            }
                            TempData["Error"] = "Could not map required columns. Found: " + string.Join(", ", mappedCols.Values) + " | All Headers: " + allDetected;
                            return RedirectToAction("DispatchDetails", "Report");
                        }

                        int skippedNoChallan = 0;
                        List<TblDispatch> newRecords = new List<TblDispatch>();
                        HashSet<string> processedChallansInFile = new HashSet<string>();
                        string mappingInfo = string.Join(", ", mappedCols.Values);


                        for (int i = headerRowIndex + 1; i < dt.Rows.Count; i++)
                        {
                            DataRow row = dt.Rows[i];
                            
                            string rawChallan = row[colChallan]?.ToString();
                            string challanNo = NormalizeChallan(rawChallan);
                            string exNo = colExNo != -1 ? row[colExNo]?.ToString() : "";

                            // --- AUTO FIX LOGIC for MANIGAR ---
                            if (currentFactoryName.Replace(" ", "").Contains("MANIGAR") || currentFactoryName.Replace(" ", "").Contains("MANIKGARH"))
                            {
                                // 1. If wrong challan is 89-series, rescue it to ExNo
                                if (challanNo.StartsWith("89") && string.IsNullOrWhiteSpace(exNo))
                                {
                                    exNo = challanNo;
                                }

                                // 2. If we don't have a valid 69-series challan, search the row for it
                                if (!challanNo.StartsWith("69"))
                                {
                                    bool foundRealChallan = false;
                                    for (int c = 0; c < dt.Columns.Count; c++)
                                    {
                                        string cellVal = NormalizeChallan(row[c]?.ToString());
                                        if (cellVal.StartsWith("69"))
                                        {
                                            challanNo = cellVal;
                                            foundRealChallan = true;
                                            break;
                                        }
                                        else if (cellVal.StartsWith("89") && string.IsNullOrWhiteSpace(exNo))
                                        {
                                            exNo = cellVal; // Catch any stray 89-series as ExNo too
                                        }
                                    }

                                    // 3. If STILL no valid challan found, we MUST skip (cannot insert without primary key)
                                    if (!foundRealChallan)
                                    {
                                        skippedRows++;
                                        errors.Add($"Row {i+1}: Skipped - No valid 69-series Challan found. Wrong mapping was: {rawChallan}");
                                        continue;
                                    }
                                }
                            }

                            if (string.IsNullOrWhiteSpace(challanNo)) { skippedNoChallan++; continue; }

                             // Skip duplicates within the same file
                             if (processedChallansInFile.Contains(challanNo)) { duplicateRows++; continue; }
                            processedChallansInFile.Add(challanNo);

                            DateTime? dispatchDate = ParseExcelDate(row[colDate]);
                            double qty = ParseQuantity(row[colQty]);
                            string vehicleNo = colVehicle != -1 ? NormalizeVehicle(row[colVehicle]?.ToString()) : "";
                            string partyName = colParty != -1 ? row[colParty]?.ToString() : "";
                            string destination = colDest != -1 ? row[colDest]?.ToString() : "";
                            double unitPrice = colUnitPrice != -1 ? ParseQuantity(row[colUnitPrice]) : 0;

                            // --- VALIDATION ---
                            // 1. Qty must be numeric and positive (ParseQuantity already handles decimal conversion)
                            if (qty <= 0)
                            {
                                skippedRows++;
                                errors.Add($"Row {i + 1}: Invalid Qty ({row[colQty]}) - Must be a number.");
                                continue;
                            }

                            // 2. Vehicle No should be alphanumeric
                            if (!string.IsNullOrWhiteSpace(vehicleNo) && !Regex.IsMatch(vehicleNo, "^[A-Z0-9]+$"))
                            {
                                skippedRows++;
                                errors.Add($"Row {i + 1}: Invalid Vehicle No ({row[colVehicle]}) - Must be alphanumeric.");
                                continue;
                            }

                            if (!dispatchDate.HasValue)
                            {
                                skippedRows++;
                                errors.Add($"Row {i+1}: Invalid Date({row[colDate]})");
                                continue;
                            }

                            if (dispatchDate.Value > DateTime.Now)
                            {
                                skippedRows++;
                                errors.Add($"Row {i+1}: Future date detected ({dispatchDate.Value:dd-MM-yyyy}). Only current or past dates are allowed.");
                                continue;
                            }

                            var existing = _transportMgmtContext.TblDispatches.FirstOrDefault(d => d.ChallanNo == challanNo);
                            if (existing != null)
                            {
                                bool isUpdated = false;

                                if (!string.IsNullOrWhiteSpace(vehicleNo) && existing.VehicleNo != vehicleNo)
                                {
                                    existing.VehicleNo = vehicleNo;
                                    isUpdated = true;
                                }
                                if (!string.IsNullOrWhiteSpace(partyName) && existing.PartyName != partyName)
                                {
                                    existing.PartyName = partyName;
                                    isUpdated = true;
                                }
                                if (!string.IsNullOrWhiteSpace(destination) && existing.Destination != destination)
                                {
                                    existing.Destination = destination;
                                    isUpdated = true;
                                }
                                if (!string.IsNullOrWhiteSpace(exNo) && existing.ExNo != exNo)
                                {
                                    existing.ExNo = exNo;
                                    isUpdated = true;
                                }
                                if (unitPrice > 0 && existing.UnitPrice != unitPrice)
                                {
                                    existing.UnitPrice = unitPrice;
                                    isUpdated = true;
                                }
                                if (qty > 0 && existing.DispatchQuantity != qty)
                                {
                                    existing.DispatchQuantity = qty;
                                    isUpdated = true;
                                }
                                if (dispatchDate.HasValue && existing.DispatchDate != dispatchDate.Value)
                                {
                                    existing.DispatchDate = dispatchDate.Value;
                                    isUpdated = true;
                                }

                                if (isUpdated)
                                {
                                    existing.DisVid = FID; // optionally update the factory if we are updating a blank record
                                    updatedRecords++;
                                }
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

            return RedirectToAction("DispatchDetails", "Report");
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
            
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res)) return res;
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
            if (string.IsNullOrWhiteSpace(s)) return null;

            // Priority 1: Exact Indian/UK formats (dots, dashes, slashes)
            string[] formats = { 
                "dd.MM.yyyy", "dd.MM.yy", 
                "dd-MM-yyyy", "dd-MM-yy", 
                "dd/MM/yyyy", "dd/MM/yy", 
                "d.M.yyyy", "d.M.yy",
                "dd-MMM-yy", "dd-MMM-yyyy",
                "yyyy-MM-dd"
            };
            
            if (DateTime.TryParseExact(s, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime exactRes)) 
            {
                return exactRes;
            }

            // Priority 2: General parsing with Indian culture
            if (DateTime.TryParse(s, System.Globalization.CultureInfo.GetCultureInfo("en-IN"), System.Globalization.DateTimeStyles.None, out DateTime res)) 
            {
                return res;
            }
            
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
                Destination = string.IsNullOrEmpty(a.Destination) ? a.PartyName : a.Destination,
                PartyName = a.PartyName,
                Shortage = a.Shortage,
                ExNo = a.ExNo,
                IsReceived = a.IsReceived ?? false, // Ensure default value to prevent null issues
                FactoryName = _transportMgmtContext.TblFactories.Where(f => f.FID == a.DisVid).Select(f => f.FactoryName).FirstOrDefault() ?? "Unknown",
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
                    FactoryName = _transportMgmtContext.TblFactories.Where(f => f.FID == d.DisVid).Select(f => f.FactoryName).FirstOrDefault() ?? "Unknown",
                    DispatchDate = Convert.ToDateTime(d.DispatchDate),
                    ChallanNo = d.ChallanNo,
                    VehicleNo = d.VehicleNo,
                    Destination = string.IsNullOrEmpty(d.Destination) ? d.PartyName : d.Destination,
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
                worksheet.Cells[1, 6].Value = "Dispatch Quantity";
                worksheet.Cells[1, 7].Value = "Ex. Number";
                worksheet.Cells[1, 8].Value = reportType == "Shortage" ? "Shortage" : "Is Received";

                // Data
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.FactoryName;
                    worksheet.Cells[row, 2].Value = item.DispatchDate.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 3].Value = item.ChallanNo;
                    worksheet.Cells[row, 4].Value = item.VehicleNo;
                    worksheet.Cells[row, 5].Value = item.Destination;
                    worksheet.Cells[row, 6].Value = item.DispatchQuantity;
                    worksheet.Cells[row, 7].Value = item.ExNo;
                    worksheet.Cells[row, 8].Value = reportType == "Shortage" ? item.Shortage : (item.IsReceived == true ? "Yes" : "No");
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



    


using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using ExcelDataReader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Data;
using Newtonsoft.Json;

namespace KhanLogistics.Controllers
{
    public class GstController : Controller
    {
        private readonly TransportMgmtContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GstController(IWebHostEnvironment webHostEnvironment, TransportMgmtContext context)
        {
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public IActionResult UploadGst()
        {
            var vm = new DispatchVm
            {
                ddlFactory = _context.TblFactories
                    .Select(f => new SelectListItem
                    {
                        Value = f.FID.ToString(),
                        Text = f.FactoryName
                    })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UploadGst(IFormFile? file, int selectedFactoryId, bool isConfirmed = false)
        {
            try
            {
                if (!isConfirmed && (file == null || file.Length == 0))
                {
                    TempData["ErrorMessage"] = "Please select a file.";
                    return RedirectToAction("UploadGst");
                }

                string filePath = "";
                if (file != null)
                {
                    string dirPath = Path.Combine(_webHostEnvironment.WebRootPath, "files");
                    Directory.CreateDirectory(dirPath);
                    filePath = Path.Combine(dirPath, "gst_temp_" + file.FileName);
                    using (var uploadStream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(uploadStream);
                }
                else if (isConfirmed)
                {
                    // If confirmed, retrieve the last uploaded file path from TempData or use a standard name
                    string dirPath = Path.Combine(_webHostEnvironment.WebRootPath, "files");
                    var files = Directory.GetFiles(dirPath, "gst_temp_*").OrderByDescending(f => System.IO.File.GetCreationTime(f)).ToList();
                    if (files.Any()) filePath = files.First();
                    else {
                        TempData["ErrorMessage"] = "Session expired or file missing. Please upload again.";
                        return RedirectToAction("UploadGst");
                    }
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                List<GstUpdatePreview> previews = new List<GstUpdatePreview>();
                int successCount = 0;
                int failureCount = 0;
                var failedRecords = new List<string>();

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet();
                    if (dataSet == null || dataSet.Tables.Count == 0) return BadRequest("No data found.");

                    DataTable table = dataSet.Tables[0];
                    for (int i = 1; i < table.Rows.Count; i++)
                    {
                        DataRow row = table.Rows[i];
                        if (row.ItemArray.All(v => v == null || string.IsNullOrWhiteSpace(v?.ToString()))) continue;

                        if (!double.TryParse(row[0]?.ToString(), out double excelGstAmt)) continue;
                        // Use Round to ignore decimals as per user request
                        int gstMatch = (int)Math.Round(excelGstAmt, MidpointRounding.AwayFromZero);
                        var billDateExcel = ParseDate(row[1]);
                        var gstUpdateDate = ParseDate(row[2]);

                        if (billDateExcel == null || gstUpdateDate == null) continue;

                        // Get candidates with matching factory and rounded GST amount (using range to avoid Math.Round translation issues)
                        var candidates = await _context.BillTables
                            .Where(b => b.FID == selectedFactoryId && 
                                        b.Gst.HasValue && 
                                        b.Gst >= (double)gstMatch - 0.5 && 
                                        b.Gst < (double)gstMatch + 0.5)
                            .ToListAsync();

                        var targetDate = billDateExcel.Value.Date;
                        var date1M = targetDate.AddMonths(-1);
                        var date2M = targetDate.AddMonths(-2);
                        var date3M = targetDate.AddMonths(-3);

                        // Priority: Exact Match > 1 Month Back > 2 Months Back > 3 Months Back
                        var exactMatch = candidates.FirstOrDefault(b => b.BillDate.HasValue && b.BillDate.Value.Date == targetDate);
                        var driftMatch1 = candidates.FirstOrDefault(b => b.BillDate.HasValue && b.BillDate.Value.Month == date1M.Month && b.BillDate.Value.Year == date1M.Year);
                        var driftMatch2 = candidates.FirstOrDefault(b => b.BillDate.HasValue && b.BillDate.Value.Month == date2M.Month && b.BillDate.Value.Year == date2M.Year);
                        var driftMatch3 = candidates.FirstOrDefault(b => b.BillDate.HasValue && b.BillDate.Value.Month == date3M.Month && b.BillDate.Value.Year == date3M.Year);

                        BillTable? bestMatch = exactMatch ?? driftMatch1 ?? driftMatch2 ?? driftMatch3;
                        bool isDrift = bestMatch != null && bestMatch.BillDate!.Value.Date != targetDate;

                        if (bestMatch != null)
                        {
                            if (!isConfirmed && isDrift)
                            {
                                previews.Add(new GstUpdatePreview { 
                                    BillNum = bestMatch.BillNum ?? "N/A", 
                                    ExcelDate = targetDate, 
                                    ActualDate = bestMatch.BillDate!.Value,
                                    GstAmt = gstMatch
                                });
                            }
                            else if (isConfirmed || !isDrift)
                            {
                                bestMatch.GstDate = gstUpdateDate;
                                _context.Entry(bestMatch).Property(b => b.GstDate).IsModified = true;
                                successCount++;
                            }
                        }
                        else
                        {
                            failureCount++;
                            failedRecords.Add($"Row {i+1}: No match for Amt:{gstMatch}, Ref Date:{targetDate:dd/MM/yyyy}");
                        }
                    }
                }

                if (!isConfirmed && previews.Any())
                {
                    TempData["DriftPreviews"] = JsonConvert.SerializeObject(previews);
                    TempData["SelectedFactoryId"] = selectedFactoryId;
                    return RedirectToAction("UploadGst");
                }

                if (successCount > 0) await _context.SaveChangesAsync();

                TempData["SuccessCount"] = successCount;
                TempData["FailureCount"] = failureCount;
                TempData["FailedRecords"] = failedRecords.Count > 0 ? string.Join(" | ", failedRecords.Take(5)) : "";
                
                // Clean up temp files
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("UploadGst");
        }

        private DateTime? ParseDate(object? value)
        {
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return dt;
            string s = value.ToString()?.Trim() ?? "";
            if (double.TryParse(s, out double d) && d > 1000) try { return DateTime.FromOADate(d); } catch { }
            
            // Prioritize ISO formats (yyyy-MM-dd) as per user request
            string[] fms = { "yyyy-MM-dd", "yy-MM-dd", "dd/MM/yyyy", "d/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy" };
            if (DateTime.TryParseExact(s, fms, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime p)) return p;
            return DateTime.TryParse(s, out DateTime f) ? f : null;
        }
    }

    public class GstUpdatePreview {
        public string BillNum { get; set; } = "";
        public DateTime ExcelDate { get; set; }
        public DateTime ActualDate { get; set; }
        public double GstAmt { get; set; }
    }
}
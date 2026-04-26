using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using ExcelDataReader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Data;

namespace KhanLogistics.Controllers
{
    public class GstController : Controller
    {
        private readonly TransportMgmtContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor updated to initialize _webHostEnvironment
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
                    .Select(f => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = f.FID.ToString(),
                        Text = f.FactoryName
                    })
                    .ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UploadGst(IFormFile file, int selectedFactoryId)
        {
            int successCount = 0;
            int failureCount = 0;
            var successfulRecords = new List<string>();
            var failedRecords = new List<string>();

            try
            {
                if (_webHostEnvironment == null)
                    throw new InvalidOperationException("WebHostEnvironment is not configured.");

                // Save uploaded file to wwwroot/files
                string dirPath = Path.Combine(_webHostEnvironment.WebRootPath, "files");
                Directory.CreateDirectory(dirPath);
                string fileName = Path.GetFileName(file.FileName);
                string filePath = Path.Combine(dirPath, fileName);

                using (var uploadStream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(uploadStream);

                // Enable ExcelDataReader codepages
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var dataSet = reader.AsDataSet();
                    if (dataSet != null && dataSet.Tables.Count > 0)
                    {
                        foreach (DataTable table in dataSet.Tables)
                        {
                            foreach (DataRow row in table.Rows)
                            {
                                try
                                {
                                    // 1) GST Amount
                                    if (!decimal.TryParse(row[0]?.ToString(), out decimal excelGstAmount))
                                    {
                                        failedRecords.Add($"Invalid GST Amount format: {row[0]}");
                                        failureCount++;
                                        continue;
                                    }
                                    int gstAmountInt = (int)Math.Truncate(excelGstAmount);

                                    // 2) Bill Date
                                    if (!DateTime.TryParse(row[1]?.ToString(), out DateTime billDate))
                                    {
                                        failedRecords.Add($"Invalid Bill Date: {row[1]}");
                                        failureCount++;
                                        continue;
                                    }

                                    // 3) GST Update Date
                                    if (!DateTime.TryParse(row[2]?.ToString(), out DateTime gstUpdateDate))
                                    {
                                        failedRecords.Add($"Invalid GST Update Date: {row[2]}");
                                        failureCount++;
                                        continue;
                                    }

                                    // Find matching record by factory, truncated GST, and exact bill date
                                    var billToUpdate = await _context.BillTables
                                        .Where(b =>
                                            b.FID == selectedFactoryId &&
                                            b.Gst.HasValue &&
                                            (int)Math.Truncate((double)b.Gst.Value) == gstAmountInt &&
                                            b.BillDate.HasValue &&
                                            b.BillDate.Value.Date == billDate.Date)
                                        .FirstOrDefaultAsync();

                                    if (billToUpdate != null)
                                    {
                                        billToUpdate.GstDate = gstUpdateDate;
                                        _context.Entry(billToUpdate).Property(b => b.GstDate).IsModified = true;
                                        await _context.SaveChangesAsync();

                                        successfulRecords.Add($"Updated Bill ID: {billToUpdate.BillID}");
                                        successCount++;
                                    }
                                    else
                                    {
                                        failedRecords.Add(
                                            $"No matching bill for GST {gstAmountInt} on {billDate:yyyy-MM-dd}");
                                        failureCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    failedRecords.Add($"Row error: {ex.Message}");
                                    failureCount++;
                                }
                            }
                        }
                    }
                }

                TempData["SuccessCount"] = successCount;
                TempData["FailureCount"] = failureCount;
                TempData["SuccessfulRecords"] = successfulRecords;
                TempData["FailedRecords"] = failedRecords;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("UploadGst");
        }





    }
}
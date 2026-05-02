using KhanLogistics.Models.ViewModel;
using KhanLogistics.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection.Metadata;

namespace KhanLogistics.Controllers
{
    public class PaymentController : Controller
    {
        TransportMgmtContext _transportMgmtContext;
        IConfiguration _configuration;
        IWebHostEnvironment _hostingEnvironment;
        IExcelDataReader _excelDataReader;

        public PaymentController(TransportMgmtContext context, IWebHostEnvironment webHostEnvironment)
        {
            this._transportMgmtContext = context;
            this._hostingEnvironment = webHostEnvironment;
        }

        public IActionResult ShowBill()
        {
            PaymentVm model = new PaymentVm();
            var vendors = _transportMgmtContext.TblFactories.ToList();
            model.FactoryList = _transportMgmtContext.TblFactories.ToList().Select(a => new SelectListItem()
            {
                Text = a.FactoryName,
                Value = Convert.ToString(a.FID)
            }).ToList();
            model.payments = _transportMgmtContext.BillTables.ToList().Select(a => new PaymentViewModel()
            {
                FID = Convert.ToInt32(a.FID),
                BillId = Convert.ToInt32(a.BillID),
                BillDate = Convert.ToDateTime(a.BillDate),
                BillNum = Convert.ToString(a.BillNum),
                PaymentReceived = Convert.ToDouble(a.PaymentReceived),
                FactoryName = vendors.FirstOrDefault(f => f.FID == a.FID)?.FactoryName,

            }).AsEnumerable();

            return View("ShowBill", model);
        }

        [HttpPost]
        public async Task<IActionResult> ShowBill(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Please select a file.");
                }

                List<BillTable> updatedBills = new List<BillTable>();
                Dictionary<string, PaymentTable> paymentDictionary = new Dictionary<string, PaymentTable>();

                string dirPath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

                string fileName = Path.GetFileName(file.FileName);
                string filePath = Path.Combine(dirPath, fileName);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
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

                        // --- DYNAMIC HEADER ROW DETECTION ---
                        int headerRowIndex = 0;
                        for (int r = 0; r < Math.Min(dataTable.Rows.Count, 5); r++)
                        {
                            bool foundHeader = false;
                            for (int c = 0; c < dataTable.Columns.Count; c++)
                            {
                                string cellValue = dataTable.Rows[r][c]?.ToString()?.ToLower() ?? "";
                                if (cellValue.Contains("bill") || cellValue.Contains("invoice") || cellValue.Contains("amount") || cellValue.Contains("received"))
                                {
                                    foundHeader = true;
                                    break;
                                }
                            }
                            if (foundHeader)
                            {
                                headerRowIndex = r;
                                break;
                            }
                        }

                        DataRow headerRow = dataTable.Rows[headerRowIndex];
                        int colBillNum = -1, colDocNum = -1, colDate = -1, colActualAmt = -1;
                        int colTds = -1, colGst = -1, colPaidAmt = -1, colShortage = -1;

                        for (int c = 0; c < dataTable.Columns.Count; c++)
                        {
                            string header = headerRow[c]?.ToString()?.Trim().ToLower() ?? "";
                            if (header == "bill" || header == "bill no" || header == "bill no." || header == "invoice" || header.Contains("bill num")) colBillNum = c;
                            else if (header.Contains("doc") || header.Contains("payment num") || header.Contains("utr") || header.Contains("ref") || header == "page") colDocNum = c;
                            else if (header.Contains("date") || header.Contains("value") || header.Contains("posting") || header.Contains("voucher") || header.Contains("receive date")) colDate = c;
                            else if (header == "amount" || header.Contains("actual") || header.Contains("gross") || (header.Contains("amount") && !header.Contains("paid") && !header.Contains("net") && !header.Contains("received") && !header.Contains("payment") && !header.Contains("r.amount") && !header.Contains("rec"))) colActualAmt = c;
                            else if (header.Contains("tds")) colTds = c;
                            else if (header.Contains("gst")) colGst = c;
                            else if (header == "received" || header == "paid" || header.Contains("net") || header == "payment" || (header.Contains("payment") && header.Contains("amount")) || header.Contains("r.amount") || header.Contains("rec amount") || header.Contains("received amount")) colPaidAmt = c;
                            else if (header.Contains("shortage") || header.Contains("deduction") || header.Contains("short")) colShortage = c;
                        }

                        for (int i = headerRowIndex + 1; i < dataTable.Rows.Count; i++)
                        {
                            DataRow row = dataTable.Rows[i];
                            if (row.ItemArray.All(v => v == null || string.IsNullOrWhiteSpace(v.ToString()))) continue;

                            var billNumber = row[colBillNum]?.ToString()?.Trim();
                            if (string.IsNullOrWhiteSpace(billNumber)) continue;

                            var existingBill = _transportMgmtContext.BillTables.FirstOrDefault(b => b.BillNum == billNumber);
                            if (existingBill != null)
                            {
                                var paymentReceivedStr = row[colPaidAmt]?.ToString();
                                if (!string.IsNullOrWhiteSpace(paymentReceivedStr) && double.TryParse(paymentReceivedStr, out double paymentReceived))
                                {
                                    existingBill.PaymentReceived = paymentReceived;
                                    existingBill.ActualAmount = SafeNum(row[colActualAmt]);
                                    existingBill.Tds = SafeNum(row[colTds]);
                                    existingBill.Gst = SafeNum(row[colGst]);

                                    var paymentNumber = row[colDocNum]?.ToString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(paymentNumber))
                                    {
                                        if (!paymentDictionary.ContainsKey(paymentNumber))
                                        {
                                            var existingPayment = _transportMgmtContext.PaymentTables.FirstOrDefault(p => p.DocNumber == paymentNumber);
                                            if (existingPayment == null)
                                            {
                                                var payRecDate = ParseExcelDate(row[colDate]) ?? existingBill.BillDate ?? DateTime.Now;
                                                var shortageStr = row[colShortage]?.ToString()?.Replace("-", "").Trim();
                                                double shortage = 0;
                                                double.TryParse(shortageStr, out shortage);

                                                var newPayment = new PaymentTable
                                                {
                                                    DocNumber = paymentNumber,
                                                    PayRecDate = payRecDate,
                                                    Shortage = shortage,
                                                };

                                                _transportMgmtContext.PaymentTables.Add(newPayment);
                                                await _transportMgmtContext.SaveChangesAsync();
                                                paymentDictionary.Add(paymentNumber, newPayment);
                                            }
                                            else
                                            {
                                                paymentDictionary.Add(paymentNumber, existingPayment);
                                            }
                                        }

                                        existingBill.PId = paymentDictionary[paymentNumber].PId;
                                        updatedBills.Add(existingBill);
                                    }
                                }
                            }
                        }

                        if (updatedBills.Any())
                        {
                            _transportMgmtContext.BillTables.UpdateRange(updatedBills);
                            await _transportMgmtContext.SaveChangesAsync();
                        }

                        return Ok(updatedBills.Count);
                    }
                    return BadRequest("No data found in the uploaded file.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred while processing the payments: {ex.Message}");
            }
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
            return null;
        }
    }
}

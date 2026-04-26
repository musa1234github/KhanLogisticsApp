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
            //this._excelDataReader = excelDataReader;
            //this._configuration  = configuration;
            this._hostingEnvironment = webHostEnvironment;
            //this._hostingEnvironment = hostingEnvironment;

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
                List<BillTable> updatedBills = new List<BillTable>(); // List to store updated bill records
                Dictionary<string, PaymentTable> paymentDictionary = new Dictionary<string, PaymentTable>(); // Dictionary to store unique payment records mapped by payment number

                // Saving the uploaded file
                string filename = $"{_hostingEnvironment.WebRootPath}\\files\\{file.FileName}";
                string dirpath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                string datafilename = Path.GetFileName(file.FileName);
                string savetopath = Path.Combine(dirpath, datafilename);
                string extension = Path.GetExtension(datafilename);

                using (FileStream stream = new FileStream(savetopath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Flush();
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                // Reading the uploaded Excel file
                using (var stream = new FileStream(savetopath, FileMode.Open))
                {
                    _excelDataReader = ExcelReaderFactory.CreateReader(stream);
                    DataSet dataSet = _excelDataReader.AsDataSet();
                    _excelDataReader.Close();

                    if (dataSet != null && dataSet.Tables.Count > 0)
                    {
                        foreach (DataTable dataTable in dataSet.Tables)
                        {
                            foreach (DataRow row in dataTable.Rows)
                            {
                                var billNumber = row[0]?.ToString();
                                if (!string.IsNullOrWhiteSpace(billNumber))
                                {
                                    var existingBill = _transportMgmtContext.BillTables.FirstOrDefault(b => b.BillNum == billNumber);
                                    if (existingBill != null)
                                    {
                                        var paymentReceivedStr = row[6]?.ToString(); // Updated column index for Payment Received
                                        if (!string.IsNullOrWhiteSpace(paymentReceivedStr))
                                        {
                                            if (double.TryParse(paymentReceivedStr, out double paymentReceived))
                                            {
                                                existingBill.PaymentReceived = paymentReceived;

                                                var actualAmountStr = row[3]?.ToString(); // Actual Amount
                                                var tdsStr = row[4]?.ToString(); // Tds
                                                var gstStr = row[5]?.ToString(); // Gst

                                                if (double.TryParse(actualAmountStr, out double actualAmount))
                                                {
                                                    existingBill.ActualAmount = actualAmount;
                                                }
                                                else
                                                {
                                                    return BadRequest($"Error parsing Actual Amount for bill number {billNumber}. Actual Amount: {actualAmountStr}");
                                                }

                                                if (double.TryParse(tdsStr, out double tds))
                                                {
                                                    existingBill.Tds = tds;
                                                }
                                                else
                                                {
                                                    return BadRequest($"Error parsing Tds for bill number {billNumber}. Tds: {tdsStr}");
                                                }

                                                if (double.TryParse(gstStr, out double gst))
                                                {
                                                    existingBill.Gst = gst;
                                                }
                                                else
                                                {
                                                    return BadRequest($"Error parsing Gst for bill number {billNumber}. Gst: {gstStr}");
                                                }

                                                var paymentNumber = row[1]?.ToString();
                                                if (!string.IsNullOrWhiteSpace(paymentNumber))
                                                {
                                                    if (!paymentDictionary.ContainsKey(paymentNumber))
                                                    {
                                                        var existingPayment = _transportMgmtContext.PaymentTables.FirstOrDefault(p => p.DocNumber == paymentNumber);
                                                        if (existingPayment == null)
                                                        {
                                                            var payRecDateStr = row[2]?.ToString();
                                                            var shortageStr = row[7]?.ToString(); // Updated column index for Shortage

                                                            if (DateTime.TryParse(payRecDateStr, out DateTime payRecDate))
                                                            {
                                                                shortageStr = shortageStr?.Replace("-", "").Trim();

                                                                if (double.TryParse(shortageStr, out double shortage))
                                                                {
                                                                    var newPayment = new PaymentTable
                                                                    {
                                                                        DocNumber = paymentNumber,
                                                                        PayRecDate = payRecDate,
                                                                        Shortage = shortage,
                                                                    };

                                                                    _transportMgmtContext.PaymentTables.Add(newPayment);
                                                                    await _transportMgmtContext.SaveChangesAsync(); // Save changes immediately to get the PId

                                                                    paymentDictionary.Add(paymentNumber, newPayment);
                                                                }
                                                                else
                                                                {
                                                                    return BadRequest($"Error parsing shortage amount for payment number {paymentNumber}. Shortage: {shortageStr}");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                return BadRequest($"Error parsing payment date for payment number {paymentNumber}. PayRecDate: {payRecDateStr}");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            paymentDictionary.Add(paymentNumber, existingPayment);
                                                        }
                                                    }

                                                    existingBill.PId = paymentDictionary[paymentNumber].PId;
                                                    updatedBills.Add(existingBill);
                                                }
                                                else
                                                {
                                                    return BadRequest("Payment number is missing or empty.");
                                                }
                                            }
                                            else
                                            {
                                                return BadRequest("Error parsing PaymentReceived amount.");
                                            }
                                        }
                                        else
                                        {
                                            return BadRequest("PaymentReceived amount is missing or empty.");
                                        }
                                    }
                                }
                            }
                        }

                        _transportMgmtContext.BillTables.UpdateRange(updatedBills);
                        await _transportMgmtContext.SaveChangesAsync();

                        int updatedBillCount = updatedBills.Count;
                        return Ok(updatedBillCount);
                    }
                    else
                    {
                        return BadRequest("No data found in the uploaded file.");
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                return BadRequest("An error occurred while saving the entity changes. See the inner exception for details.");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred while processing the invoice: {ex.Message}");
            }

        }



      


    }

}




    




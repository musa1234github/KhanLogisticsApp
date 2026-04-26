using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using ExcelDataReader;
using System.Data;

public class JswCtrController : Controller
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly TransportMgmtContext _transportMgmtContext;

    public JswCtrController(IWebHostEnvironment hostingEnvironment, TransportMgmtContext transportMgmtContext)
    {
        _hostingEnvironment = hostingEnvironment;
        _transportMgmtContext = transportMgmtContext;
    }

    public IActionResult JswShipment()
    {
        DispatchVm model = new DispatchVm
        {
            ddlFactory = _transportMgmtContext.TblFactories
                .Where(f => f.FID == 10)
                .Select(a => new SelectListItem()
                {
                    Text = a.FactoryName,
                    Value = a.FID.ToString()
                }).ToList(),

            dispatchVm = new List<DispatchViewModel>() // Ensures it is initialized
        };

        return View("~/Views/JswCtr/JswShipment.cshtml", model); // Explicit view path
    }


    [HttpPost]
    public async Task<IActionResult> JswShipment(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            List<TblDispatch> updatedDispatches = new List<TblDispatch>();
            List<string> failedChallans = new List<string>();
            string filename = Path.Combine(_hostingEnvironment.WebRootPath, "files", file.FileName);

            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                file.CopyTo(stream);
                stream.Flush();
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = new FileStream(filename, FileMode.Open))
            {
                using var excelReader = ExcelReaderFactory.CreateReader(stream);
                DataSet dataSet = excelReader.AsDataSet();

                if (dataSet.Tables.Count > 0)
                {
                    foreach (DataTable dataTable in dataSet.Tables)
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {
                            var dispatchId = row[0].ToString();

                            if (!string.IsNullOrWhiteSpace(dispatchId))
                            {
                                var existingDispatch = _transportMgmtContext.TblDispatches
                                    .FirstOrDefault(c => c.ChallanNo == dispatchId && c.DisVid == 10);

                                if (existingDispatch != null)
                                {
                                    if (double.TryParse(row[5]?.ToString(), out double unitPrice))
                                        existingDispatch.UnitPrice = unitPrice;

                                    var deliveryNum = row[6]?.ToString();
                                    if (!string.IsNullOrWhiteSpace(deliveryNum))
                                        existingDispatch.DeliveryNum = deliveryNum;

                                    updatedDispatches.Add(existingDispatch);
                                }
                                else
                                {
                                    failedChallans.Add(dispatchId);
                                }
                            }
                        }
                    }

                    if (updatedDispatches.Any())
                    {
                        _transportMgmtContext.TblDispatches.UpdateRange(updatedDispatches);
                        await _transportMgmtContext.SaveChangesAsync();
                    }

                    string resultMessage = $"{updatedDispatches.Count} records updated.";
                    if (failedChallans.Any())
                    {
                        resultMessage += $" Failed to update {failedChallans.Count} records. Challan Numbers: {string.Join(", ", failedChallans)}";
                    }

                    return Ok(resultMessage);
                }
            }
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occurred while processing the invoice: {ex.Message}");
        }

        return BadRequest("An error occurred while processing the invoice.");
    }

   
}

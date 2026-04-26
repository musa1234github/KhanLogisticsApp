using KhanLogistics.Models.ViewModel;
using KhanLogistics.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

public class BillController : Controller
{
    private readonly TransportMgmtContext _context;

    public BillController(TransportMgmtContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IActionResult Index()
    {
        var vendors = _context.TblFactories.ToList();
        var bills = _context.BillTables.ToList();

        var billViewModels = bills.Select(bill => new BillDispatchViewModel
        {
            FID = bill.FID,
            BillID = bill.BillID,
            BillDate = bill.BillDate,
            Gst = bill.Gst,
            BillNum = bill.BillNum,
            GstDate = bill.GstDate,
            BillType = bill.BillType,
            ActualAmount = bill.ActualAmount,
            Tds = bill.Tds,
            TotalValue = bill.TotalValue,
            FactoryName = vendors.FirstOrDefault(f => f.FID == bill.FID)?.FactoryName,
            Vendors = vendors.Select(vendor => new SelectListItem
            {
                Value = vendor.FID.ToString(),
                Text = vendor.FactoryName
            }).ToList()
        }).ToList();

        return View("Index", billViewModels);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var bill = await _context.BillTables.FindAsync(id);
        if (bill == null)
        {
            return NotFound();
        }

        return View(bill);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DateTime gstDate)
    {
        var existingBill = await _context.BillTables.FindAsync(id);
        if (existingBill == null)
        {
            return NotFound();
        }

        existingBill.GstDate = gstDate;

        _context.Entry(existingBill).Property(b => b.GstDate).IsModified = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }




    private bool TblBillExists(int id)
    {
        return (_context.BillTables?.Any(e => e.BillID == id)).GetValueOrDefault();
    }

    private async Task<IEnumerable<SelectListItem>> GetFactorySelectList()
    {
        var factories = await _context.TblFactories.ToListAsync();
        return factories.Select(f => new SelectListItem
        {
            Value = f.FID.ToString(),
            Text = f.FactoryName
        }).AsEnumerable();
    }



    // Action to list all bills
    public ActionResult BillView(DateTime? startDate, DateTime? endDate, int? factoryId)
    {
        // Fetch the list of factories for the dropdown
        var factories = _context.TblFactories.ToList();

        // Build the query with filtering
        var billsQuery = _context.BillTables.AsQueryable();

        if (startDate.HasValue)
        {
            billsQuery = billsQuery.Where(b => b.BillDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            billsQuery = billsQuery.Where(b => b.BillDate <= endDate.Value);
        }

        if (factoryId.HasValue)
        {
            billsQuery = billsQuery.Where(b => b.FID == factoryId.Value);
        }

        var vendors = _context.TblFactories.ToList();
        var bills = billsQuery.ToList();

        var billList = bills.Select(b => new BillDispatchViewModel
        {
            BillID = b.BillID,
            BillNum = b.BillNum,
            BillDate = b.BillDate,
            BillType = b.BillType,
            ActualAmount = b.ActualAmount,
            Tds = b.Tds,
            TotalValue = b.TotalValue,
            Gst = b.Gst,
            FactoryName = vendors.FirstOrDefault(f => f.FID == b.FID)?.FactoryName
        }).ToList();

        // Create a ViewModel to pass data to the view
        var viewModel = new BillViewModel
        {
            Bills = billList,
            Factories = factories,
            SelectedFactoryId = factoryId,
            StartDate = startDate,
            EndDate = endDate
        };

        return View(viewModel);
    }

   

    public ActionResult Details(int id)
    {
        var bill = _context.BillTables
                      .Include(b => b.tblDispatches)
                      .FirstOrDefault(b => b.BillID == id);

        if (bill == null)
        {
            return NotFound();
        }

        // Fetch the factory details manually
        var factory = _context.TblFactories.FirstOrDefault(f => f.FID == bill.FID);

        var viewModel = new BillDispatchViewModel
        {
            BillID = bill.BillID,
            BillNum = bill.BillNum,
            BillDate = bill.BillDate,
            BillType = bill.BillType,
            FactoryName = factory?.FactoryName, // Set the factory name here
            tblDispatches = bill.tblDispatches.Select(d => new DispatchViewModel
            {
                ChallanNo = d.ChallanNo,
                DispatchDate = Convert.ToDateTime(d.DispatchDate),
                Destination = d.Destination,
                DispatchQuantity = d.DispatchQuantity,
                UnitPrice = d.UnitPrice,
                FinalPrice = d.FinalPrice,
                VehicleNo = d.VehicleNo,
                Lr = d.Lr,
                DeliveryNum = d.DeliveryNum
            }).ToList()
        };

        return View(viewModel);
    }



    public IActionResult ExportToExcel(int id)
    {
        // Set the license context for EPPlus
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var bill = _context.BillTables
                      .Include(b => b.tblDispatches)
                      .FirstOrDefault(b => b.BillID == id);

        if (bill == null)
        {
            return NotFound();
        }

        var factory = _context.TblFactories.FirstOrDefault(f => f.FID == bill.FID);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Bill Details");

        worksheet.Cells[1, 1].Value = "Factory Name";
        worksheet.Cells[1, 2].Value = factory?.FactoryName ?? "N/A";

        worksheet.Cells[2, 1].Value = "Bill Number";
        worksheet.Cells[2, 2].Value = bill.BillNum;

        worksheet.Cells[3, 1].Value = "Bill Date";
        worksheet.Cells[3, 2].Value = bill.BillDate?.ToString("yyyy-MM-dd");

        worksheet.Cells[4, 1].Value = "Bill Type";
        worksheet.Cells[4, 2].Value = bill.BillType;

        worksheet.Cells[6, 1].Value = "Challan No";
        worksheet.Cells[6, 2].Value = "Dispatch Date";
        worksheet.Cells[6, 3].Value = "Destination";
        worksheet.Cells[6, 4].Value = "Dispatch Quantity";
        worksheet.Cells[6, 5].Value = "Unit Price";
        worksheet.Cells[6, 6].Value = "Final Price";
        worksheet.Cells[6, 7].Value = "Vehicle No";
        worksheet.Cells[6, 8].Value = "LR Number";
        worksheet.Cells[6, 9].Value = "Delivery Number";

        int row = 7;
        foreach (var dispatch in bill.tblDispatches)
        {
            worksheet.Cells[row, 1].Value = dispatch.ChallanNo;
            worksheet.Cells[row, 2].Value = Convert.ToDateTime(dispatch.DispatchDate).ToString("yyyy-MM-dd");
            worksheet.Cells[row, 3].Value = dispatch.Destination;
            worksheet.Cells[row, 4].Value = dispatch.DispatchQuantity;
            worksheet.Cells[row, 5].Value = dispatch.UnitPrice;
            worksheet.Cells[row, 6].Value = dispatch.FinalPrice;
            worksheet.Cells[row, 7].Value = dispatch.VehicleNo;
            worksheet.Cells[row, 8].Value = dispatch.Lr;
            worksheet.Cells[row, 9].Value = dispatch.DeliveryNum;
            row++;
        }

        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;

        var fileName = "BillDetails.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var bill = await _context.BillTables
            .Include(b => b.tblDispatches)
            .FirstOrDefaultAsync(b => b.BillID == id);

        if (bill != null)
        {
            // Unlink dispatches
            if (bill.tblDispatches != null)
            {
                foreach (var dispatch in bill.tblDispatches)
                {
                    dispatch.BillID = null;
                    // We don't necessarily need to zero out prices here as the re-upload will overwrite them,
                    // but unlinking ensures they are available for the next upload process.
                }
            }

            _context.BillTables.Remove(bill);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bill deleted successfully. Dispatches have been unlinked and are ready for re-upload.";
        }

        return RedirectToAction(nameof(BillView));
    }
}


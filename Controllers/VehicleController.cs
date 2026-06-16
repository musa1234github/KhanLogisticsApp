using KhanLogistics.Models;
using KhanLogistics.Models.ViewModel;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data;

namespace KhanLogistics.Controllers
{
    public class VehicleController : Controller
    {
        TransportMgmtContext _context;
        IConfiguration _configuration;
        IWebHostEnvironment _hostingEnvironment;
        IExcelDataReader _excelDataReader;

        public VehicleController(TransportMgmtContext context, IWebHostEnvironment webHostEnvironment)
        {
            this._context = context;
            //this._excelDataReader = excelDataReader;
            //this._configuration  = configuration;
            this._hostingEnvironment = webHostEnvironment;
            //this._hostingEnvironment = hostingEnvironment;

        }
        public IActionResult ShowVehicle()
        {
            var vehicles = _context.TblVehicles.ToList(); // Get all vehicles from the database
            return View("ShowVehicle", vehicles); // Return the view with the vehicles list
        }


        [HttpPost]
        public async Task<IActionResult> ShowVehicle(IFormFile file)
        {
            int successfulUploads = 0;
            int updatedRecords = 0;

            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("File is empty or null.");
                }

                string filename = $"{_hostingEnvironment.WebRootPath}\\files\\{file.FileName}";
                string dirpath = Path.Combine(_hostingEnvironment.WebRootPath, "files");
                string datafilename = Path.GetFileName(file.FileName);
                string savetopath = Path.Combine(dirpath, datafilename);
                string extention = Path.GetExtension(datafilename);

                using (FileStream stream = new FileStream(savetopath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    stream.Flush();
                }

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = new FileStream(savetopath, FileMode.Open))
                {
                    _excelDataReader = ExcelReaderFactory.CreateReader(stream);
                    DataSet dts = _excelDataReader.AsDataSet();

                    if (dts != null && dts.Tables.Count > 0)
                    {
                        foreach (DataTable dataTable in dts.Tables)
                        {
                            for (int i = 1; i < dataTable.Rows.Count; i++)
                            {
                                var cell1 = Convert.ToString(dataTable.Rows[i][0]); // VehicleNumber
                                var cell2 = Convert.ToString(dataTable.Rows[i][1]); // VehicleType
                                var cell3 = Convert.ToString(dataTable.Rows[i][2]); // VehicleInsurStartDate
                                var cell4 = Convert.ToString(dataTable.Rows[i][3]); // VehicleInsurEndDate
                                var cell5 = Convert.ToString(dataTable.Rows[i][4]); // VehicleFitnessStartDate
                                var cell6 = Convert.ToString(dataTable.Rows[i][5]); // VehicleFitnessEndDate
                                var cell7 = Convert.ToString(dataTable.Rows[i][6]); // TaxStartDate
                                var cell8 = Convert.ToString(dataTable.Rows[i][7]); // TaxEndDate
                                var cell9 = Convert.ToString(dataTable.Rows[i][8]); // VehiclePermitDate

                                if (string.IsNullOrWhiteSpace(cell1))
                                {
                                    continue; // Skip if VehicleNumber is missing
                                }

                                // Convert date strings to DateTime objects
                                DateTime? insurStartDate = string.IsNullOrWhiteSpace(cell3) ? (DateTime?)null : Convert.ToDateTime(cell3);
                                DateTime? insurEndDate = string.IsNullOrWhiteSpace(cell4) ? (DateTime?)null : Convert.ToDateTime(cell4);
                                DateTime? fitnessStartDate = string.IsNullOrWhiteSpace(cell5) ? (DateTime?)null : Convert.ToDateTime(cell5);
                                DateTime? fitnessEndDate = string.IsNullOrWhiteSpace(cell6) ? (DateTime?)null : Convert.ToDateTime(cell6);
                                DateTime? taxStartDate = string.IsNullOrWhiteSpace(cell7) ? (DateTime?)null : Convert.ToDateTime(cell7);
                                DateTime? taxEndDate = string.IsNullOrWhiteSpace(cell8) ? (DateTime?)null : Convert.ToDateTime(cell8);
                                DateTime? permitDate = string.IsNullOrWhiteSpace(cell9) ? (DateTime?)null : Convert.ToDateTime(cell9);

                                // Check if the vehicle already exists in the database
                                var existingVehicle = _context.TblVehicles.FirstOrDefault(v => v.VehicleNumber == cell1);
                                if (existingVehicle != null)
                                {
                                    // Check if you want to update or not
                                    // You can add a custom flag to indicate whether this duplicate should be updated or not
                                    if (existingVehicle != null)
                                    {
                                        // Update the existing vehicle's data
                                        if (!string.IsNullOrWhiteSpace(cell2)) existingVehicle.VehicleType = cell2;
                                        if (insurStartDate.HasValue) existingVehicle.VehicleInsurStartDate = insurStartDate;
                                        if (insurEndDate.HasValue) existingVehicle.VehicleInsurEndtDate = insurEndDate;
                                        if (fitnessStartDate.HasValue) existingVehicle.VehicleFitnessStartDate = fitnessStartDate;
                                        if (fitnessEndDate.HasValue) existingVehicle.VehicleFitnessEndDate = fitnessEndDate;
                                        if (taxStartDate.HasValue) existingVehicle.TaxStartDate = taxStartDate;
                                        if (taxEndDate.HasValue) existingVehicle.TaxEndDate = taxEndDate;
                                        if (permitDate.HasValue) existingVehicle.VehiclePermitDate = permitDate;

                                        _context.Update(existingVehicle);
                                        updatedRecords++;
                                    }
                                }
                                else
                                {
                                    var newVehicle = new TblVehicle()
                                    {
                                        VehicleNumber = cell1,
                                        VehicleType = cell2,
                                        VehicleInsurStartDate = insurStartDate,
                                        VehicleInsurEndtDate = insurEndDate,
                                        VehicleFitnessStartDate = fitnessStartDate,
                                        VehicleFitnessEndDate = fitnessEndDate,
                                        TaxStartDate = taxStartDate,
                                        TaxEndDate = taxEndDate,
                                        VehiclePermitDate = permitDate
                                    };

                                    _context.Add(newVehicle);
                                    successfulUploads++;
                                }
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error occurred: {ex.Message}");
            }

            var vehicles = _context.TblVehicles.ToList();
            ViewBag.SuccessfulUploads = successfulUploads;
            ViewBag.UpdatedRecords = updatedRecords; // Display the number of updated records

            return View("ShowVehicle", vehicles);
        }


        public IActionResult NearExpiryVehicles()
        {
            DateTime today = DateTime.Now;
            DateTime nearExpiryThreshold = today.AddDays(30);

            // Fetch vehicles nearing expiry
            var vehicles = _context.TblVehicles
                .Where(v =>
                    (v.VehicleInsurEndtDate.HasValue && v.VehicleInsurEndtDate <= nearExpiryThreshold) ||
                    (v.VehicleFitnessEndDate.HasValue && v.VehicleFitnessEndDate <= nearExpiryThreshold) ||
                    (v.TaxEndDate.HasValue && v.TaxEndDate <= nearExpiryThreshold) ||
                    (v.VehiclePermitDate.HasValue && v.VehiclePermitDate <= nearExpiryThreshold))
                .ToList();

            if (!vehicles.Any())
            {
                return View("NoVehiclesNearExpiry"); // Render a view with no data message
            }

            var vehiclesGroupedByExpiry = vehicles
                .Select(v => new
                {
                    Vehicle = v,
                    DaysToExpire = CalculateDaysToExpire(v)
                })
                .OrderBy(v => v.DaysToExpire)  // Sort by days to expiry
                .ToList();

            return View(vehiclesGroupedByExpiry);
        }





        public IActionResult ExportToExcel()
        {
            try
            {
                // Set the license context for EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                DateTime today = DateTime.Now;
                DateTime nearExpiryThreshold = today.AddDays(30);

                // Fetch vehicles nearing expiry
                var vehicles = _context.TblVehicles
                    .Where(v =>
                        (v.VehicleInsurEndtDate.HasValue && v.VehicleInsurEndtDate <= nearExpiryThreshold) ||
                        (v.VehicleFitnessEndDate.HasValue && v.VehicleFitnessEndDate <= nearExpiryThreshold) ||
                        (v.TaxEndDate.HasValue && v.TaxEndDate <= nearExpiryThreshold) ||
                        (v.VehiclePermitDate.HasValue && v.VehiclePermitDate <= nearExpiryThreshold))
                    .ToList();

                if (!vehicles.Any())
                {
                    return NotFound("No vehicle records found.");
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Vehicle Details");

                // Define headers
                var headers = new[] { "Vehicle Number", "Vehicle Type", "Insurance Expiry", "Fitness Expiry", "Tax Expiry", "Permit Expiry", "Days to Expiry" };
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cells[1, col].Value = headers[col - 1];
                }

                // Add vehicle data
                int row = 2;
                foreach (var vehicle in vehicles)
                {
                    int daysToExpire = CalculateDaysToExpire(vehicle);
                    worksheet.Cells[row, 1].Value = vehicle.VehicleNumber;
                    worksheet.Cells[row, 2].Value = vehicle.VehicleType;
                    worksheet.Cells[row, 3].Value = vehicle.VehicleInsurEndtDate?.ToString("dd-MM-yyyy") ?? "N/A";
                    worksheet.Cells[row, 4].Value = vehicle.VehicleFitnessEndDate?.ToString("dd-MM-yyyy") ?? "N/A";
                    worksheet.Cells[row, 5].Value = vehicle.TaxEndDate?.ToString("dd-MM-yyyy") ?? "N/A";
                    worksheet.Cells[row, 6].Value = vehicle.VehiclePermitDate?.ToString("dd-MM-yyyy") ?? "N/A";
                    worksheet.Cells[row, 7].Value = daysToExpire;
                    row++;
                }

                // Autofit columns for better readability
                worksheet.Cells.AutoFitColumns();

                // Save to a stream
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = "NearExpiryVehicles.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                Console.WriteLine(ex.Message); // Replace with proper logging
                return StatusCode(500, "An error occurred while exporting the data.");
            }
        }



        private int CalculateDaysToExpire(TblVehicle vehicle)
        {
            DateTime today = DateTime.Now;
            var expiryDates = new List<DateTime?>
        {
            vehicle.VehicleInsurEndtDate,
            vehicle.VehicleFitnessEndDate,
            vehicle.TaxEndDate,
            vehicle.VehiclePermitDate
        };

            // Get the nearest expiry date
            var nearestExpiryDate = expiryDates.Where(d => d.HasValue).Select(d => d!.Value).DefaultIfEmpty(today).Min();

            // Calculate the days to expire
            return (nearestExpiryDate - today).Days;
        }
    }




}


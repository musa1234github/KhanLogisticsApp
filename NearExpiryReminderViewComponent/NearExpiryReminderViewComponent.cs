using KhanLogistics.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhanLogistics.ViewComponents
{
    public class NearExpiryReminderViewComponent : ViewComponent
    {
        private readonly TransportMgmtContext _context;

        public NearExpiryReminderViewComponent(TransportMgmtContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            DateTime today = DateTime.Now;
            DateTime nearExpiryThreshold = today.AddDays(30);

            // Fetch vehicles with any expiry within the next 30 days
            var nearExpiryVehicles = await _context.TblVehicles
                .Where(v =>
                    (v.VehicleInsurEndtDate.HasValue && v.VehicleInsurEndtDate <= nearExpiryThreshold) ||
                    (v.VehicleFitnessEndDate.HasValue && v.VehicleFitnessEndDate <= nearExpiryThreshold) ||
                    (v.TaxEndDate.HasValue && v.TaxEndDate <= nearExpiryThreshold) ||
                    (v.VehiclePermitDate.HasValue && v.VehiclePermitDate <= nearExpiryThreshold))
                .ToListAsync();

            // Group vehicles by how close they are to expiry
            var vehiclesGroupedByExpiry = nearExpiryVehicles
                .Select(v => new
                {
                    Vehicle = v,
                    DaysToExpire = CalculateDaysToExpire(v)
                })
                .OrderBy(v => v.DaysToExpire)  // Sort by days to expiry
                .ToList();

            return View(vehiclesGroupedByExpiry);
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
            var nearestExpiryDate = expiryDates.Where(d => d.HasValue).Min(d => d.Value);

            // Calculate the days to expire
            return (nearestExpiryDate - today).Days;
        }
    }


}

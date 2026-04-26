using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class SpGetDispatch
    {
        public int DispId { get; set; }  // ✅ Added DispatchId
        public string? ChallanNo { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yy}", ApplyFormatInEditMode = true)]
        public DateTime? DispatchDate { get; set; }
        public double? DispatchQuantity { get; set; } = 0;
        public double? UnitPrice { get; set; } = 0;
        public string? FactoryName { get; set; } = null!;
        public string? Destination { get; set; } = null!;
        public string? VehicleNo { get; set; } = null!;
        public string? BillNum { get; set; }
    }
}

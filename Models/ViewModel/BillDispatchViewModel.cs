using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models.ViewModel
{
    public class BillDispatchViewModel
    {
        public string? FactoryName { get; set; }
        public int BillID { get; set; }
        public string? BillNum { get; set; }
        public DateTime? BillDate { get; set; }
        public string? Destination { get; set; }
        public DateTime? DispatchDate { get; set; }
        public DateTime? GstDate { get; set; }
        public string? BillType { get; set; }
        public double? DispatchQuantity { get; set; }
        public string? VehicleNo { get; set; }
        public double? UnitPrice { get; set; }
        public double? Gst { get; set; }
        public double? FinalPrice { get; set; }
        public double TotalPrice { get; set; }
        public double? PaymentReceived { get; set; }
        public double? ActualAmount { get; set; }
        public double? Tds { get; set; }
        public double? TotalValue { get; set; }
        public double Difference { get; set; }
        public string? ChallanNo { get; set; }

        [Display(Name = "Factory")]
        public string Factory { get; set; } = null!;
        public int? FID { get; set; } // For the selected value

        public IEnumerable<SelectListItem> Vendors { get; set; } = null!;

        public List<DispatchViewModel> tblDispatches { get; set; } = new List<DispatchViewModel>(); 
    }


}

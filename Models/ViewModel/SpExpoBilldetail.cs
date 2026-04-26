using Microsoft.EntityFrameworkCore;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class SpExpoBilldetail
    {
        public string? BillNum { get; set; }
        public DateTime? BillDate { get; set; }
        public string? BillType { get; set; }
        public int? FactoryId { get; set; }
        public string? FactoryName { get; set; }
        public double? BillQty { get; set; }
        public double? TaxableAmount { get; set; }
        public double? FinalPrice { get; set; }
        public double? ActualAmount { get; set; }
        public double? Tds { get; set; }
        public double? Gst { get; set; }
        public string? DispatchMonth { get; set; }
        public int? LrQty { get; set; }
    }


}
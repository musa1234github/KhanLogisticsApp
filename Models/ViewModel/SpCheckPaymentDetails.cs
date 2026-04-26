using Microsoft.EntityFrameworkCore;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class SpCheckPaymentDetails
    {
        public string? BillNum { get; set; }
        public DateTime? BillDate { get; set; }
        public string? PaymentNum { get; set; }
        public DateTime? PaymentDate { get; set; }
        public double? Deduction { get; set; }
        public double? PaymentReceived { get; set; }
        public double? ActualAmount { get; set; }
        public double? Tds { get; set; }
        public double? Gst { get; set; }
    }

}

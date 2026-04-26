using Microsoft.EntityFrameworkCore;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class SpPaymentByDate
    {
        public string PaymentNum { get; set; }  // Rename from PaymentNum to match stored procedure
        public double? TotalPaymentReceived { get; set; }  // Rename from PaymentReceived and use SUM() aggregate
        public DateTime? PaymentDate { get; set; }
        public double? Deduction { get; set; }
        public double? AmountAfterDeduction { get; set; }
        //public int FactoryId { get; set; }
        public string FactoryName { get; set; }
        public double? TotalActualAmount { get; set; }
        public double? Tds { get; set; }
        public double? Gst { get; set; }
    }
}

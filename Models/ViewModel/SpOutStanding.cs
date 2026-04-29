using Microsoft.EntityFrameworkCore;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class SpOutStanding
    {
        public string? BillNum { get; set; }
        public DateTime? BillDate { get; set; }
        public string? BillType { get; set; }
        public string? FactoryName { get; set; }

        public double? ActualAmount { get; set; }
        public string? DispatchMonth { get; set; }

        public double? PaymentReceived { get; set; }
        public int? InvoiceAge { get; set; }
    }
}

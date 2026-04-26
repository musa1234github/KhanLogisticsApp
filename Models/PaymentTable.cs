using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models
{
    public class PaymentTable
    {
        [Key]
        public int PId { get; set; }
        public ICollection<BillTable>? tblBill { get; set; } // Navigation property
        public DateTime? PayRecDate { get; set; }
        public int? FID { get; set; }
        public string? DocNumber { get; set; }
        public double? Shortage { get; set; }
        
    }
}

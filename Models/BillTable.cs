using System.ComponentModel.DataAnnotations;


namespace KhanLogistics.Models
{
    public class BillTable
    {
        [Key]
        public int BillID { get; set; }
        public string? BillNum { get; set; }
        public int? PId { get; set; }
        public ICollection<TblDispatch>? tblDispatches { get; set; } // Navigation property
        public DateTime? BillDate { get; set; }
        public DateTime? GstDate { get; set; }
        public string? BillType { get; set; }
        public int? FID { get; set; }
        public double? PaymentReceived { get; set; }
        public double? ActualAmount { get; set; }
        public double? Tds { get; set; }
        public double? Gst { get; set; }
        public string? PartyName { get; set; }
        //public string? Lr { get; set; }
        //public string? DeliveryNum { get; set; }
        public double? TotalValue { get; set; }


    }
}

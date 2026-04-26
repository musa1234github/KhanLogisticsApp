using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models
{
    public class TblDispatch
    {
        [Key]
        public int DispId { get; set; }

        public string? ChallanNo { get; set; }

        public DateTime? DispatchDate { get; set; }
        public string? Destination { get; set; }
        public double? DispatchQuantity { get; set; }
        public double? UnitPrice { get; set; }
        public double? FinalPrice { get; set; }
        public int? DisVid { get; set; }
        public int? Shortage { get; set; }
        public int? BillID { get; set; }
        public BillTable? bill { get; set; }
        public string? VehicleNo { get; set; }
        public string? PartyName { get; set; }
        public string? Lr { get; set; }
        public string? DeliveryNum { get; set; }
        public double? TotalValue { get; set; }
        public bool? IsReceived { get; set; }
        public string? ExNo { get; set; }
    }

}

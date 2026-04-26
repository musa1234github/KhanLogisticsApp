using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class BillDetailViewModel
    {
        public int FID { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yy}", ApplyFormatInEditMode = true)]
        [DisplayName("Bill Date")]
        public DateTime? BillDate { get; set; }
        [DisplayName("Bill Num")]
        public string? BillNum { get; set; }
        [DisplayName("Bill Type")]
        public string? BillType { get; set; }
        [DisplayName("Bill Qty")]
        public double? BillQty { get; set; } = 0;
        [DisplayName("T.Amount")]
        public double? TaxableAmount { get; set; } = 0;
        [DisplayName("A.Amount")]
        public double? ActualAmount { get; set; } = 0;
        public double? Tds { get; set; } = 0;
        public double? Gst { get; set; } = 0;
        public int? LrQty { get; set; } = 0;
        [DisplayName("Factory")]
        public string? FactoryName { get; set; } = null!;
        [DisplayName("Month")]
        public string? DispatchMonth { get; set; } = null!;
    }

}

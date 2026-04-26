using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models.ViewModel
{
    public class PaymentViewModel
    {
        public int BillId { get; set; }
        public string? BillNum { get; set; }
        public int FID { get; set; }
        public string FactoryName { get; set; }
        public IEnumerable<SelectListItem> FactoryList { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yy}", ApplyFormatInEditMode = true)]
        public DateTime? BillDate { get; set; }
        public string? BillType { get; set; }
        public double? PaymentReceived { get; set; }
        public double? Shortage { get; set; }
        public string? DocNumber { get; set; }
        public double? ActualAmount { get; set; }
        public double? Tds { get; set; }
        public double? Gst { get; set; }
    }
    public class PaymentVm
    {
        public IEnumerable<PaymentViewModel> payments { get; set; }
        public IEnumerable<SelectListItem> FactoryList { get; set; }
        public List<int> SelectedIds { get; set; }
    }

}

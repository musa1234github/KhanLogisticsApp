using Microsoft.AspNetCore.Mvc.Rendering;

namespace KhanLogistics.Models.ViewModel
{
    public class InvoiceViewModel
    {
        public int BilldetailId { get; set; }
        public int BillId { get; set; }
        public int FID { get; set; }
        public string FactoryName { get; set; }
        public IEnumerable<SelectListItem> FactoryList { get; set; }
        public string ChallanNo { get; set; }
        public DateTime? DispatchDate { get; set; }
        public string Destination { get; set; }
        public double? InvoicechQty { get; set; }
        public string VehicleNo { get; set; }
        public double? UnitPrice { get; set; }
    }
    public class InvoiceVm
    {
        public IEnumerable<InvoiceViewModel> Invoices { get; set; }
        public IEnumerable<SelectListItem> FactoryList { get; set; }
        public List<int> SelectedIds { get; set; }
    }

}

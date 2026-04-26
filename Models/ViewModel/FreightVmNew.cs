using Microsoft.AspNetCore.Mvc.Rendering;

namespace KhanLogistics.Models.ViewModels
{
    public class FreightVmNew
    {
        public int DestId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string Destination { get; set; } = null!;
        public int Vid { get; set; } 
        public string FactoryName { get; set; } = null!;
        public string Freight { get; set; } = null!;    
        public double FreightRate { get; set; } 
        public string Wheels { get; set; } = null!;  
    }
    public partial class FreightVModel
    {
        public IEnumerable<FreightVmNew> freightVm { get; set; }
        public IEnumerable<SelectListItem> ddlVendors { get; set; }
    }
    public partial class VendorViewModel
    {
        public int Fid { get; set; }
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

}

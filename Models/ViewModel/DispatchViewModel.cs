using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models.ViewModel
{
    public class DispatchViewModel
    {
            public int DispId { get; set; }
            public string? FactoryName { get; set; } = null!;
            [DisplayFormat(DataFormatString = "{0:dd/MM/yy}", ApplyFormatInEditMode = true)]
            public DateTime DispatchDate { get; set; }
            public string? Destination { get; set; }
            public double? DispatchQuantity { get; set; }
            public double? UnitPrice { get; set; }
            public double? FinalPrice { get; set; }
            public string? ChallanNo { get; set; }
            public int? FID { get; set; }
            public int? Shortage { get; set; }
            public string? VehicleNo { get; set; }
            public string? PartyName { get; set; }
            public string? Lr { get; set; }
            public string? DeliveryNum { get; set; }
            public double? TotalValue { get; set; }
            public IEnumerable<SelectListItem> factory { get; set; } = null!;
            public bool? IsReceived { get; set; }
            public string? ExNo { get; set; }

    }
        public partial class DispatchVm
        {
        public int SelectedFactoryForDelete { get; set; }
        public IEnumerable<DispatchViewModel> dispatchVm { get; set; }
        public IEnumerable<SelectListItem> ddlFactory { get; set; }
        public List<int> SelectedIds { get; set; }
        public int? ExportFactoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int SelectedImageFactoryId { get; set; }
    }
        public partial class FactoryViewModel
        {
            public int Fid { get; set; }
            public string Code { get; set; } = null!;
            public string FactoryName { get; set; } = null!;
        }
    }

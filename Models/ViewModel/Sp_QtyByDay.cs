using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class Sp_QtyByDay
    {
        public string? Factory { get; set; }

        [Display(Name = "Dispatch Qty")]
        public double? totalQty { get; set; }
        [Display(Name = "Bill Qty")]
        public double? BillQty { get; set; }
        [Display(Name = "Day")]
        public string? DayName { get; set; }
    }
}

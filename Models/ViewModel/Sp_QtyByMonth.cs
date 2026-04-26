using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models.ViewModel
{
    [Keyless]
    public class Sp_QtyByMonth
    {
        public string? Factory { get; set; }

        [Display(Name = "Dispatch Qty")]
        public double? totalQty { get; set; }
        [Display(Name = "Bill Qty")]
        public double? BillQty { get; set; }
        [Display(Name = "Month")]
        public string? MonthName { get; set; }
        [Display(Name = "Balance Qty")]
        public double? Balance { get; set; }



    }
}

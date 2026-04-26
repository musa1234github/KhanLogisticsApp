using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models
{
    public partial class TblFreight
    {
        [Key]
        public int DestId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string Destination { get; set; } = null!;
        public string Wheels { get; set; } = null!;
        public string Quantity { get; set; } = null!;
        public double FreightRate { get; set; }
        public int Vid { get; set; }
    }
}

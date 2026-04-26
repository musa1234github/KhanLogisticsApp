using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models
{
    public class TblFactory
    {
        [Key]
        public int FID { get; set; }
        public string Code { get; set; } = null!;
        public string FactoryName { get; set; } = null!;
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public double? Gstin { get; set; }
    }
}

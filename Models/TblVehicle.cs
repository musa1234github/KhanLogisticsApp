using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models
{
    public class TblVehicle
    {
        [Key]
        public int VehicleId { get; set; }
        public string? VehicleNumber { get; set; }
        public string? VehicleType { get; set; }
        public DateTime? VehicleInsurStartDate { get; set; }
        public DateTime? VehicleInsurEndtDate { get; set; }
        public DateTime? VehicleFitnessStartDate { get; set; }
        public DateTime? VehicleFitnessEndDate { get; set; }
        public DateTime? TaxStartDate { get; set; }
        public DateTime? TaxEndDate { get; set; }
        public DateTime? VehiclePermitDate { get; set; }
    }
}

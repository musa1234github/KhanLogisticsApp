namespace KhanLogistics.Models.ViewModel
{
    public class NearExpiryViewModel
    {
        public List<TblVehicle> InsuranceExpiry { get; set; } = new List<TblVehicle>();
        public List<TblVehicle> FitnessExpiry { get; set; } = new List<TblVehicle>();
        public List<TblVehicle> TaxExpiry { get; set; } = new List<TblVehicle>();
        public List<TblVehicle> PermitExpiry { get; set; } = new List<TblVehicle>();
    }


}

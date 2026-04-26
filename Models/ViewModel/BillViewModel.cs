namespace KhanLogistics.Models.ViewModel
{
    public class BillViewModel
    {
        public IEnumerable<BillDispatchViewModel> Bills { get; set; }
        public IEnumerable<TblFactory> Factories { get; set; }
        public int? SelectedFactoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}

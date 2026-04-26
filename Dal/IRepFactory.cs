using KhanLogistics.Models;

namespace KhanLogistics.Dal
{
    public interface IRepFactory
    {
        public IEnumerable<TblFactory> GetAllFactory();
        public TblFactory GetFactoryDetails(int id);
        public TblFactory GetFactoryById(int id);
        public int CreateFactory(TblFactory factory);
        public int UpdateFactory(TblFactory factory);
        public int DeleteFactory(int id);
        public bool TblFactoryExists(int id);
    }
}

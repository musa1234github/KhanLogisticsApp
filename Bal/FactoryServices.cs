using KhanLogistics.Dal;
using KhanLogistics.Models;

namespace KhanLogistics.Bal
{
    public class FactoryServices:ISRVFactory
    {
        private IRepFactory iRepfactory = null;
        public FactoryServices(IRepFactory factory)
        {
            this.iRepfactory = factory;
        }
        public int CreateFactory(TblFactory factory)
        {
            return iRepfactory.CreateFactory(factory);
        }

        public int DeleteFactory(int id)
        {
            return iRepfactory.DeleteFactory(id);
        }

        public IEnumerable<TblFactory> GetAllFactory()
        {
            return iRepfactory.GetAllFactory();
        }

        public TblFactory GetFactoryById(int id)
        {
            return iRepfactory.GetFactoryById(id);
        }

        public TblFactory GetFactoryDetails(int id)
        {
            return iRepfactory.GetFactoryDetails(id);
        }

        public bool TblFactoryExists(int id)
        {
            return iRepfactory.TblFactoryExists(id);
        }

        public int UpdateFactory(TblFactory factory)
        {
            return iRepfactory.UpdateFactory(factory);
        }
    }
}


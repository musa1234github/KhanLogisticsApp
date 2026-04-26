using KhanLogistics.Models;

namespace KhanLogistics.Dal
{
    public class RepFactory:IRepFactory      
    {
        private readonly TransportMgmtContext _context;
        public RepFactory(TransportMgmtContext _ctx)
        {
            this._context = _ctx;
        }
        public int CreateFactory(TblFactory factory)
        {
            _context.TblFactories.Add(factory);
            _context.SaveChanges();
            return 1;
        }

        public int DeleteFactory(int id)
        {
            TblFactory factory = _context.TblFactories.FirstOrDefault(x => x.FID == id);
            _context.TblFactories.Remove(factory);
            _context.SaveChanges();
            return 1;
        }

        public IEnumerable<TblFactory> GetAllFactory()
        {
            return _context.TblFactories.AsEnumerable();
        }

        //public TblVendor GetVenderById(int id)
        //{
        //    return _context.TblVendors.FirstOrDefault(v => v.Vid == id);

        //}
        public TblFactory GetFactoryById(int id)
        {
            return _context.TblFactories.Where(f => f.FID == id).FirstOrDefault();
        }

        public TblFactory GetFactoryDetails(int id)
        {
            return _context.TblFactories.Where(f => f.FID == id).FirstOrDefault();
        }

        public int UpdateFactory(TblFactory factory)
        {
            if (factory == null || factory.FID == 0)
            {
                return 0;
            }
            _context.TblFactories.Update(factory);
            _context.SaveChanges();
            return 1;
        }
        public bool TblFactoryExists(int id)
        {
            return (_context.TblFactories?.Any(f => f.FID == id)).GetValueOrDefault();
        }


    }
}


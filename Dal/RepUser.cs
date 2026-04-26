using KhanLogistics.Models;

namespace KhanLogistics.Dal
{
    public class RepUser : IRepUser
    {
        private readonly TransportMgmtContext _context;
        public RepUser(TransportMgmtContext _ctx)
        {
            _context = _ctx;
        }


        public int CreateUser(TblUser user)
        {
            _context.TblUsers.Add(user);
            _context.SaveChanges();
            return 1;
        }

        public int DeleteUser(int id)
        {
            TblUser user = _context.TblUsers.FirstOrDefault(x => x.UserId == id);
            _context.TblUsers.Remove(user);
            _context.SaveChanges();
            return 1;
        }

        public IEnumerable<TblUser> GetAllUsers()
        {
            return _context.TblUsers.AsEnumerable();
        }

        public TblUser GetUserByCreds(string emailId, string password)
        {
            return _context.TblUsers.FirstOrDefault(a => a.Email == emailId && a.Password == password);
        }

        public TblUser GetUserById(int id)
        {
            return _context.TblUsers.FirstOrDefault(a => a.UserId == id);

        }

        public bool TblUserExists(int id)
        {
            return (_context.TblUsers?.Any(e => e.UserId == id)).GetValueOrDefault();
        }

        public int UpdateUser(TblUser user)
        {
            if (user == null || user.UserId == 0)
            {
                return 0;
            }
            _context.TblUsers.Update(user);
            _context.SaveChanges();
            return 1;
        }
    }
}





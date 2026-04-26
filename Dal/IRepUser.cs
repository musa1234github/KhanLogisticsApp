using KhanLogistics.Models;

namespace KhanLogistics.Dal
{
    public interface IRepUser
    {
        public IEnumerable<TblUser> GetAllUsers();
        public TblUser GetUserByCreds(string emailId, string password);
        public TblUser GetUserById(int id);
        public int CreateUser(TblUser user);
        public int UpdateUser(TblUser user);
        public int DeleteUser(int id);
        public bool TblUserExists(int id);
    }
}

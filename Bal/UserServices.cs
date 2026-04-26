using KhanLogistics.Dal;
using KhanLogistics.Models;

namespace KhanLogistics.Bal
{
    public class UserServices:ISrvUser
    {
        private IRepUser iRepUsr = null;
        public UserServices(IRepUser _rep)
        {
            this.iRepUsr = _rep;
        }
        public int CreateUser(TblUser user)
        {
            return iRepUsr.CreateUser(user);
        }

        public int DeleteUser(int id)
        {
            return iRepUsr.DeleteUser(id);
        }

        public IEnumerable<TblUser> GetAllUsers()
        {
            return iRepUsr.GetAllUsers();
        }

        public TblUser GetUserByCreds(string emailId, string password)
        {
            return iRepUsr.GetUserByCreds(emailId, password);
        }

        public TblUser GetUserById(int id)
        {
            return iRepUsr.GetUserById(id);
        }

        public bool TblUserExists(int id)
        {
            return iRepUsr.TblUserExists(id);
        }

        public int UpdateUser(TblUser user)
        {
            return iRepUsr.UpdateUser(user);
        }
    }
}

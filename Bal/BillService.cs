//using CodeFirstTransport.Dal;
//using CodeFirstTransport.Models;

//namespace CodeFirstTransport.Bal
//{
//    public class BillService : ISRVBILL
//    {
//        private IRepBILL repBILL = null;
//        private IRepFactory repFactory = null;
//        public BillService(IRepBILL repbill, IRepFactory repFactory)
//        {
//            this.repBILL = repbill;
//            this.repFactory = repFactory;
//        }
//        public int CreateBill(TblBill bill)
//        {
//            return repBILL.CreateBill(bill);
//        }

//        public int DeleteBill(int id)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<TblBill> GetAllBill()
//        {
//            return repBILL.GetAllBill();
//        }

//        public TblBill GetBillById(int id)
//        {
//            return repBILL.GetBillById(id);
//        }

//        public bool TblBillExists(int id)
//        {
//            throw new NotImplementedException();
//        }

//        public int UpdateBill(TblBill bill)
//        {
//            return repBILL.UpdateBill(bill);
//        }
//    }
//}

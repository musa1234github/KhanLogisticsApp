using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using KhanLogistics.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KhanLogistics.Models
{
    public partial class TransportMgmtContext : DbContext
    {
        public TransportMgmtContext()
        {
        }

        public TransportMgmtContext(DbContextOptions<TransportMgmtContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TblDispatch> TblDispatches { get; set; } = null!;
        public virtual DbSet<TblFactory> TblFactories { get; set; } = null!;
        public virtual DbSet<TblFreight> TblFreights { get; set; } = null!;
        public virtual DbSet<SpGetDispatch> SpGetDispatches { get; set; } = null!;
        public virtual DbSet<Sp_QtyByDay> Sp_QtyByDay { get; set; } = null!;
        public virtual DbSet<Sp_QtyByMonth> Sp_QtyByMonth { get; set; } = null!;
        public virtual DbSet<SpCheckBill> SpCheckBill { get; set; } = null!;
        public virtual DbSet<SpBilldetail> SpBilldetails { get; set; } = null!;
        public virtual DbSet<SpExpoBilldetail> SpExpoBilldetail { get; set; } = null!;
        public virtual DbSet<SpCheckPaymentDetails> SpCheckPaymentDetails { get; set; } = null!;
        public virtual DbSet<SpPaymentByDate> SpPaymentByDate { get; set; } = null!;
        public virtual DbSet<SpOutStanding> SpOutStanding { get; set; } = null!;
        public virtual DbSet<BillDetailViewModel> BillDetailViewModels { get; set; } = null!;
        public virtual DbSet<TblUser> TblUsers { get; set; } = null!;
        public virtual DbSet<BillTable> BillTables { get; set; } = null!;
        public virtual DbSet<PaymentTable> PaymentTables { get; set; } = null!;
        public virtual DbSet<TblVehicle> TblVehicles { get; set; } = null!;
    }
}

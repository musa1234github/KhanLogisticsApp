//using System;
//using System.Collections.Generic;
//using KhanLogistics.Models.ViewModel;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata;

//namespace KhanLogistics.Models
//{
//    public partial class TransportMgmtContext : DbContext
//    {
//        public TransportMgmtContext()
//        {
//        }

//        public TransportMgmtContext(DbContextOptions<TransportMgmtContext> options)
//            : base(options)
//        {
//        }

//        public virtual DbSet<TblDestination> TblDestinations { get; set; } = null!;
//        public virtual DbSet<TblDispatch> TblDispatches { get; set; } = null!;
//        public virtual DbSet<TblFactory> TblFactories { get; set; } = null!;
//        public virtual DbSet<TblBill> TblBills { get; set; } = null!;
//        public virtual DbSet<TblDriver> TblDrivers { get; set; } = null!;
//        public virtual DbSet<TblFreight> TblFreights { get; set; } = null!;
//        public virtual DbSet<TblOwner> TblOwners { get; set; } = null!;
//        public virtual DbSet<TblReceiptTran> TblReceiptTrans { get; set; } = null!;
//        //public virtual DbSet<Sp_QtyByMonth> Sp_QtyByMonth { get; set; } = null!;
//        public virtual DbSet<TblTrip> TblTrips { get; set; } = null!;
//        public virtual DbSet<TblUser> TblUsers { get; set; } = null!;
//        public virtual DbSet<TblVehicle> TblVehicles { get; set; } = null!;
//        public virtual DbSet<TblVendor> TblVendors { get; set; } = null!;

////        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
////        {
////            if (!optionsBuilder.IsConfigured)
////            {
////#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
////                optionsBuilder.UseSqlServer("Server=MusaKhan\\SQLEXPRESS;Database=CodeFirstDb;Trusted_Connection=True;TrustServerCertificate=True;");
////            }
////        }

////        protected override void OnModelCreating(ModelBuilder modelBuilder)
////        {
////            modelBuilder.Entity<TblDestination>(entity =>
////            {
////                entity.HasKey(e => e.DestId)
////                    .HasName("PK_tblDestination");

////                entity.ToTable("TblDestination");

////                entity.Property(e => e.DestId).ValueGeneratedNever();

////                entity.Property(e => e.CityName)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.CreatedOn)
////                    .HasColumnType("datetime")
////                    .HasDefaultValueSql("(getdate())");

////                entity.Property(e => e.ModifiedOn)
////                    .HasColumnType("datetime")
////                    .HasDefaultValueSql("(getdate())");

////                entity.Property(e => e.StateName)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);
////            });

////            modelBuilder.Entity<TblDriver>(entity =>
////            {
////                entity.HasKey(e => e.Did);

////                entity.ToTable("TblDriver");

////                entity.Property(e => e.CreatedOn).HasColumnType("date");

////                entity.Property(e => e.DriverCode)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.DriverName)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.LicType)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.LicenseNo)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.MobileNo)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.ModifiedOn).HasColumnType("date");
////            });

////            modelBuilder.Entity<TblFreight>(entity =>
////            {
////                entity.HasKey(e => e.DestId);

////                entity.ToTable("TblFreight");

////                entity.Property(e => e.CompanyName)
////                    .HasMaxLength(255)
////                    .IsUnicode(false);

////                entity.Property(e => e.Destination)
////                    .HasMaxLength(255)
////                    .IsUnicode(false);

////                entity.Property(e => e.Quantity)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.Wheels)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);
////            });

////            modelBuilder.Entity<TblOwner>(entity =>
////            {
////                entity.HasKey(e => e.OwnerId)
////                    .HasName("PK_tblOwner");

////                entity.ToTable("TblOwner");

////                entity.Property(e => e.Address)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.CityName)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.MobileNo)
////                    .HasMaxLength(10)
////                    .IsUnicode(false);

////                entity.Property(e => e.OwnerName)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.StateName)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);
////            });

////            modelBuilder.Entity<TblReceiptTran>(entity =>
////            {
////                entity.HasKey(e => e.TransId)
////                    .HasName("PK__TblRecei__9E5DDB3C91D4E95D");

////                entity.Property(e => e.Cgst).HasColumnName("CGST");

////                entity.Property(e => e.CreatedOn).HasColumnType("date");

////                entity.Property(e => e.InvoiceNum)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.ModifiedOn).HasColumnType("date");

////                entity.Property(e => e.PaidOn).HasColumnType("date");

////                entity.Property(e => e.Rvid).HasColumnName("RVID");

////                entity.Property(e => e.Tds).HasColumnName("TDS");

////                entity.Property(e => e.TransDate).HasColumnType("date");
////            });

////            modelBuilder.Entity<TblTrip>(entity =>
////            {
////                entity.HasKey(e => e.TripId)
////                    .HasName("PK_tblTrips");

////                entity.Property(e => e.ChallanNo)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);
////            });

////            modelBuilder.Entity<TblUser>(entity =>
////            {
////                entity.HasKey(e => e.UserId)
////                    .HasName("PK_tblUser");

////                entity.ToTable("TblUser");

////                entity.Property(e => e.Address)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.City)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.Doj)
////                    .HasColumnType("date")
////                    .HasColumnName("DOJ");

////                entity.Property(e => e.Email)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.LastLogIn).HasColumnType("datetime");

////                entity.Property(e => e.Password)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.Role)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.UserName)
////                    .HasMaxLength(100)
////                    .IsUnicode(false);
////            });

////            modelBuilder.Entity<TblVehicle>(entity =>
////            {
////                entity.HasKey(e => e.VechicleId)
////                    .HasName("PK_tblVehicle");

////                entity.ToTable("TblVehicle");

////                entity.Property(e => e.CreatedOn)
////                    .HasColumnType("datetime")
////                    .HasDefaultValueSql("(getdate())");

////                entity.Property(e => e.FuelType)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");

////                entity.Property(e => e.LoadCapacity)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.ModifiedOn)
////                    .HasColumnType("datetime")
////                    .HasDefaultValueSql("(getdate())");

////                entity.Property(e => e.VechicleType)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.VechileNo)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);
////            });

////            modelBuilder.Entity<TblVendor>(entity =>
////            {
////                entity.HasKey(e => e.Vid)
////                    .HasName("PK__TblVendo__C5DF22BB0812DED8");

////                entity.ToTable("TblVendor");

////                entity.Property(e => e.Vid).HasColumnName("VID");

////                entity.Property(e => e.Address)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.Code)
////                    .HasMaxLength(50)
////                    .IsUnicode(false);

////                entity.Property(e => e.CreatedOn)
////                    .HasColumnType("datetime")
////                    .HasDefaultValueSql("(getdate())");

////                entity.Property(e => e.Description)
////                    .HasMaxLength(200)
////                    .IsUnicode(false);

////                entity.Property(e => e.Gstin).HasColumnName("GSTIN");

////                entity.Property(e => e.ModifiedOn)
////                    .HasColumnType("datetime")
////                    .HasDefaultValueSql("(getdate())");
////            });

////            OnModelCreatingPartial(modelBuilder);
////        }

////        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
//    }
//}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

public partial class MilkTeaContext : DbContext
{
    public MilkTeaContext()
    {
    }

    public MilkTeaContext(DbContextOptions<MilkTeaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }

    public virtual DbSet<ChiTietTopping> ChiTietToppings { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<DoUong> DoUongs { get; set; }

    public virtual DbSet<DoUongSize> DoUongSizes { get; set; }

    public virtual DbSet<DonHang> DonHangs { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<NhaCungCap> NhaCungCaps { get; set; }

    public virtual DbSet<SizeTb> SizeTbs { get; set; }

    public virtual DbSet<Topping> Toppings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("workstation id=milktea16.mssql.somee.com;packet size=4096;user id=nnsang_SQLLogin_1;pwd=tivor4kg3l;data source=milktea16.mssql.somee.com;persist security info=False;initial catalog=milktea16;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietDonHang>(entity =>
        {
            entity.HasKey(e => e.MaChiTiet).HasName("PK__ChiTietD__CDF0A11438E981FD");

            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaDoUongNavigation).WithMany(p => p.ChiTietDonHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTiet_DonHang_DoUong");

            entity.HasOne(d => d.MaDonHangNavigation).WithMany(p => p.ChiTietDonHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTiet_DonHang_DonHang");

            entity.HasOne(d => d.MaSizeNavigation).WithMany(p => p.ChiTietDonHangs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTiet_DonHang_Size");
        });

        modelBuilder.Entity<ChiTietTopping>(entity =>
        {
            entity.HasKey(e => e.MaChiTietTopping).HasName("PK__ChiTietT__FE455B135BAADA73");

            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaChiTietNavigation).WithMany(p => p.ChiTietToppings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietTopping_ChiTiet");

            entity.HasOne(d => d.MaToppingNavigation).WithMany(p => p.ChiTietToppings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChiTietTopping_Topping");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.MaDanhMuc).HasName("PK__DanhMuc__B375088704F30D19");
        });

        modelBuilder.Entity<DoUong>(entity =>
        {
            entity.HasKey(e => e.MaDoUong).HasName("PK__DoUong__D17CF24E6D8A4E88");

            entity.Property(e => e.Moi).HasDefaultValue(false);
            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<DoUongSize>(entity =>
        {
            entity.HasOne(d => d.MaDoUongNavigation).WithMany(p => p.DoUongSizes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DoUongSize_DoUong");

            entity.HasOne(d => d.MaSizeNavigation).WithMany(p => p.DoUongSizes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DoUongSize_Size");
        });

        modelBuilder.Entity<DonHang>(entity =>
        {
            entity.HasKey(e => e.MaDonHang).HasName("PK__DonHang__129584ADFFFEDCE5");

            entity.Property(e => e.DaThanhToan).HasDefaultValue(false);
            entity.Property(e => e.NgayDat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TinhTrangGiaoHang).HasDefaultValue(0);

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK_DonHang_KhachHang");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KhachHan__2725CF1E976901AE");
        });

        modelBuilder.Entity<NhaCungCap>(entity =>
        {
            entity.HasKey(e => e.MaNcc).HasName("PK__NhaCungC__3A185DEB69CB318A");
        });

        modelBuilder.Entity<SizeTb>(entity =>
        {
            entity.HasKey(e => e.MaSize).HasName("PK__SizeTB__A787E7ED58ABC848");
        });

        modelBuilder.Entity<Topping>(entity =>
        {
            entity.HasKey(e => e.MaTopping).HasName("PK__Topping__33C2FC61BE6F8C82");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

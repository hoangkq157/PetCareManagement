using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Models;

namespace PetCareManagement.Data;

public partial class PetCareDbContext : DbContext
{
    public PetCareDbContext()
    {
    }

    public PetCareDbContext(DbContextOptions<PetCareDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChuNuoi> ChuNuois { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<LichHen> LichHens { get; set; }

    public virtual DbSet<LichHenDichVu> LichHenDichVus { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<ThuCung> ThuCungs { get; set; }

    public virtual DbSet<TiemPhong> TiemPhongs { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChuNuoi>(entity =>
        {
            entity.HasKey(e => e.MaCn).HasName("PK__ChuNuoi__27258E0EC1D71451");

            entity.Property(e => e.NgayDangKy).HasDefaultValueSql("(CONVERT([date],getdate()))");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDv).HasName("PK__DichVu__272586579879F12E");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK__HoaDon__2725A6E0D6037B33");

            entity.Property(e => e.NgayLap).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.TrangThaiTt).HasDefaultValue("ChuaThanhToan");

            entity.HasOne(d => d.MaCnNavigation).WithMany(p => p.HoaDons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoaDon_ChuNuoi");

            entity.HasOne(d => d.MaLhNavigation).WithMany(p => p.HoaDons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoaDon_LichHen");
        });

        modelBuilder.Entity<LichHen>(entity =>
        {
            entity.HasKey(e => e.MaLh).HasName("PK__LichHen__2725C77FE6E51A6A");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue("ChoDuyet");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.LichHens).HasConstraintName("FK_LichHen_NhanVien");

            entity.HasOne(d => d.MaTcNavigation).WithMany(p => p.LichHens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichHen_ThuCung");
        });

        modelBuilder.Entity<LichHenDichVu>(entity =>
        {
            entity.HasKey(e => e.MaLhdv).HasName("PK__LichHen___649537010EBD20EE");

            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.LichHenDichVus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LHDV_DichVu");

            entity.HasOne(d => d.MaLhNavigation).WithMany(p => p.LichHenDichVus).HasConstraintName("FK_LHDV_LichHen");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70ACEFB3E98");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
            entity.Property(e => e.VaiTro).HasDefaultValue("NhanVien");
        });

        modelBuilder.Entity<ThuCung>(entity =>
        {
            entity.HasKey(e => e.MaTc).HasName("PK__ThuCung__27250068CBAAB644");

            entity.HasOne(d => d.MaCnNavigation).WithMany(p => p.ThuCungs).HasConstraintName("FK_ThuCung_ChuNuoi");
        });

        modelBuilder.Entity<TiemPhong>(entity =>
        {
            entity.HasKey(e => e.MaTp).HasName("PK__TiemPhon__2725007D6FBCD213");

            entity.Property(e => e.ChuKyNgay).HasDefaultValue(365);

            entity.HasOne(d => d.MaTcNavigation).WithMany(p => p.TiemPhongs).HasConstraintName("FK_TiemPhong_ThuCung");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

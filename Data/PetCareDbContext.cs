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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChuNuoi>(entity =>
        {
            entity.HasKey(e => e.MaCn).HasName("PK_ChuNuoi");

            entity.HasIndex(e => e.SoDienThoai)
                  .IsUnique()
                  .HasDatabaseName("UQ_ChuNuoi_SoDT");

            entity.Property(e => e.NgayDangKy).HasDefaultValueSql("(CONVERT([date],getdate()))");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDv).HasName("PK_DichVu");

            entity.Property(e => e.TrangThai).HasDefaultValue(true);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK_HoaDon");

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
            entity.HasKey(e => e.MaLh).HasName("PK_LichHen");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue("ChoDuyet");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.LichHens).HasConstraintName("FK_LichHen_NhanVien");

            entity.HasOne(d => d.MaTcNavigation).WithMany(p => p.LichHens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichHen_ThuCung");
        });

        modelBuilder.Entity<LichHenDichVu>(entity =>
        {
            entity.HasKey(e => e.MaLhdv).HasName("PK_LichHen_DichVu");

            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.LichHenDichVus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LHDV_DichVu");

            entity.HasOne(d => d.MaLhNavigation).WithMany(p => p.LichHenDichVus).HasConstraintName("FK_LHDV_LichHen");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK_NhanVien");

            entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue(true);
            entity.Property(e => e.VaiTro).HasDefaultValue("NhanVien");
        });

        modelBuilder.Entity<ThuCung>(entity =>
        {
            entity.HasKey(e => e.MaTc).HasName("PK_ThuCung");

            entity.HasOne(d => d.MaCnNavigation).WithMany(p => p.ThuCungs).HasConstraintName("FK_ThuCung_ChuNuoi");
        });

        modelBuilder.Entity<TiemPhong>(entity =>
        {
            entity.HasKey(e => e.MaTp).HasName("PK_TiemPhong");

            entity.Property(e => e.ChuKyNgay).HasDefaultValue(365);

            entity.HasOne(d => d.MaTcNavigation).WithMany(p => p.TiemPhongs).HasConstraintName("FK_TiemPhong_ThuCung");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
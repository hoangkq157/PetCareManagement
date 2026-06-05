using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("HoaDon")]
public partial class HoaDon
{
    [Key]
    [Column("MaHD")]
    public int MaHd { get; set; }

    [Column("MaLH")]
    public int MaLh { get; set; }

    [Column("MaCN")]
    public int MaCn { get; set; }

    public DateOnly NgayLap { get; set; }

    [Column(TypeName = "decimal(12, 0)")]
    public decimal TongTien { get; set; }

    [Column("TrangThaiTT")]
    [StringLength(30)]
    public string TrangThaiTt { get; set; } = null!;

    [Column("PhuongThucTT")]
    [StringLength(30)]
    public string? PhuongThucTt { get; set; }

    [Column(TypeName = "decimal(12, 0)")]
    public decimal? SoTienKhachTra { get; set; }

    [Column(TypeName = "decimal(12, 0)")]
    public decimal? TienThua { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    [StringLength(200)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaCn")]
    [InverseProperty("HoaDons")]
    public virtual ChuNuoi MaCnNavigation { get; set; } = null!;

    [ForeignKey("MaLh")]
    [InverseProperty("HoaDons")]
    public virtual LichHen MaLhNavigation { get; set; } = null!;
}

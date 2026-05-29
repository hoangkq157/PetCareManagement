using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("ThuCung")]
public partial class ThuCung
{
    [Key]
    [Column("MaTC")]
    public int MaTc { get; set; }

    [Column("MaCN")]
    public int MaCn { get; set; }

    [StringLength(50)]
    public string TenThuCung { get; set; } = null!;

    [StringLength(30)]
    public string Loai { get; set; } = null!;

    [StringLength(50)]
    public string? Giong { get; set; }

    public DateOnly? NgaySinh { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? CanNang { get; set; }

    [StringLength(30)]
    public string? MauLong { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [InverseProperty("MaTcNavigation")]
    public virtual ICollection<LichHen> LichHens { get; set; } = new List<LichHen>();

    [ForeignKey("MaCn")]
    [InverseProperty("ThuCungs")]
    public virtual ChuNuoi MaCnNavigation { get; set; } = null!;

    [InverseProperty("MaTcNavigation")]
    public virtual ICollection<TiemPhong> TiemPhongs { get; set; } = new List<TiemPhong>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("LichHen")]
public partial class LichHen
{
    [Key]
    [Column("MaLH")]
    public int MaLh { get; set; }

    [Column("MaTC")]
    public int MaTc { get; set; }

    [Column("MaNV")]
    public int? MaNv { get; set; }

    public DateOnly NgayHen { get; set; }

    public TimeOnly GioHen { get; set; }

    [StringLength(30)]
    public string TrangThai { get; set; } = null!;

    [StringLength(300)]
    public string? GhiChu { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [InverseProperty("MaLhNavigation")]
    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    [InverseProperty("MaLhNavigation")]
    public virtual ICollection<LichHenDichVu> LichHenDichVus { get; set; } = new List<LichHenDichVu>();

    [ForeignKey("MaNv")]
    [InverseProperty("LichHens")]
    public virtual NhanVien? MaNvNavigation { get; set; }

    [ForeignKey("MaTc")]
    [InverseProperty("LichHens")]
    public virtual ThuCung MaTcNavigation { get; set; } = null!;
}

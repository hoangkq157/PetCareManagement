using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("TiemPhong")]
public partial class TiemPhong
{
    [Key]
    [Column("MaTP")]
    public int MaTp { get; set; }

    [Column("MaTC")]
    public int MaTc { get; set; }

    [StringLength(100)]
    public string TenVaccine { get; set; } = null!;

    public DateOnly NgayTiem { get; set; }

    public DateOnly? NgayTiemTiep { get; set; }

    public int ChuKyNgay { get; set; }

    [StringLength(50)]
    public string? LieuLuong { get; set; }

    [StringLength(100)]
    public string? BacSiThucHien { get; set; }

    [StringLength(300)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaTc")]
    [InverseProperty("TiemPhongs")]
    public virtual ThuCung MaTcNavigation { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("ChuNuoi")]
public partial class ChuNuoi
{
    [Key]
    [Column("MaCN")]
    public int MaCn { get; set; }

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(15)]
    [Unicode(false)]
    public string SoDienThoai { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string? MatKhau { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? DiaChi { get; set; }

    public DateOnly NgayDangKy { get; set; }

    [InverseProperty("MaCnNavigation")]
    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    [InverseProperty("MaCnNavigation")]
    public virtual ICollection<ThuCung> ThuCungs { get; set; } = new List<ThuCung>();
}
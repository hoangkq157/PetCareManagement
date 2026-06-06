using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("NhanVien")]
[Index("Email", Name = "UQ_NhanVien_Email", IsUnique = true)]
public partial class NhanVien
{
    [Key]
    [Column("MaNV")]
    public int MaNv { get; set; }

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string MatKhau { get; set; } = null!;

    [StringLength(20)]
    public string VaiTro { get; set; } = null!;

    [StringLength(15)]
    [Unicode(false)]
    public string? SoDienThoai { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("MaNvNavigation")]
    public virtual ICollection<LichHen> LichHens { get; set; } = new List<LichHen>();
}
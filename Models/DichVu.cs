using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("DichVu")]
public partial class DichVu
{
    [Key]
    [Column("MaDV")]
    public int MaDv { get; set; }

    [StringLength(100)]
    public string TenDichVu { get; set; } = null!;

    [StringLength(50)]
    public string? DanhMuc { get; set; }

    [Column(TypeName = "decimal(10, 0)")]
    public decimal GiaCho { get; set; }

    [Column(TypeName = "decimal(10, 0)")]
    public decimal GiaMeo { get; set; }

    [Column(TypeName = "decimal(10, 0)")]
    public decimal GiaKhac { get; set; }

    [StringLength(300)]
    public string? MoTa { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("MaDvNavigation")]
    public virtual ICollection<LichHenDichVu> LichHenDichVus { get; set; } = new List<LichHenDichVu>();
}

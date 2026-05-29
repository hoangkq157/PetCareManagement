using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Models;

[Table("LichHen_DichVu")]
public partial class LichHenDichVu
{
    [Key]
    [Column("MaLHDV")]
    public int MaLhdv { get; set; }

    [Column("MaLH")]
    public int MaLh { get; set; }

    [Column("MaDV")]
    public int MaDv { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(10, 0)")]
    public decimal DonGia { get; set; }

    [ForeignKey("MaDv")]
    [InverseProperty("LichHenDichVus")]
    public virtual DichVu MaDvNavigation { get; set; } = null!;

    [ForeignKey("MaLh")]
    [InverseProperty("LichHenDichVus")]
    public virtual LichHen MaLhNavigation { get; set; } = null!;
}

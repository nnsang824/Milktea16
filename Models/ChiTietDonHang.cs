using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("ChiTietDonHang")]
public partial class ChiTietDonHang
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaDonHang { get; set; }

    public int MaDoUong { get; set; }

    public int MaSize { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal DonGia { get; set; }

    [InverseProperty("MaChiTietNavigation")]
    public virtual ICollection<ChiTietTopping> ChiTietToppings { get; set; } = new List<ChiTietTopping>();

    [ForeignKey("MaDoUong")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual DoUong MaDoUongNavigation { get; set; } = null!;

    [ForeignKey("MaDonHang")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual DonHang MaDonHangNavigation { get; set; } = null!;

    [ForeignKey("MaSize")]
    [InverseProperty("ChiTietDonHangs")]
    public virtual SizeTb MaSizeNavigation { get; set; } = null!;
}

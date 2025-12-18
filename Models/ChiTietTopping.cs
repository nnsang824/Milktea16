using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("ChiTietTopping")]
public partial class ChiTietTopping
{
    [Key]
    public int MaChiTietTopping { get; set; }

    public int MaChiTiet { get; set; }

    public int MaTopping { get; set; }

    public int SoLuong { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal DonGia { get; set; }

    [ForeignKey("MaChiTiet")]
    [InverseProperty("ChiTietToppings")]
    public virtual ChiTietDonHang MaChiTietNavigation { get; set; } = null!;

    [ForeignKey("MaTopping")]
    [InverseProperty("ChiTietToppings")]
    public virtual Topping MaToppingNavigation { get; set; } = null!;
}

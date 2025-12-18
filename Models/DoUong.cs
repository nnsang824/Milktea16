using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("DoUong")]
public partial class DoUong
{
    [Key]
    public int MaDoUong { get; set; }

    [StringLength(200)]
    public string TenDoUong { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? HinhAnh { get; set; }

    public int? SoLuongTon { get; set; }

    public int? MaDanhMuc { get; set; }

    [Column("MaNCC")]
    public int? MaNcc { get; set; }

    public bool? Moi { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayCapNhat { get; set; }

    [InverseProperty("MaDoUongNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [InverseProperty("MaDoUongNavigation")]
    public virtual ICollection<DoUongSize> DoUongSizes { get; set; } = new List<DoUongSize>();
}

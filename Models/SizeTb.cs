using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("SizeTB")]
public partial class SizeTb
{
    [Key]
    public int MaSize { get; set; }

    [StringLength(10)]
    public string TenSize { get; set; } = null!;

    [InverseProperty("MaSizeNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [InverseProperty("MaSizeNavigation")]
    public virtual ICollection<DoUongSize> DoUongSizes { get; set; } = new List<DoUongSize>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[PrimaryKey("MaDoUong", "MaSize")]
[Table("DoUong_Size")]
public partial class DoUongSize
{
    [Key]
    public int MaDoUong { get; set; }

    [Key]
    public int MaSize { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal Gia { get; set; }

    [ForeignKey("MaDoUong")]
    [InverseProperty("DoUongSizes")]
    public virtual DoUong MaDoUongNavigation { get; set; } = null!;

    [ForeignKey("MaSize")]
    [InverseProperty("DoUongSizes")]
    public virtual SizeTb MaSizeNavigation { get; set; } = null!;
}

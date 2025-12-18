using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("NhaCungCap")]
public partial class NhaCungCap
{
    [Key]
    [Column("MaNCC")]
    public int MaNcc { get; set; }

    [Column("TenNCC")]
    [StringLength(100)]
    public string? TenNcc { get; set; }

    [StringLength(200)]
    public string? DiaChi { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? DienThoai { get; set; }
}

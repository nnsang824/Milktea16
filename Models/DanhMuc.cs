using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("DanhMuc")]
public partial class DanhMuc
{
    [Key]
    public int MaDanhMuc { get; set; }

    [StringLength(100)]
    public string TenDanhMuc { get; set; } = null!;
}

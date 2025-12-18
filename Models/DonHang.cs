using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("DonHang")]
public partial class DonHang
{
    [Key]
    public int MaDonHang { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayDat { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayGiao { get; set; }

    public bool? DaThanhToan { get; set; }

    public int? TinhTrangGiaoHang { get; set; }

    [Column("MaKH")]
    public int? MaKh { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [InverseProperty("MaDonHangNavigation")]
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [ForeignKey("MaKh")]
    [InverseProperty("DonHangs")]
    public virtual KhachHang? MaKhNavigation { get; set; }
}

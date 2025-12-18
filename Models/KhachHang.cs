using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Models;

[Table("KhachHang")]
public partial class KhachHang
{
    [Key]
    [Column("MaKH")]
    public int MaKh { get; set; }

    [StringLength(100)]
    public string? HoTen { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgaySinh { get; set; }

    [StringLength(10)]
    public string? GioiTinh { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? DienThoai { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? TaiKhoan { get; set; }

    [StringLength(200)]
    public string? MatKhau { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? DiaChi { get; set; }

    [InverseProperty("MaKhNavigation")]
    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}

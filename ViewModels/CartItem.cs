using System.Collections.Generic;
using System.Linq;

namespace N16_MilkTea.ViewModels
{
    public class CartItem
    {
        public int MaDoUong { get; set; }
        public string? TenDoUong { get; set; } // Thêm dấu ?
        public string? HinhAnh { get; set; }   // Thêm dấu ?
        
        public int MaSize { get; set; }
        public string? TenSize { get; set; }   // Thêm dấu ?
        
        public decimal DonGia { get; set; } 
        public int SoLuong { get; set; }

        public List<CartTopping> Toppings { get; set; } = new List<CartTopping>();

        public decimal ThanhTien => (DonGia + Toppings.Sum(t => t.Gia)) * SoLuong;
    }

    public class CartTopping
    {
        public int MaTopping { get; set; }
        public string? TenTopping { get; set; } // Thêm dấu ?
        public decimal Gia { get; set; }
    }
}
using N16_MilkTea.Models;

namespace N16_MilkTea.ViewModels
{
    public class AdminOrderViewModel
    {
        // Dữ liệu danh sách đơn hàng (để hiện bảng)
        public List<DonHang> Orders { get; set; }
        
        // Dữ liệu phân trang
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        
        // Dữ liệu biểu đồ (Thống kê doanh thu 7 ngày gần nhất)
        public string[] ChartLabels { get; set; } // Ngày
        public decimal[] ChartValues { get; set; } // Tiền
    }
}
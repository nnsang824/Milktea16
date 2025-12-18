using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;
using System.Diagnostics;
// using N16_MilkTea.Helpers; // Tạm tắt nếu không dùng
using System.Linq; 

namespace N16_MilkTea.Controllers
{
    public class HomeController : Controller
    {
        private readonly MilkTeaContext _context;

        public HomeController(MilkTeaContext context)
        {
            _context = context;
        }

        // --- 1. TRANG CHỦ (HIỆN 6 MÓN TIÊU BIỂU) ---
        public async Task<IActionResult> Index(string? query, int? danhMucId)
        {
            // Kiểm tra DB context
            if (_context.DoUongs == null) return Problem("Entity set 'MilkTeaContext.DoUongs' is null.");

            var products = _context.DoUongs
                .Include(d => d.DoUongSizes) 
                .AsQueryable();

            // Logic tìm kiếm & Lọc
            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p => p.TenDoUong.Contains(query));
                ViewBag.Keyword = query;
            }

            if (danhMucId.HasValue)
            {
                products = products.Where(p => p.MaDanhMuc == danhMucId);
            }

            // Sắp xếp món mới nhất lên đầu
            products = products.OrderByDescending(p => p.MaDoUong);

            // Nếu không tìm kiếm gì cả, chỉ lấy 6 món tiêu biểu
            if (string.IsNullOrEmpty(query) && !danhMucId.HasValue)
            {
                products = products.Take(6);
            }

            // Kiểm tra DanhMucs null tránh lỗi view
            if (_context.DanhMucs != null)
            {
                ViewBag.DanhMucs = await _context.DanhMucs.ToListAsync();
            }
            
            return View(await products.ToListAsync());
        }

        // --- 2. CHI TIẾT SẢN PHẨM ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.DoUongs == null) return NotFound();

            var doUong = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                    .ThenInclude(ds => ds.MaSizeNavigation)
                .FirstOrDefaultAsync(m => m.MaDoUong == id);

            if (doUong == null) return NotFound();

            if (_context.Toppings != null)
            {
                ViewBag.Toppings = await _context.Toppings.ToListAsync();
            }
            
            return View(doUong);
        }

        // --- 3. TRANG THỰC ĐƠN (HIỆN 20 MÓN) ---
        public async Task<IActionResult> Menu()
        {
            if (_context.DoUongs == null) return NotFound();

            // Lấy 20 món mới nhất
            var products = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                .OrderByDescending(d => d.MaDoUong)
                .Take(20) 
                .ToListAsync();

            if (_context.DanhMucs != null)
            {
                ViewBag.DanhMucs = await _context.DanhMucs.ToListAsync();
            }

            return View(products);
        }

        // --- 4. TRANG KHUYẾN MÃI ---
        public IActionResult Promotions()
        {
            return View();
        }

        // --- CÁC HÀM GIỎ HÀNG (AddToCart, Cart, Checkout...) ĐÃ ĐƯỢC CHUYỂN SANG CARTCONTROLLER 
        // ĐỂ TRÁNH LỖI XUNG ĐỘT CODE ---
    }
}
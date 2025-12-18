using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;
using Microsoft.AspNetCore.Http; 
using System.IO; 

namespace N16_MilkTea.Controllers
{
    public class AdminController : Controller
    {
        private readonly MilkTeaContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment; 

        public AdminController(MilkTeaContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
        }

        // ====================================================================
        // 1. DASHBOARD & LOGIN
        // ====================================================================
        
        // Trang chính Admin - Thống kê doanh thu
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            // Lấy dữ liệu về RAM để xử lý (Tránh lỗi GroupBy của EF Core cũ)
            var donHangs = _context.DonHangs
                .Where(d => d.DaThanhToan == true)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.ChiTietToppings)
                .ToList();

            var revenueData = donHangs
                .GroupBy(d => d.NgayDat.HasValue ? d.NgayDat.Value.Date : DateTime.Now.Date)
                .Select(g => new { 
                    Date = g.Key, 
                    // Tính tổng tiền = (Giá món * SL) + Giá Topping
                    Revenue = g.Sum(d => 
                        d.ChiTietDonHangs.Sum(ct => (ct.DonGia * ct.SoLuong) + ct.ChiTietToppings.Sum(t => t.DonGia))
                    )
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.Dates = revenueData.Select(x => x.Date.ToString("dd/MM")).ToArray();
            ViewBag.Revenues = revenueData.Select(x => x.Revenue).ToArray();

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var adminUser = _configuration["AdminAccount:Username"];
            var adminPass = _configuration["AdminAccount:Password"];

            if (username == adminUser && password == adminPass)
            {
                HttpContext.Session.SetString("AdminUser", username);
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Sai thông tin đăng nhập!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminUser");
            return RedirectToAction("Login");
        }

        // ====================================================================
        // 2. QUẢN LÝ ĐƠN HÀNG (ORDERS)
        // ====================================================================
        public async Task<IActionResult> Orders(int page = 1, string searchName = "", string fromDate = "", string toDate = "")
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            int pageSize = 10;
            var query = _context.DonHangs
                .Include(d => d.MaKhNavigation) 
                .OrderByDescending(d => d.NgayDat) 
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchName))
            {
                searchName = searchName.Trim().ToLower();
                query = query.Where(d => 
                    d.MaDonHang.ToString().Contains(searchName) ||
                    (d.MaKhNavigation != null && d.MaKhNavigation.HoTen.ToLower().Contains(searchName))
                );
            }

            // Lọc ngày
            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out DateTime dtFrom))
            {
                query = query.Where(d => d.NgayDat >= dtFrom);
            }
            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out DateTime dtTo))
            {
                dtTo = dtTo.Date.AddDays(1).AddTicks(-1);
                query = query.Where(d => d.NgayDat <= dtTo);
            }

            int totalOrders = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.SearchName = searchName;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            var order = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.MaDoUongNavigation)
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.MaSizeNavigation)
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.ChiTietToppings).ThenInclude(t => t.MaToppingNavigation)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int MaDonHang, int TrangThai)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            var order = await _context.DonHangs.FindAsync(MaDonHang);
            if (order != null)
            {
                order.TinhTrangGiaoHang = TrangThai;
                if (TrangThai == 1) order.DaThanhToan = true; // Giao thành công = Đã thanh toán
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("OrderDetails", new { id = MaDonHang });
        }

        // ====================================================================
        // 3. QUẢN LÝ SẢN PHẨM (PRODUCTS) - CÓ XỬ LÝ ẢNH & GIÁ SIZE
        // ====================================================================
        public async Task<IActionResult> Products(int page = 1)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            int pageSize = 8;
            var query = _context.DoUongs
                .Include(d => d.DoUongSizes) // Kèm giá để hiển thị
                .OrderByDescending(d => d.MaDoUong);

            int totalItems = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.ListDanhMuc = await _context.DanhMucs.ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(products);
        }

        [HttpGet]
        public IActionResult CreateProduct()
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");
            ViewBag.DanhMucs = _context.DanhMucs.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(DoUong model, IFormFile? imageFile, decimal GiaSizeS)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            // 1. Lưu ảnh vào wwwroot/images
            if (imageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string uploadPath = Path.Combine(_environment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                model.HinhAnh = fileName;
            }

            // 2. Lưu thông tin cơ bản
            model.NgayCapNhat = DateTime.Now;
            model.Moi = true;
            _context.DoUongs.Add(model);
            await _context.SaveChangesAsync(); // Lưu để có ID

            // 3. Tự động tạo 3 mức giá (S, M, L)
            
            var listGia = new List<DoUongSize>
            {
                new DoUongSize { MaDoUong = model.MaDoUong, MaSize = 1, Gia = GiaSizeS },        // S
                new DoUongSize { MaDoUong = model.MaDoUong, MaSize = 2, Gia = GiaSizeS + 6000 }, // M (+6k)
                new DoUongSize { MaDoUong = model.MaDoUong, MaSize = 3, Gia = GiaSizeS + 12000 } // L (+12k)
            };
            _context.DoUongSizes.AddRange(listGia);
            await _context.SaveChangesAsync();

            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            var p = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                .FirstOrDefaultAsync(x => x.MaDoUong == id);

            if (p == null) return NotFound();

            ViewBag.DanhMucs = _context.DanhMucs.ToList();

            // Lấy giá hiện tại để điền vào form
            ViewBag.GiaS = p.DoUongSizes.FirstOrDefault(s => s.MaSize == 1)?.Gia ?? 0;
            ViewBag.GiaM = p.DoUongSizes.FirstOrDefault(s => s.MaSize == 2)?.Gia ?? 0;
            ViewBag.GiaL = p.DoUongSizes.FirstOrDefault(s => s.MaSize == 3)?.Gia ?? 0;

            return View(p);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(int id, DoUong model, IFormFile? imageFile, decimal GiaS, decimal GiaM, decimal GiaL)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            var existingProduct = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                .FirstOrDefaultAsync(x => x.MaDoUong == id);

            if (existingProduct == null) return NotFound();

            existingProduct.TenDoUong = model.TenDoUong;
            existingProduct.MoTa = model.MoTa;
            existingProduct.MaDanhMuc = model.MaDanhMuc;
            existingProduct.NgayCapNhat = DateTime.Now;

            if (imageFile != null)
            {
                // Upload ảnh mới
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string uploadPath = Path.Combine(_environment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                existingProduct.HinhAnh = fileName;
            }

            // Cập nhật giá từng size
            await CapNhatGiaSize(existingProduct, 1, GiaS);
            await CapNhatGiaSize(existingProduct, 2, GiaM);
            await CapNhatGiaSize(existingProduct, 3, GiaL);

            await _context.SaveChangesAsync();
            return RedirectToAction("Products");
        }

        private async Task CapNhatGiaSize(DoUong product, int maSize, decimal giaMoi)
        {
            var size = product.DoUongSizes.FirstOrDefault(s => s.MaSize == maSize);
            if (size != null) size.Gia = giaMoi;
            else _context.DoUongSizes.Add(new DoUongSize { MaDoUong = product.MaDoUong, MaSize = maSize, Gia = giaMoi });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            var p = await _context.DoUongs.FindAsync(id);
            if (p != null)
            {
                try 
                {
                    var prices = _context.DoUongSizes.Where(x => x.MaDoUong == id);
                    _context.DoUongSizes.RemoveRange(prices); // Xóa giá trước

                    _context.DoUongs.Remove(p); // Xóa sản phẩm sau
                    await _context.SaveChangesAsync();
                }
                catch 
                {
                    TempData["Error"] = "Không thể xóa vì món này đã có người đặt mua!";
                }
            }
            return RedirectToAction("Products");
        }
    }
}
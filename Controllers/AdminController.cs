using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;
using Microsoft.AspNetCore.Http; // Để xử lý file upload
using System.IO; // Để xử lý đường dẫn file

namespace N16_MilkTea.Controllers
{
    public class AdminController : Controller
    {
        private readonly MilkTeaContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment; // Dùng để lấy đường dẫn lưu ảnh

        public AdminController(MilkTeaContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
        }

        // --- 1. ĐĂNG NHẬP / ĐĂNG XUẤT ---
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
                return RedirectToAction("Orders");
            }

            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminUser");
            return RedirectToAction("Login");
        }

        // --- QUẢN LÝ ĐƠN HÀNG (CÓ TÌM KIẾM & PHÂN TRANG) ---
        public async Task<IActionResult> Orders(int page = 1, string searchName = "", string fromDate = "", string toDate = "")
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            int pageSize = 10;
            
            // 1. Khởi tạo query và Include bảng KhachHang để lấy tên
            var query = _context.DonHangs
                .Include(d => d.MaKhNavigation) 
                .OrderByDescending(d => d.NgayDat) // Mặc định sắp xếp mới nhất
                .AsQueryable();

            // 2. Xử lý tìm kiếm theo Tên hoặc Mã KH
            if (!string.IsNullOrEmpty(searchName))
            {
                searchName = searchName.Trim().ToLower();
                query = query.Where(d => 
                    // Tìm theo Mã Đơn
                    d.MaDonHang.ToString().Contains(searchName) ||
                    // Tìm theo Mã Khách (nếu có)
                    (d.MaKh != null && d.MaKh.ToString().Contains(searchName)) ||
                    // Tìm theo Tên Khách (nếu có)
                    (d.MaKhNavigation != null && d.MaKhNavigation.HoTen.ToLower().Contains(searchName)) ||
                    // Tìm trong Ghi chú (đối với khách vãng lai tên nằm trong ghi chú)
                    (d.GhiChu != null && d.GhiChu.ToLower().Contains(searchName))
                );
            }

            // 3. Xử lý tìm kiếm theo Ngày
            if (!string.IsNullOrEmpty(fromDate))
            {
                if(DateTime.TryParse(fromDate, out DateTime dtFrom))
                {
                    query = query.Where(d => d.NgayDat >= dtFrom);
                }
            }

            if (!string.IsNullOrEmpty(toDate))
            {
                if(DateTime.TryParse(toDate, out DateTime dtTo))
                {
                    // Lấy đến cuối ngày đó (23:59:59)
                    dtTo = dtTo.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(d => d.NgayDat <= dtTo);
                }
            }

            // 4. Tính toán phân trang
            int totalOrders = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // 5. Lưu lại giá trị tìm kiếm để hiện lại trên View
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
            var order = await _context.DonHangs.FindAsync(MaDonHang);
            if (order != null)
            {
                order.TinhTrangGiaoHang = TrangThai;
                if (TrangThai == 1) order.DaThanhToan = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("OrderDetails", new { id = MaDonHang });
        }

        // --- 3. QUẢN LÝ SẢN PHẨM (CRUD) ---

        // A. Danh sách sản phẩm (Có phân trang)
        public async Task<IActionResult> Products(int page = 1)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            int pageSize = 8;
            
            // 1. Lấy danh sách sản phẩm (BỎ Include MaDanhMucNavigation vì gây lỗi)
            var query = _context.DoUongs.OrderByDescending(d => d.MaDoUong);

            int totalItems = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // 2. Truyền danh sách DanhMuc sang View để tự tra cứu tên
            ViewBag.ListDanhMuc = await _context.DanhMucs.ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(products);
        }

        // B. Thêm sản phẩm (GET - Hiển thị form)
        [HttpGet]
        public IActionResult CreateProduct()
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");
            
            // Lấy danh sách danh mục để hiện dropdown
            ViewBag.DanhMucs = _context.DanhMucs.ToList();
            return View();
        }

        // B. Thêm sản phẩm (POST - Xử lý lưu)
        [HttpPost]
        public async Task<IActionResult> CreateProduct(DoUong model, IFormFile? imageFile, decimal GiaSizeS)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            // 1. Xử lý Upload ảnh
            if (imageFile != null)
            {
                // Tạo tên file ngẫu nhiên để tránh trùng
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
            // Nếu model.MaNCC null thì có thể gán mặc định hoặc để null tùy DB
            
            _context.DoUongs.Add(model);
            await _context.SaveChangesAsync(); // Lưu để sinh ra MaDoUong

            // 3. Tự động tạo giá cho 3 size (S, M, L) vào bảng DoUong_Size
            // Size S = Giá nhập; Size M = +6000; Size L = +12000
            var listGia = new List<DoUongSize>
            {
                new DoUongSize { MaDoUong = model.MaDoUong, MaSize = 1, Gia = GiaSizeS },        // Size S
                new DoUongSize { MaDoUong = model.MaDoUong, MaSize = 2, Gia = GiaSizeS + 6000 }, // Size M
                new DoUongSize { MaDoUong = model.MaDoUong, MaSize = 3, Gia = GiaSizeS + 12000 } // Size L
            };

            _context.DoUongSizes.AddRange(listGia);
            await _context.SaveChangesAsync();

            return RedirectToAction("Products");
        }

        // C. Sửa sản phẩm (GET)
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            // 1. Lấy thông tin món và KÈM THEO GIÁ (DoUongSizes)
            var p = await _context.DoUongs
                .Include(d => d.DoUongSizes) 
                .FirstOrDefaultAsync(x => x.MaDoUong == id);

            if (p == null) return NotFound();

            ViewBag.DanhMucs = _context.DanhMucs.ToList();

            // 2. Lấy giá hiện tại của từng Size để hiển thị ra View
            // Giả sử: Size 1=S, 2=M, 3=L (theo logic lúc Create)
            var sizeS = p.DoUongSizes.FirstOrDefault(s => s.MaSize == 1);
            var sizeM = p.DoUongSizes.FirstOrDefault(s => s.MaSize == 2);
            var sizeL = p.DoUongSizes.FirstOrDefault(s => s.MaSize == 3);

            ViewBag.GiaS = sizeS != null ? sizeS.Gia : 0;
            ViewBag.GiaM = sizeM != null ? sizeM.Gia : 0;
            ViewBag.GiaL = sizeL != null ? sizeL.Gia : 0;

            return View(p);
        }

        // C. Sửa sản phẩm (POST)
        [HttpPost]
        public async Task<IActionResult> EditProduct(int id, DoUong model, IFormFile? imageFile, decimal GiaS, decimal GiaM, decimal GiaL)
        {
            // Tìm món cũ
            var existingProduct = await _context.DoUongs
                .Include(d => d.DoUongSizes) // Nhớ Include để lấy được danh sách size cũ
                .FirstOrDefaultAsync(x => x.MaDoUong == id);

            if (existingProduct == null) return NotFound();

            // 1. Cập nhật thông tin cơ bản
            existingProduct.TenDoUong = model.TenDoUong;
            existingProduct.MoTa = model.MoTa;
            existingProduct.MaDanhMuc = model.MaDanhMuc;
            existingProduct.NgayCapNhat = DateTime.Now;

            // 2. Cập nhật ảnh (nếu có chọn ảnh mới)
            if (imageFile != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string uploadPath = Path.Combine(_environment.WebRootPath, "images", fileName);
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                existingProduct.HinhAnh = fileName;
            }

            // 3. CẬP NHẬT GIÁ TIỀN CHO TỪNG SIZE
            // Hàm cập nhật hoặc thêm mới nếu chưa có
            await CapNhatGiaSize(existingProduct, 1, GiaS); // Size S
            await CapNhatGiaSize(existingProduct, 2, GiaM); // Size M
            await CapNhatGiaSize(existingProduct, 3, GiaL); // Size L

            await _context.SaveChangesAsync();
            return RedirectToAction("Products");
        }

        // Hàm phụ trợ để code gọn hơn
        private async Task CapNhatGiaSize(DoUong product, int maSize, decimal giaMoi)
        {
            var size = product.DoUongSizes.FirstOrDefault(s => s.MaSize == maSize);
            if (size != null)
            {
                // Nếu đã có size này thì cập nhật giá
                size.Gia = giaMoi;
            }
            else
            {
                // Nếu chưa có (vd món cũ thiếu size) thì thêm mới
                var newSize = new DoUongSize 
                { 
                    MaDoUong = product.MaDoUong, 
                    MaSize = maSize, 
                    Gia = giaMoi 
                };
                _context.DoUongSizes.Add(newSize);
            }
        }

        // D. Xóa sản phẩm
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (HttpContext.Session.GetString("AdminUser") == null) return RedirectToAction("Login");

            var p = await _context.DoUongs.FindAsync(id);
            if (p != null)
            {
                // Bước 1: Xóa giá tiền trong bảng DoUong_Size trước (Khóa ngoại)
                var prices = _context.DoUongSizes.Where(x => x.MaDoUong == id);
                _context.DoUongSizes.RemoveRange(prices);

                // Bước 2: Xóa sản phẩm
                try 
                {
                    _context.DoUongs.Remove(p);
                    await _context.SaveChangesAsync();
                }
                catch 
                {
                    // Nếu lỗi do sản phẩm đã có trong đơn hàng cũ (ChiTietDonHang)
                    TempData["Error"] = "Không thể xóa món này vì đã có trong lịch sử đơn hàng của khách!";
                }
            }
            return RedirectToAction("Products");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using N16_MilkTea.Models;
using Microsoft.EntityFrameworkCore;

namespace N16_MilkTea.Controllers
{
    public class AccountController : Controller
    {
        private readonly MilkTeaContext _context;

        public AccountController(MilkTeaContext context)
        {
            _context = context;
        }

        // --- 1. ĐĂNG KÝ ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(KhachHang model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên đăng nhập đã tồn tại chưa
                var existingUser = await _context.KhachHangs
                    .FirstOrDefaultAsync(k => k.TaiKhoan == model.TaiKhoan);

                if (existingUser != null)
                {
                    ViewBag.Error = "Tên tài khoản này đã được sử dụng!";
                    return View(model);
                }

                // Gán ngày tạo mặc định nếu null
                model.NgaySinh ??= DateTime.Now;

                _context.KhachHangs.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // --- 2. ĐĂNG NHẬP (ĐÃ SỬA) ---
        [HttpGet]
        public IActionResult Login(string returnUrl = null) // Thêm tham số này
        {
            // Lưu lại URL muốn quay về để truyền sang View
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string TaiKhoan, string MatKhau, string returnUrl = null) // Thêm tham số này
        {
            var user = await _context.KhachHangs
                .SingleOrDefaultAsync(k => k.TaiKhoan == TaiKhoan && k.MatKhau == MatKhau);

            if (user != null)
            {
                // 1. Lưu Session như cũ
                HttpContext.Session.SetString("MaKh", user.MaKh.ToString());
                HttpContext.Session.SetString("TenKh", user.HoTen ?? "Khách hàng");
                HttpContext.Session.SetString("UserPhone", user.DienThoai ?? "");
                HttpContext.Session.SetString("UserAddress", user.DiaChi ?? "");
                HttpContext.Session.SetString("UserEmail", user.Email ?? "");

                // 2. LOGIC QUAN TRỌNG: Kiểm tra và quay lại trang cũ
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl); // Quay lại trang Giỏ hàng (hoặc trang trước đó)
                }

                return RedirectToAction("Index", "Home"); // Mặc định về trang chủ
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl; // Giữ lại URL nếu đăng nhập sai để lần sau nhập đúng vẫn quay lại được
            return View();
        }
        // --- 3. ĐĂNG XUẤT ---
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa toàn bộ session
            return RedirectToAction("Index", "Home");
        }
        // Xem lịch sử đơn hàng
        public async Task<IActionResult> History()
        {
            // Lấy mã khách từ Session
            var maKhString = HttpContext.Session.GetString("MaKh");
            if (maKhString == null) return RedirectToAction("Login");

            int maKh = int.Parse(maKhString);

            var listDonHang = await _context.DonHangs
                .Where(d => d.MaKh == maKh)
                .OrderByDescending(d => d.NgayDat) // Đơn mới nhất lên đầu
                .Include(d => d.ChiTietDonHangs) // Kèm chi tiết nếu muốn hiển thị
                .ToListAsync();

            return View(listDonHang);
        }
        // --- QUẢN LÝ THÔNG TIN CÁ NHÂN ---
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Login");

            var user = await _context.KhachHangs.FindAsync(int.Parse(maKhStr));
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(KhachHang model)
        {
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null) return RedirectToAction("Login");

            var user = await _context.KhachHangs.FindAsync(int.Parse(maKhStr));
            if (user != null)
            {
                // Cập nhật thông tin
                user.HoTen = model.HoTen;
                user.DienThoai = model.DienThoai;
                user.DiaChi = model.DiaChi;
                user.Email = model.Email;
                
                // Nếu người dùng nhập mật khẩu mới thì mới đổi
                if (!string.IsNullOrEmpty(model.MatKhau))
                {
                    user.MatKhau = model.MatKhau;
                }

                await _context.SaveChangesAsync();
                
                // Cập nhật lại Session
                HttpContext.Session.SetString("TenKh", user.HoTen);
                HttpContext.Session.SetString("UserPhone", user.DienThoai ?? "");
                HttpContext.Session.SetString("UserAddress", user.DiaChi ?? "");
                HttpContext.Session.SetString("UserEmail", user.Email ?? "");

                TempData["Success"] = "Cập nhật thông tin thành công!";
            }
            return RedirectToAction("Profile");
        }
    }
}
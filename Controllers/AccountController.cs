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

        // --- 2. ĐĂNG NHẬP ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string TaiKhoan, string MatKhau)
        {
            var user = await _context.KhachHangs
                .SingleOrDefaultAsync(k => k.TaiKhoan == TaiKhoan && k.MatKhau == MatKhau);

            if (user != null)
            {
                // Lưu thông tin vào Session
                HttpContext.Session.SetString("MaKh", user.MaKh.ToString());
                HttpContext.Session.SetString("TenKh", user.HoTen ?? "Khách hàng");
                
                // --- CÁC DÒNG MỚI THÊM ---
                // Lưu SĐT, Địa chỉ, Email để tự điền khi thanh toán
                HttpContext.Session.SetString("UserPhone", user.DienThoai ?? "");
                HttpContext.Session.SetString("UserAddress", user.DiaChi ?? "");
                HttpContext.Session.SetString("UserEmail", user.Email ?? ""); 
                // --------------------------

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return View();
        }
        // --- 3. ĐĂNG XUẤT ---
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa toàn bộ session
            return RedirectToAction("Index", "Home");
        }
    }
}
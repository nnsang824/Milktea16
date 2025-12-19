using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;
using System.Net;
using System.Net.Mail;

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
                var existingUser = await _context.KhachHangs
                    .FirstOrDefaultAsync(k => k.TaiKhoan == model.TaiKhoan);

                if (existingUser != null)
                {
                    ViewBag.Error = "Tên tài khoản này đã được sử dụng!";
                    return View(model);
                }

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
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string TaiKhoan, string MatKhau, string returnUrl = null)
        {
            var user = await _context.KhachHangs
                .SingleOrDefaultAsync(k => k.TaiKhoan == TaiKhoan && k.MatKhau == MatKhau);

            if (user != null)
            {
                HttpContext.Session.SetString("MaKh", user.MaKh.ToString());
                HttpContext.Session.SetString("TenKh", user.HoTen ?? "Khách hàng");
                HttpContext.Session.SetString("UserPhone", user.DienThoai ?? "");
                HttpContext.Session.SetString("UserAddress", user.DiaChi ?? "");
                HttpContext.Session.SetString("UserEmail", user.Email ?? "");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // --- 3. ĐĂNG XUẤT ---
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // --- 4. THÔNG TIN CÁ NHÂN ---
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
                user.HoTen = model.HoTen;
                user.DienThoai = model.DienThoai;
                user.DiaChi = model.DiaChi;
                user.Email = model.Email;
                
                if (!string.IsNullOrEmpty(model.MatKhau))
                {
                    user.MatKhau = model.MatKhau;
                }

                await _context.SaveChangesAsync();
                
                HttpContext.Session.SetString("TenKh", user.HoTen);
                HttpContext.Session.SetString("UserPhone", user.DienThoai ?? "");
                HttpContext.Session.SetString("UserAddress", user.DiaChi ?? "");
                HttpContext.Session.SetString("UserEmail", user.Email ?? "");

                TempData["Success"] = "Cập nhật thông tin thành công!";
            }
            return RedirectToAction("Profile");
        }
        // --- 5. LỊCH SỬ ĐƠN HÀNG ---
        public async Task<IActionResult> History()
        {
            // 1. Kiểm tra đăng nhập
            if (HttpContext.Session.GetString("MaKh") == null)
            {
                return RedirectToAction("Login");
            }

            // 2. Lấy MaKh từ Session
            int maKh = int.Parse(HttpContext.Session.GetString("MaKh"));

            // 3. Truy vấn DB lấy đơn hàng của khách đó
            var orders = await _context.DonHangs
                .Where(d => d.MaKh == maKh)
                .Include(d => d.ChiTietDonHangs) // Kèm chi tiết để tính tổng tiền
                .OrderByDescending(d => d.NgayDat) // Đơn mới nhất lên đầu
                .ToListAsync();

            return View(orders);
        }

        // Xem chi tiết một đơn hàng cụ thể trong lịch sử
        public async Task<IActionResult> HistoryDetails(int id)
        {
            if (HttpContext.Session.GetString("MaKh") == null) return RedirectToAction("Login");

            var order = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.MaDoUongNavigation)
                .Include(d => d.ChiTietDonHangs).ThenInclude(ct => ct.MaSizeNavigation) // Nếu có bảng Size
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // --- 6. QUÊN MẬT KHẨU (MỚI THÊM) ---
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            // Tìm user theo email
            var user = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email này chưa được đăng ký trong hệ thống!";
                return View();
            }

            // Tạo mật khẩu mới ngẫu nhiên (8 ký tự)
            string newPassword = Guid.NewGuid().ToString().Substring(0, 8);
            
            // Cập nhật vào DB
            user.MatKhau = newPassword;
            await _context.SaveChangesAsync();

            // Gửi mail
            bool guiThanhCong = await GuiEmailMatKhauMoi(email, newPassword);

            if (guiThanhCong)
            {
                TempData["Success"] = "Mật khẩu mới đã được gửi vào Email của bạn. Vui lòng kiểm tra (cả mục Spam).";
                return RedirectToAction("Login");
            }
            else
            {
                ViewBag.Error = "Lỗi gửi mail. Vui lòng thử lại sau.";
                return View();
            }
        }

        // --- HÀM GỬI EMAIL QUÊN MẬT KHẨU ---
        private async Task<bool> GuiEmailMatKhauMoi(string emailNhan, string matKhauMoi)
        {
            try
            {
                var fromAddress = new MailAddress("sangchuadao123@gmail.com", "N16 MilkTea");
                const string fromPassword = "ghwn wefe ofde ymlp"; // App Password của bạn
                
                var toAddress = new MailAddress(emailNhan);
                string subject = "Cấp lại mật khẩu - N16 MilkTea";
                string body = $"<h3>Mật khẩu mới của bạn là: <span style='color:red; font-size: 20px'>{matKhauMoi}</span></h3><p>Vui lòng đăng nhập và đổi lại mật khẩu ngay.</p>";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body, IsBodyHtml = true })
                {
                    await smtp.SendMailAsync(message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
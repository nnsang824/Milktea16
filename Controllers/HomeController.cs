using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;
using System.Diagnostics;
using N16_MilkTea.ViewModels;
using N16_MilkTea.Helpers;
using System.Net;
using System.Net.Mail;
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

        // --- 1. TRANG CHỦ (TÌM KIẾM + LỌC) ---
        public async Task<IActionResult> Index(string? query, int? danhMucId)
        {
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

            // [THAY ĐỔI] Nếu không tìm kiếm gì cả, chỉ lấy 6 món tiêu biểu để trang chủ gọn đẹp
            if (string.IsNullOrEmpty(query) && !danhMucId.HasValue)
            {
                products = products.Take(6);
            }

            ViewBag.DanhMucs = await _context.DanhMucs.ToListAsync();
            return View(await products.ToListAsync());
        }

        // --- 2. CHI TIẾT SẢN PHẨM ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var doUong = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                    .ThenInclude(ds => ds.MaSizeNavigation)
                .FirstOrDefaultAsync(m => m.MaDoUong == id);

            if (doUong == null) return NotFound();

            ViewBag.Toppings = await _context.Toppings.ToListAsync();
            return View(doUong);
        }
        // --- THÊM VÀO HomeController.cs ---

        // 8. Trang Thực đơn (Hiển thị theo nhóm danh mục)
        public async Task<IActionResult> Menu()
        {
            // 1. Lấy tất cả sản phẩm
            var products = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                .OrderByDescending(d => d.MaDoUong) // Món mới lên đầu
                // .Take(20) <--- XÓA DÒNG NÀY ĐI
                .ToListAsync();

            // 2. Lấy danh sách danh mục để gửi sang View tự tra cứu tên
            ViewBag.DanhMucs = await _context.DanhMucs.ToListAsync();

            return View(products);
        }
        // 9. Trang Khuyến mãi
        public IActionResult Promotions()
        {
            return View();
        }

        // --- 3. THÊM VÀO GIỎ HÀNG (BẮT BUỘC ĐĂNG NHẬP) ---
        [HttpPost]
        public async Task<IActionResult> AddToCart(int MaDoUong, int MaSize, int SoLuong, List<int> Toppings)
        {
            // [MỚI] Kiểm tra đăng nhập
            if (HttpContext.Session.GetString("MaKh") == null)
            {
                // Lưu lại thông báo để hiện ở trang Login (nếu muốn)
                return RedirectToAction("Login", "Account");
            }

            var doUong = await _context.DoUongs.FindAsync(MaDoUong);
            
            var sizeInfo = await _context.DoUongSizes
                .Include(ds => ds.MaSizeNavigation)
                .FirstOrDefaultAsync(x => x.MaDoUong == MaDoUong && x.MaSize == MaSize);

            if (doUong == null || sizeInfo == null) return NotFound("Lỗi dữ liệu sản phẩm");

            // Xử lý topping
            var listToppingsChon = new List<CartTopping>();
            if (Toppings != null && Toppings.Any())
            {
                var dbToppings = await _context.Toppings
                    .Where(t => Toppings.Contains(t.MaTopping))
                    .ToListAsync();

                foreach (var t in dbToppings)
                {
                    listToppingsChon.Add(new CartTopping 
                    { 
                        MaTopping = t.MaTopping, 
                        TenTopping = t.TenTopping, 
                        Gia = t.Gia 
                    });
                }
            }

            // Lưu vào Session
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang") ?? new List<CartItem>();
            var newItem = new CartItem
            {
                MaDoUong = doUong.MaDoUong,
                TenDoUong = doUong.TenDoUong,
                HinhAnh = doUong.HinhAnh,
                MaSize = sizeInfo.MaSize,
                TenSize = sizeInfo.MaSizeNavigation.TenSize,
                DonGia = sizeInfo.Gia,
                SoLuong = SoLuong,
                Toppings = listToppingsChon
            };

            cart.Add(newItem);
            HttpContext.Session.Set("GioHang", cart);

            return RedirectToAction("Index"); 
        }

        // --- 4. XEM GIỎ HÀNG ---
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang") ?? new List<CartItem>();
            ViewBag.TongTien = cart.Sum(item => item.ThanhTien);
            return View(cart);
        }

        // --- 5. XÓA MÓN ---
        public IActionResult RemoveFromCart(int MaDoUong, int MaSize)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang");
            if (cart != null)
            {
                var itemToRemove = cart.FirstOrDefault(x => x.MaDoUong == MaDoUong && x.MaSize == MaSize);
                if (itemToRemove != null)
                {
                    cart.Remove(itemToRemove);
                    HttpContext.Session.Set("GioHang", cart);
                }
            }
            return RedirectToAction("Cart");
        }

        // --- 6. CHECKOUT (GET) - Hiển thị form ---
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang");
            if (cart == null || cart.Count == 0) return RedirectToAction("Index");
            
            // Tự động điền thông tin nếu đã đăng nhập
            if (HttpContext.Session.GetString("TenKh") != null)
            {
                ViewBag.HoTen = HttpContext.Session.GetString("TenKh");
                ViewBag.DienThoai = HttpContext.Session.GetString("UserPhone");
                ViewBag.DiaChi = HttpContext.Session.GetString("UserAddress");
                // [MỚI] Tự điền Email nếu AccountController đã lưu
                ViewBag.Email = HttpContext.Session.GetString("UserEmail");
            }

            ViewBag.TongTien = cart.Sum(i => i.ThanhTien);
            return View(cart); 
        }

        // --- 7. CHECKOUT (POST) - Lưu đơn & Gửi mail ---
        [HttpPost]
        public async Task<IActionResult> Checkout(string HoTen, string DienThoai, string DiaChi, string GhiChu, string Email)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang");
            if (cart == null || !cart.Any()) return RedirectToAction("Index");

            // [MỚI] KIỂM TRA BẮT BUỘC EMAIL
            if (string.IsNullOrEmpty(Email))
            {
                ViewBag.Error = "Vui lòng nhập Email để nhận thông tin đơn hàng.";
                // Gán lại dữ liệu để form không bị trống
                ViewBag.HoTen = HoTen;
                ViewBag.DienThoai = DienThoai;
                ViewBag.DiaChi = DiaChi;
                ViewBag.TongTien = cart.Sum(i => i.ThanhTien);
                return View(cart); // Trả về trang Checkout kèm thông báo lỗi
            }

            // A. Lấy MaKh (Chắc chắn có vì đã bắt đăng nhập ở AddToCart)
            int? maKhachHang = null;
            if (HttpContext.Session.GetString("MaKh") != null)
            {
                maKhachHang = int.Parse(HttpContext.Session.GetString("MaKh")!);
            }

            // B. Tạo Đơn Hàng
            var donHang = new DonHang
            {
                NgayDat = DateTime.Now,
                TinhTrangGiaoHang = 0,
                DaThanhToan = false,
                GhiChu = $"KH: {HoTen}, SĐT: {DienThoai}, ĐC: {DiaChi}, Email: {Email}. Note: {GhiChu}",
                MaKh = maKhachHang
            };
            
            _context.DonHangs.Add(donHang);
            await _context.SaveChangesAsync();

            // C. Lưu Chi Tiết
            foreach (var item in cart)
            {
                var chiTiet = new ChiTietDonHang
                {
                    MaDonHang = donHang.MaDonHang,
                    MaDoUong = item.MaDoUong,
                    MaSize = item.MaSize,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia
                };
                _context.ChiTietDonHangs.Add(chiTiet);
                await _context.SaveChangesAsync();

                if (item.Toppings != null)
                {
                    foreach (var top in item.Toppings)
                    {
                        var chiTietTopping = new ChiTietTopping
                        {
                            MaChiTiet = chiTiet.MaChiTiet,
                            MaTopping = top.MaTopping,
                            SoLuong = 1,
                            DonGia = top.Gia
                        };
                        _context.ChiTietToppings.Add(chiTietTopping);
                    }
                }
            }
            await _context.SaveChangesAsync();

            // D. Gửi Email
            try 
            {
                GuiEmailXacNhan(Email, donHang.MaDonHang, HoTen, cart);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi gửi mail: " + ex.Message);
            }

            // E. Xóa giỏ hàng
            HttpContext.Session.Remove("GioHang");
            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        // --- HÀM GỬI EMAIL ---
        private void GuiEmailXacNhan(string emailNhan, int maDon, string tenKhach, List<CartItem> cart)
        {
            var fromAddress = new MailAddress("sangchuadao123@gmail.com", "WebBanTraSua");
            const string fromPassword = "ghwn wefe ofde ymlp"; // Mật khẩu ứng dụng
            
            var toAddress = new MailAddress(emailNhan, tenKhach);
            const string subject = "Xác nhận đơn hàng từ N16 MilkTea";
            
            string body = $"<h3>Cảm ơn {tenKhach} đã đặt hàng!</h3>";
            body += $"<p>Mã đơn: <b>#{maDon}</b></p>";
            body += "<table border='1' style='border-collapse:collapse; width:100%;'>";
            body += "<tr style='background-color:#f2f2f2;'><th>Món</th><th>Size</th><th>Topping</th><th>Giá</th></tr>";
            
            decimal tongTien = 0;
            foreach(var item in cart)
            {
                string toppings = item.Toppings.Any() ? string.Join(", ", item.Toppings.Select(t => t.TenTopping)) : "Không";
                body += $"<tr><td>{item.TenDoUong}</td><td>{item.TenSize}</td><td>{toppings}</td><td>{item.ThanhTien:N0} đ</td></tr>";
                tongTien += item.ThanhTien;
            }
            body += $"</table><h3>Tổng cộng: <span style='color:red;'>{tongTien:N0} VNĐ</span></h3>";
            
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }
    }
}
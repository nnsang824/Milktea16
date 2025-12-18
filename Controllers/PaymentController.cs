using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;
using N16_MilkTea.Services;
using N16_MilkTea.Extensions;
using System.Net;
using System.Net.Mail;

namespace N16_MilkTea.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly MilkTeaContext _context;

        public PaymentController(IConfiguration configuration, MilkTeaContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // --- 1. THANH TOÁN VNPAY (TẠO URL) ---
        public IActionResult CreatePaymentUrl()
        {
            if (HttpContext.Session.GetString("MaKh") == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Index" });
            }

            var totalString = HttpContext.Session.GetString("OrderTotalAmount");
            var amount = string.IsNullOrEmpty(totalString) ? 0 : double.Parse(totalString);

            if (amount <= 0) return RedirectToAction("CheckoutCOD");

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]);
            vnpay.AddRequestData("vnp_Amount", (amount * 100).ToString()); 
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + DateTime.Now.Ticks);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", "http://localhost:5086/Payment/Callback"); 
            vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(_configuration["VnPay:BaseUrl"], _configuration["VnPay:HashSecret"]);
            return Redirect(paymentUrl);
        }

        // --- 2. XỬ LÝ KẾT QUẢ VNPAY (CALLBACK) ---
        public async Task<IActionResult> Callback()
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            if (vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret))
            {
                if (vnpay.GetResponseData("vnp_ResponseCode") == "00")
                {
                    // VNPAY THÀNH CÔNG -> GỌI HÀM LƯU & GỬI MAIL
                    await XuLyLuuDonHang(true, "Thanh toán qua VNPAY");
                    return View(); 
                }
                else
                {
                    ViewBag.Message = "Lỗi thanh toán VNPAY: " + vnpay.GetResponseData("vnp_ResponseCode");
                }
            }
            else
            {
                ViewBag.Message = "Cảnh báo: Sai chữ ký bảo mật!";
            }
            return View();
        }

        // --- 3. THANH TOÁN COD ---
        public async Task<IActionResult> CheckoutCOD()
        {
            if (HttpContext.Session.GetString("MaKh") == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Index" });
            }

            bool ketQua = await XuLyLuuDonHang(false, "Thanh toán khi nhận hàng (COD)");

            if (ketQua)
            {
                ViewBag.Message = "Đặt hàng thành công! Vui lòng chuẩn bị tiền mặt khi nhận hàng.";
                return View("Callback"); 
            }
            return RedirectToAction("Index", "Cart");
        }

        // --- 4. HÀM LƯU ĐƠN HÀNG (TÁCH RIÊNG) ---
        private async Task<bool> XuLyLuuDonHang(bool daThanhToan, string ghiChu)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");
            var maKhString = HttpContext.Session.GetString("MaKh");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var tenKh = HttpContext.Session.GetString("TenKh") ?? "Khách hàng"; // Lấy tên khách để gửi mail

            if (cart == null || maKhString == null) return false;

            // A. Lưu Đơn
            var donHang = new DonHang
            {
                MaKh = int.Parse(maKhString),
                NgayDat = DateTime.Now,
                TinhTrangGiaoHang = 0,
                DaThanhToan = daThanhToan,
                GhiChu = ghiChu
            };
            _context.DonHangs.Add(donHang);
            await _context.SaveChangesAsync();

            // B. Lưu Chi Tiết
            foreach (var item in cart)
            {
                _context.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaDonHang = donHang.MaDonHang,
                    MaDoUong = item.ProductId,
                    SoLuong = item.Quantity,
                    DonGia = (decimal)item.Price,
                    MaSize = item.MaSize
                });
            }
            await _context.SaveChangesAsync();

            // C. Gửi Email (DÙNG LOGIC CŨ CỦA BẠN)
            if (!string.IsNullOrEmpty(userEmail))
            {
                // Gọi hàm gửi mail cũ (chạy đồng bộ cho chắc ăn)
                GuiEmailXacNhan(userEmail, donHang.MaDonHang, tenKh, cart);
            }

            // D. Dọn dẹp
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("OrderTotalAmount");
            HttpContext.Session.Remove("DiscountAmount");

            ViewBag.Message = $"Đặt hàng thành công! Mã đơn #{donHang.MaDonHang}.";
            return true;
        }

        // --------------------------------------------------------------------
        // 5. HÀM GỬI MAIL (CODE CŨ CỦA BẠN - ĐÃ FIX LẠI THAM SỐ CHO KHỚP)
        // --------------------------------------------------------------------
        private void GuiEmailXacNhan(string emailNhan, int maDon, string tenKhach, List<CartItem> cart)
        {
            try 
            {
                var fromAddress = new MailAddress("sangchuadao123@gmail.com", "WebBanTraSua"); // Nhớ @gmail.com
                const string fromPassword = "ghwn wefe ofde ymlp"; // App Password cũ của bạn
                
                var toAddress = new MailAddress(emailNhan, tenKhach);
                const string subject = "Xác nhận đơn hàng từ N16 MilkTea";
                
                string body = $"<h3>Cảm ơn {tenKhach} đã đặt hàng!</h3>";
                body += $"<p>Mã đơn: <b>#{maDon}</b></p>";
                body += "<table border='1' style='border-collapse:collapse; width:100%;'>";
                body += "<tr style='background-color:#f2f2f2;'><th>Món</th><th>SL</th><th>Giá</th></tr>";
                
                double tongTien = 0;
                foreach(var item in cart)
                {
                    // Logic cũ không có Toppings list trong CartItem, nên mình bỏ đoạn string join toppings đi để tránh lỗi
                    // Nếu bạn muốn hiện topping, CartItem phải có List Toppings
                    body += $"<tr><td>{item.ProductName}</td><td style='text-align:center'>{item.Quantity}</td><td>{item.Total:N0} đ</td></tr>";
                    tongTien += item.Total;
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
                    smtp.Send(message); // Dùng hàm Send đồng bộ như code cũ
                }
            }
            catch (Exception ex)
            {
                // Ghi log để debug nếu cần
                System.Diagnostics.Debug.WriteLine("Lỗi gửi mail: " + ex.Message);
            }
        }
    }
}
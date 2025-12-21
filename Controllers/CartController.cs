using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // QUAN TRỌNG: Thêm dòng này để sửa lỗi .Include()
using N16_MilkTea.Models;
using N16_MilkTea.Extensions;

namespace N16_MilkTea.Controllers
{
    public class CartController : Controller
    {
        private readonly MilkTeaContext _context;

        public CartController(MilkTeaContext context)
        {
            _context = context;
        }

        // 1. Xem giỏ hàng
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            
            // Tính tổng tiền gốc
            double total = cart.Sum(item => item.Total);
            
            // Kiểm tra xem đã áp mã giảm giá chưa
            double discount = HttpContext.Session.GetObject<double>("DiscountAmount");
            double finalTotal = total - discount;
            if (finalTotal < 0) finalTotal = 0;

            // Truyền dữ liệu sang View
            ViewBag.Total = total;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = finalTotal;

            // Lưu số tiền chốt hạ để PaymentController lấy dùng
            HttpContext.Session.SetString("OrderTotalAmount", finalTotal.ToString());

            return View(cart);
        }

        // 2. Thêm vào giỏ hàng (Đã sửa logic lấy giá từ Size S)
        [HttpPost]
        public IActionResult AddToCart(int MaDoUong, int MaSize, int SoLuong, List<int> Toppings)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            
            // 1. Lấy thông tin Món + Size
            var product = _context.DoUongs
                .Include(p => p.DoUongSizes)
                .FirstOrDefault(p => p.MaDoUong == MaDoUong);

            if (product == null) return NotFound();

            var sizeInfo = product.DoUongSizes.FirstOrDefault(s => s.MaSize == MaSize);
            double price = sizeInfo != null ? (double)sizeInfo.Gia : 0;
            string sizeName = sizeInfo?.MaSize == 1 ? "S" : (sizeInfo?.MaSize == 2 ? "M" : "L");

            // 2. Xử lý Topping (Tính tiền + Tạo tên hiển thị)
            double toppingPrice = 0;
            List<string> toppingNames = new List<string>();

            if (Toppings != null && Toppings.Count > 0)
            {
                // Lấy danh sách topping từ DB dựa trên ID gửi lên
                var selectedToppings = _context.Toppings
                    .Where(t => Toppings.Contains(t.MaTopping))
                    .ToList();

                foreach (var t in selectedToppings)
                {
                    toppingPrice += (double)t.Gia;
                    toppingNames.Add(t.TenTopping);
                }
            }

            // 3. Tổng giá cho 1 ly = Giá Size + Giá Topping
            double finalUnitTest = price + toppingPrice;

            // Tạo tên hiển thị đầy đủ: "Trà Sữa (Size L) + Trân châu, Pudding"
            string fullName = $"{product.TenDoUong} (Size {sizeName})";
            if (toppingNames.Count > 0)
            {
                fullName += " + " + string.Join(", ", toppingNames);
            }

            // 4. Thêm vào giỏ
            // Logic: Nếu món giống hệt (cùng ID, cùng giá tiền - tức là cùng size/topping) thì cộng dồn
            var item = cart.FirstOrDefault(p => p.ProductId == MaDoUong && Math.Abs(p.Price - finalUnitTest) < 1);

            if (item != null)
            {
                item.Quantity += SoLuong;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.MaDoUong,
                    ProductName = fullName, // Lưu tên kèm topping
                    ImageUrl = product.HinhAnh,
                    Price = finalUnitTest,  // Giá đã bao gồm topping
                    Quantity = SoLuong,
                    MaSize = MaSize
                });
            }

            // Lưu Session
            HttpContext.Session.SetObject("Cart", cart);
            HttpContext.Session.Remove("DiscountAmount");

            return RedirectToAction("Index");
        }
        // 3. Xóa sản phẩm
        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");
            if (cart != null)
            {
                var item = cart.FirstOrDefault(p => p.ProductId == id);
                if (item != null)
                {
                    cart.Remove(item);
                    HttpContext.Session.SetObject("Cart", cart);
                    HttpContext.Session.Remove("DiscountAmount"); 
                }
            }
            return RedirectToAction("Index");
        }

        // 4. Xử lý Voucher (Đã nâng cấp logic 1 lần/khách)
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string code)
        {
            // A. Kiểm tra đăng nhập
            var maKhStr = HttpContext.Session.GetString("MaKh");
            if (maKhStr == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để dùng mã giảm giá!" });
            }

            int maKh = int.Parse(maKhStr);
            code = code.ToUpper().Trim(); // Chuẩn hóa mã về chữ hoa

            // B. LOGIC KIỂM TRA LỊCH SỬ (QUAN TRỌNG)
            // Thêm điều kiện d.GhiChu != null để tránh lỗi với các đơn hàng cũ
            bool daDung = await _context.DonHangs
                .AnyAsync(d => d.MaKh == maKh && d.GhiChu != null && d.GhiChu.Contains(code));

            if (daDung)
            {
                return Json(new { success = false, message = "Bạn đã sử dụng mã này rồi! Mỗi khách chỉ được dùng 1 lần." });
            }

            // C. Cấu hình các mã giảm giá
            // Mã 1: HELLON16 (Khớp với trang Khuyến mãi)
            // Sửa logic: Giảm thẳng 50k cho khách mới (Dễ xử lý hơn % vì không cần tính tổng tiền lại)
            if (code == "HELLON16" || code == "MILKTEA50") 
            {
                HttpContext.Session.SetString("VoucherCode", code);
                HttpContext.Session.SetString("DiscountAmount", "50000"); // Giảm 50k
                return Json(new { success = true, message = "Áp dụng mã thành công! Giảm 50.000đ", discount = 50000 });
            }

            // Mã 2: T3VUI (Mua 1 tặng 1 -> Tương đương giảm khoảng 30k - giá trung bình 1 ly)
            if (code == "T3VUI")
            {
                // Kiểm tra có phải thứ 3 không? (Nếu muốn demo luôn thì comment dòng if này lại)
                if (DateTime.Now.DayOfWeek != DayOfWeek.Tuesday)
                {
                     // return Json(new { success = false, message = "Mã này chỉ áp dụng vào thứ 3 hàng tuần!" });
                }

                HttpContext.Session.SetString("VoucherCode", code);
                HttpContext.Session.SetString("DiscountAmount", "30000"); // Giảm 30k (coi như tặng 1 ly)
                return Json(new { success = true, message = "Áp dụng mã Mua 1 Tặng 1 thành công! (Giảm 30.000đ)", discount = 30000 });
            }

            return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });
        }
    }
}
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

        // 4. Xử lý Voucher (HARDCODE)
        [HttpPost]
        public IActionResult ApplyVoucher(string code)
        {
            var vouchers = new Dictionary<string, double>
            {
                { "MILKTEA50", 50000 },
                { "CHAOMUNG", 10000 },
                { "FREESHIP", 15000 }
            };

            if (code != null && vouchers.ContainsKey(code.ToUpper()))
            {
                double discount = vouchers[code.ToUpper()];
                HttpContext.Session.SetObject("DiscountAmount", discount);
                TempData["Message"] = $"Áp dụng mã {code} giảm {discount:N0}đ thành công!";
            }
            else
            {
                HttpContext.Session.SetObject("DiscountAmount", 0.0);
                TempData["Error"] = "Mã giảm giá không tồn tại!";
            }

            return RedirectToAction("Index");
        }
    }
}
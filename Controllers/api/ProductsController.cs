using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;

namespace N16_MilkTea.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MilkTeaContext _context;

        public ProductsController(MilkTeaContext context)
        {
            _context = context;
        }

        // 1. GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDoUongs()
        {
            var data = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                // .Include(d => d.MaDanhMucNavigation) <--- BỎ DÒNG NÀY VÌ GÂY LỖI
                .Select(p => new 
                {
                    Id = p.MaDoUong,
                    Ten = p.TenDoUong,
                    HinhAnh = p.HinhAnh,
                    MoTa = p.MoTa,
                    MaDanhMuc = p.MaDanhMuc, // Chỉ lấy ID danh mục thay vì tên
                    
                    // Lấy giá thấp nhất
                    GiaKhoiDiem = p.DoUongSizes.Any() ? p.DoUongSizes.Min(s => s.Gia) : 0,
                    
                    // Danh sách size
                    BangGia = p.DoUongSizes.Select(s => new {
                        Size = s.MaSizeNavigation.TenSize, // Bảng Size có liên kết nên giữ nguyên
                        Gia = s.Gia
                    }).ToList()
                })
                .ToListAsync();

            return Ok(data);
        }

        // 2. GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDoUong(int id)
        {
            var p = await _context.DoUongs
                .Include(d => d.DoUongSizes)
                    .ThenInclude(ds => ds.MaSizeNavigation) // Load tên size
                // .Include(d => d.MaDanhMucNavigation) <--- BỎ DÒNG NÀY
                .FirstOrDefaultAsync(x => x.MaDoUong == id);

            if (p == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm" });
            }

            var result = new
            {
                Id = p.MaDoUong,
                Ten = p.TenDoUong,
                HinhAnh = p.HinhAnh,
                MoTa = p.MoTa,
                MaDanhMuc = p.MaDanhMuc,
                BangGia = p.DoUongSizes.Select(s => new {
                    SizeId = s.MaSize,
                    SizeName = s.MaSizeNavigation != null ? s.MaSizeNavigation.TenSize : "Size",
                    Gia = s.Gia
                }).ToList()
            };

            return Ok(result);
        }

        // 3. POST: api/products
        [HttpPost]
        public async Task<ActionResult<DoUong>> PostDoUong(DoUong doUong)
        {
            _context.DoUongs.Add(doUong);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetDoUong", new { id = doUong.MaDoUong }, doUong);
        }

        // 4. PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDoUong(int id, DoUong doUong)
        {
            if (id != doUong.MaDoUong) return BadRequest();

            _context.Entry(doUong).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.DoUongs.Any(e => e.MaDoUong == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // 5. DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoUong(int id)
        {
            var doUong = await _context.DoUongs.FindAsync(id);
            if (doUong == null) return NotFound();

            // Xóa giá tiền trước
            var sizes = _context.DoUongSizes.Where(x => x.MaDoUong == id);
            _context.DoUongSizes.RemoveRange(sizes);

            _context.DoUongs.Remove(doUong);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
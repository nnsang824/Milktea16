using Microsoft.EntityFrameworkCore;
using N16_MilkTea.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. KHU VỰC ĐĂNG KÝ DỊCH VỤ (Add Services) ---

// Add MVC
builder.Services.AddControllersWithViews();

// Add Session (QUAN TRỌNG: Cần thiết để lưu giỏ hàng)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng tồn tại 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add DbContext
builder.Services.AddDbContext<MilkTeaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MilkTea16")));

var app = builder.Build();

// --- 2. KHU VỰC CẤU HÌNH PIPELINE (Middleware) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// QUAN TRỌNG: app.UseSession() PHẢI NẰM Ở ĐÂY (Trước MapControllerRoute)
app.UseSession(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
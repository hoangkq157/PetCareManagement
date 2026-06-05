using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

public class HomeController : Controller
{
    private readonly PetCareDbContext _context;

    public HomeController(PetCareDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // ✅ Kiểm tra đăng nhập
        if (HttpContext.Session.GetString("NhanVienId") == null)
            return RedirectToAction("Login", "Auth");

        var today    = DateOnly.FromDateTime(DateTime.Today);
        var today_dt = DateTime.Today;
        var warn30   = today_dt.AddDays(30);

        ViewBag.TotalPets    = await _context.ThuCungs.CountAsync();
        ViewBag.TotalOwners  = await _context.ChuNuois.CountAsync();
        ViewBag.TodayApps    = await _context.LichHens.CountAsync(a => a.NgayHen == today);
        ViewBag.PendingApps  = await _context.LichHens.CountAsync(a => a.TrangThai == "ChoDuyet");
        ViewBag.UpcomingVac  = await _context.TiemPhongs
            .CountAsync(v => v.NgayTiemTiep != null
                          && v.NgayTiemTiep >= DateOnly.FromDateTime(today_dt)
                          && v.NgayTiemTiep <= DateOnly.FromDateTime(warn30));
        ViewBag.MonthRevenue = await _context.HoaDons
            .Where(h => h.NgayLap.Month == today.Month && h.NgayLap.Year == today.Year)
            .SumAsync(h => (decimal?)h.TongTien) ?? 0;

        // Dữ liệu biểu đồ
        var revenueData = await _context.HoaDons
            .Where(h => h.NgayLap.Year == today.Year)
            .GroupBy(h => h.NgayLap.Month)
            .Select(g => new { Month = g.Key, Revenue = g.Sum(h => h.TongTien) })
            .ToListAsync();

        var chartData = Enumerable.Range(1, 12).Select(m => new
        {
            Label   = $"T{m}",
            Revenue = revenueData.FirstOrDefault(d => d.Month == m)?.Revenue ?? 0
        }).ToArray();

        ViewBag.ChartLabels  = System.Text.Json.JsonSerializer.Serialize(chartData.Select(d => d.Label));
        ViewBag.ChartRevenue = System.Text.Json.JsonSerializer.Serialize(chartData.Select(d => d.Revenue));

        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
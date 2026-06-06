using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;

namespace PetCareManagement.Controllers;

// GHI CHÚ: File này dùng cho chức năng báo cáo doanh thu.
// Chức năng chính: tổng hợp hoá đơn đã thanh toán và tạo biểu đồ 6 tháng gần nhất.
// File nằm trong thư mục Controllers, được dùng bởi view Báo cáo.

public class BaoCaoController : Controller
{
    // Khai báo DbContext để truy xuất dữ liệu báo cáo doanh thu.
    private readonly PetCareDbContext _context;

    // Inject DbContext vào controller.
    public BaoCaoController(PetCareDbContext context)
    {
        _context = context;
    }

    // Action tạo báo cáo doanh thu 6 tháng gần nhất.
    public async Task<IActionResult> DoanhThu()
    {
        // Nếu chưa đăng nhập thì yêu cầu đăng nhập trước khi xem báo cáo.
        if (HttpContext.Session.GetString("NhanVienId") == null)
            return RedirectToAction("Login", "Auth");

        // Tính ngày bắt đầu của khoảng 6 tháng trước.
        var from = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6));

        // Lấy tổng doanh thu từ các hoá đơn đã thanh toán trong 6 tháng qua.
        var data = await _context.HoaDons
            .Where(h => h.NgayLap >= from && h.TrangThaiTt == "DaThanhToan")
            .GroupBy(h => new { h.NgayLap.Year, h.NgayLap.Month })
            .Select(g => new
            {
                Thang = g.Key.Month + "/" + g.Key.Year,
                SortKey = g.Key.Year * 100 + g.Key.Month,
                TongTien = g.Sum(h => h.TongTien)
            })
            .OrderBy(g => g.SortKey)
            .ToListAsync();

        ViewBag.Labels = System.Text.Json.JsonSerializer.Serialize(data.Select(d => d.Thang));
        ViewBag.DoanhThu = System.Text.Json.JsonSerializer.Serialize(data.Select(d => d.TongTien));

        var thangNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.TongThangNay = await _context.HoaDons
            .Where(h => h.NgayLap.Month == thangNay.Month && h.NgayLap.Year == thangNay.Year && h.TrangThaiTt == "DaThanhToan")
            .SumAsync(h => (decimal?)h.TongTien) ?? 0;

        return View();
    }
}

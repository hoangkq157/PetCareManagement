using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

// GHI CHÚ: File này dùng cho chức năng hoá đơn.
// Chức năng chính: tạo hoá đơn tự động và cập nhật trạng thái thanh toán.
// File nằm trong thư mục Controllers, được dùng bởi view Hoá đơn.

public class HoaDonController : Controller
{
    private readonly PetCareDbContext _context;

    public HoaDonController(PetCareDbContext context)
    {
        _context = context;
    }

    private bool DaDangNhap() => HttpContext.Session.GetString("NhanVienId") != null;

    public async Task<IActionResult> Index()
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var hoaDons = await _context.HoaDons
            .Include(h => h.MaCnNavigation)
            .Include(h => h.MaLhNavigation)
                .ThenInclude(l => l.MaTcNavigation)
            .OrderByDescending(h => h.MaHd)
            .ToListAsync();

        return View(hoaDons);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoHoaDon(int maLh)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var lichHen = await _context.LichHens
            .Include(l => l.MaTcNavigation)
            .Include(l => l.LichHenDichVus)
                .ThenInclude(ld => ld.MaDvNavigation)
            .FirstOrDefaultAsync(l => l.MaLh == maLh);

        if (lichHen == null) return NotFound();

        var daTonTai = await _context.HoaDons.AnyAsync(h => h.MaLh == maLh);
        if (daTonTai)
        {
            TempData["Error"] = "Lịch hẹn này đã có hoá đơn.";
            return RedirectToAction(nameof(Index));
        }

        decimal tongTien = lichHen.LichHenDichVus.Sum(ld => ld.DonGia * ld.SoLuong);

        var hoaDon = new HoaDon
        {
            MaLh = maLh,
            MaCn = lichHen.MaTcNavigation!.MaCn,
            NgayLap = DateOnly.FromDateTime(DateTime.Today),
            TongTien = tongTien,
            TrangThaiTt = "ChuaThanhToan",
            PhuongThucTt = "TienMat"
        };

        _context.HoaDons.Add(hoaDon);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo hoá đơn #{hoaDon.MaHd} cho lịch hẹn {maLh}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(int id, string phuongThuc)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var hoaDon = await _context.HoaDons.FindAsync(id);
        if (hoaDon == null) return NotFound();

        hoaDon.TrangThaiTt = "DaThanhToan";
        hoaDon.PhuongThucTt = phuongThuc;
        _context.Update(hoaDon);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã đánh dấu hoá đơn #{id} là đã thanh toán.";
        return RedirectToAction(nameof(Index));
    }
}

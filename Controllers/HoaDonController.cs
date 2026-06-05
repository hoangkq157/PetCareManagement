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
            PhuongThucTt = "TienMat",
            SoTienKhachTra = 0,
            TienThua = 0,
            NgayThanhToan = null
        };

        _context.HoaDons.Add(hoaDon);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo hoá đơn #{hoaDon.MaHd} cho lịch hẹn {maLh}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(int id, string phuongThuc, decimal soTienKhachTra)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        if (soTienKhachTra <= 0)
        {
            TempData["Error"] = "Vui lòng nhập số tiền khách trả hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var hoaDon = await _context.HoaDons.FindAsync(id);
        if (hoaDon == null) return NotFound();

        var tienThua = soTienKhachTra - hoaDon.TongTien;
        var daThanhToan = soTienKhachTra >= hoaDon.TongTien;

        hoaDon.TrangThaiTt = daThanhToan ? "DaThanhToan" : "ThanhToanMotPhan";
        hoaDon.PhuongThucTt = phuongThuc;
        hoaDon.SoTienKhachTra = soTienKhachTra;
        hoaDon.TienThua = tienThua > 0 ? tienThua : 0;
        hoaDon.NgayThanhToan = DateTime.Now;

        _context.Update(hoaDon);
        await _context.SaveChangesAsync();

        TempData["Success"] = daThanhToan
            ? $"Đã thanh toán hoá đơn #{id} với số tiền {soTienKhachTra:N0} VNĐ."
            : $"Đã ghi nhận thanh toán một phần cho hoá đơn #{id}.";
        return RedirectToAction(nameof(Index));
    }
}

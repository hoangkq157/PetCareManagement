using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

public class TiemPhongController : Controller
{
    private readonly PetCareDbContext _context;

    public TiemPhongController(PetCareDbContext context)
    {
        _context = context;
    }

    // Danh sách cảnh báo tiêm phòng trong 7 ngày tới
    public async Task<IActionResult> CanhBao()
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        var sau7Ngay = homNay.AddDays(7);

        var danhSach = await _context.TiemPhongs
            .Include(t => t.MaTcNavigation)
                .ThenInclude(tc => tc.MaCnNavigation)
            .Where(t =>
                t.NgayTiemTiep.HasValue &&
                t.NgayTiemTiep.Value >= homNay &&
                t.NgayTiemTiep.Value <= sau7Ngay)
            .OrderBy(t => t.NgayTiemTiep)
            .ToListAsync();

        ViewBag.SoCanhBao = danhSach.Count;

        return View(danhSach);
    }

    // Hiển thị danh sách tiêm phòng
    public async Task<IActionResult> Index()
    {
        var ds = await _context.TiemPhongs
            .Include(t => t.MaTcNavigation)
            .ToListAsync();

        return View(ds);
    }

    // Thêm mới tiêm phòng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TiemPhong tiemPhong)
    {
        if (ModelState.IsValid)
        {
            // Tự động tính ngày tiêm tiếp theo
            tiemPhong.NgayTiemTiep =
                tiemPhong.NgayTiem.AddDays(tiemPhong.ChuKyNgay);

            _context.TiemPhongs.Add(tiemPhong);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.DsThuCung = await _context.ThuCungs
            .Select(t => new
            {
                t.MaTc,
                t.TenThuCung
            })
            .ToListAsync();

        return View(tiemPhong);
    }
}
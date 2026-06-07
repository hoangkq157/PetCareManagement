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

    // Danh sách tiêm phòng
    public async Task<IActionResult> Index()
    {
        var ds = await _context.TiemPhongs
            .Include(t => t.MaTcNavigation)
            .ToListAsync();

        return View(ds);
    }

    // ==========================
    // GET: TiemPhong/Create
    // ==========================
    public async Task<IActionResult> Create()
    {
        ViewBag.DsThuCung = await _context.ThuCungs
            .OrderBy(t => t.TenThuCung)
            .ToListAsync();

        return View();
    }

    // POST: TiemPhong/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TiemPhong tiemPhong)
    {
        ModelState.Remove("MaTcNavigation");

        if (ModelState.IsValid)
        {
            try
           {
                // Tự động tính ngày tiêm tiếp theo
                tiemPhong.NgayTiemTiep =
                tiemPhong.NgayTiem.AddDays(tiemPhong.ChuKyNgay);

                _context.TiemPhongs.Add(tiemPhong);

                var result = await _context.SaveChangesAsync();

                TempData["Success"] = $"Thêm lịch tiêm phòng thành công! ({result} bản ghi)";

                return RedirectToAction(nameof(Index));
           }
            catch (Exception ex)
           { 
                TempData["Error"] =
                    ex.InnerException?.Message ?? ex.Message;
           }
       }

        // In lỗi ModelState để debug
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
       {
            Console.WriteLine(error.ErrorMessage);
       }

        ViewBag.DsThuCung = await _context.ThuCungs
            .OrderBy(t => t.TenThuCung)
            .ToListAsync();

        return View(tiemPhong);
    }
 
    // POST: TiemPhong/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tiemPhong = await _context.TiemPhongs.FindAsync(id);

        if (tiemPhong == null)
        {
            TempData["Error"] = "Không tìm thấy lịch tiêm phòng cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        _context.TiemPhongs.Remove(tiemPhong);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã xóa lịch tiêm phòng thành công!";

        return RedirectToAction(nameof(Index));
    }
}
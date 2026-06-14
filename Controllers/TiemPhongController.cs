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

    private bool DaDangNhap() => HttpContext.Session.GetString("NhanVienId") != null;

    // Danh sách cảnh báo: NgayTiem HOẶC NgayTiemTiep trong 7 ngày tới
    public async Task<IActionResult> CanhBao()
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");
        var homNay   = DateOnly.FromDateTime(DateTime.Today);
        var sau7Ngay = homNay.AddDays(7);

        var danhSach = await _context.TiemPhongs
            .Include(t => t.MaTcNavigation)
                .ThenInclude(tc => tc.MaCnNavigation)
            .Where(t =>
                (t.NgayTiem >= homNay && t.NgayTiem <= sau7Ngay) ||
                (t.NgayTiemTiep.HasValue &&
                 t.NgayTiemTiep.Value >= homNay &&
                 t.NgayTiemTiep.Value <= sau7Ngay))
            .OrderBy(t => t.NgayTiem)
            .ToListAsync();

        ViewBag.SoCanhBao = danhSach.Count;
        return View(danhSach);
    }

    // Danh sách tiêm phòng
    public async Task<IActionResult> Index()
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var ds = await _context.TiemPhongs
            .Include(t => t.MaTcNavigation)
            .ToListAsync();

        var homNay   = DateOnly.FromDateTime(DateTime.Today);
        var sau7Ngay = homNay.AddDays(7);
        ViewBag.SoCanhBao = await _context.TiemPhongs
            .CountAsync(t =>
                (t.NgayTiem >= homNay && t.NgayTiem <= sau7Ngay) ||
                (t.NgayTiemTiep.HasValue &&
                 t.NgayTiemTiep.Value >= homNay &&
                 t.NgayTiemTiep.Value <= sau7Ngay));

        return View(ds);
    }

    // GET: TiemPhong/Create
    public async Task<IActionResult> Create()
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

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
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        ModelState.Remove("MaTcNavigation");

        if (ModelState.IsValid)
        {
            try
            {
                var homNay   = DateOnly.FromDateTime(DateTime.Today);
                var sau7Ngay = homNay.AddDays(7);

                // Chặn ngày tiêm trong quá khứ (bảo vệ phía server)
                if (tiemPhong.NgayTiem < homNay)
                {
                    ModelState.AddModelError("NgayTiem", "Ngày tiêm không được là ngày trong quá khứ.");
                    ViewBag.DsThuCung = await _context.ThuCungs.OrderBy(t => t.TenThuCung).ToListAsync();
                    return View(tiemPhong);
                }

                tiemPhong.NgayTiemTiep = tiemPhong.NgayTiem.AddDays(tiemPhong.ChuKyNgay);
                _context.TiemPhongs.Add(tiemPhong);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm lịch tiêm phòng thành công!";

                // Cảnh báo nếu NgayTiem HOẶC NgayTiemTiep nằm trong 7 ngày tới
                bool ngayTiemGan = tiemPhong.NgayTiem >= homNay &&
                                   tiemPhong.NgayTiem <= sau7Ngay;
                bool ngayTiepGan = tiemPhong.NgayTiemTiep.HasValue &&
                                   tiemPhong.NgayTiemTiep.Value >= homNay &&
                                   tiemPhong.NgayTiemTiep.Value <= sau7Ngay;

                if (ngayTiemGan || ngayTiepGan)
                {
                    var msgs = new System.Collections.Generic.List<string>();
                    if (ngayTiemGan)
                    {
                        int cl = tiemPhong.NgayTiem.DayNumber - homNay.DayNumber;
                        msgs.Add($"Ngày tiêm {tiemPhong.NgayTiem:dd/MM/yyyy} — {(cl == 0 ? "hôm nay" : $"còn {cl} ngày")}");
                    }
                    if (ngayTiepGan)
                    {
                        int cl = tiemPhong.NgayTiemTiep!.Value.DayNumber - homNay.DayNumber;
                        msgs.Add($"Lịch tiêm tiếp {tiemPhong.NgayTiemTiep.Value:dd/MM/yyyy} — {(cl == 0 ? "hôm nay" : $"còn {cl} ngày")}");
                    }
                    TempData["CanhBao"] = string.Join(" | ", msgs);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }
        }

        ViewBag.DsThuCung = await _context.ThuCungs
            .OrderBy(t => t.TenThuCung)
            .ToListAsync();
        return View(tiemPhong);
    }

    // GET: TiemPhong/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var tiemPhong = await _context.TiemPhongs.FindAsync(id);
        if (tiemPhong == null)
        {
            TempData["Error"] = "Không tìm thấy lịch tiêm phòng.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.DsThuCung = await _context.ThuCungs
            .OrderBy(t => t.TenThuCung)
            .ToListAsync();
        return View(tiemPhong);
    }

    // POST: TiemPhong/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TiemPhong tiemPhong)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        if (id != tiemPhong.MaTp)
            return NotFound();

        ModelState.Remove("MaTcNavigation");

        if (ModelState.IsValid)
        {
            try
            {
                var homNay   = DateOnly.FromDateTime(DateTime.Today);
                var sau7Ngay = homNay.AddDays(7);

                tiemPhong.NgayTiemTiep = tiemPhong.NgayTiem.AddDays(tiemPhong.ChuKyNgay);
                _context.Update(tiemPhong);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật lịch tiêm phòng thành công!";

                // Cảnh báo nếu NgayTiem HOẶC NgayTiemTiep nằm trong 7 ngày tới
                bool ngayTiemGan = tiemPhong.NgayTiem >= homNay &&
                                   tiemPhong.NgayTiem <= sau7Ngay;
                bool ngayTiepGan = tiemPhong.NgayTiemTiep.HasValue &&
                                   tiemPhong.NgayTiemTiep.Value >= homNay &&
                                   tiemPhong.NgayTiemTiep.Value <= sau7Ngay;

                if (ngayTiemGan || ngayTiepGan)
                {
                    var msgs = new System.Collections.Generic.List<string>();
                    if (ngayTiemGan)
                    {
                        int cl = tiemPhong.NgayTiem.DayNumber - homNay.DayNumber;
                        msgs.Add($"Ngày tiêm {tiemPhong.NgayTiem:dd/MM/yyyy} — {(cl == 0 ? "hôm nay" : $"còn {cl} ngày")}");
                    }
                    if (ngayTiepGan)
                    {
                        int cl = tiemPhong.NgayTiemTiep!.Value.DayNumber - homNay.DayNumber;
                        msgs.Add($"Lịch tiêm tiếp {tiemPhong.NgayTiemTiep.Value:dd/MM/yyyy} — {(cl == 0 ? "hôm nay" : $"còn {cl} ngày")}");
                    }
                    TempData["CanhBao"] = string.Join(" | ", msgs);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.TiemPhongs.AnyAsync(t => t.MaTp == id))
                    return NotFound();
                throw;
            }
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
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

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
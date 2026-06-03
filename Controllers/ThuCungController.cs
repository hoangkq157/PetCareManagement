using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

public class ThuCungController : Controller
{
    private readonly PetCareDbContext _context;

    public ThuCungController(PetCareDbContext context)
    {
        _context = context;
    }

    // Hàm helper kiểm tra session
    private bool DaDangNhap() =>
        HttpContext.Session.GetString("NhanVienId") != null;

    // READ
    public async Task<IActionResult> Index(string? search)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var query = _context.ThuCungs
            .Include(t => t.MaCnNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t =>
                t.TenThuCung.Contains(search) ||
                (t.MaCnNavigation != null && t.MaCnNavigation.HoTen.Contains(search)));

        ViewBag.Search = search;
        return View(await query.ToListAsync());
    }

    // CREATE GET
    public IActionResult Create()
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");
        ViewBag.ChuNuois = _context.ChuNuois.OrderBy(c => c.HoTen).ToList();
        return View();
    }

    // CREATE POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThuCung thuCung)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        ModelState.Remove("MaCnNavigation");
        if (!ModelState.IsValid)
        {
            ViewBag.ChuNuois = _context.ChuNuois.OrderBy(c => c.HoTen).ToList();
            return View(thuCung);
        }

        _context.ThuCungs.Add(thuCung);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm '{thuCung.TenThuCung}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // EDIT GET
    public async Task<IActionResult> Edit(int id)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var tc = await _context.ThuCungs.FindAsync(id);
        if (tc == null) return NotFound();

        ViewBag.ChuNuois = _context.ChuNuois.OrderBy(c => c.HoTen).ToList();
        return View(tc);
    }

    // EDIT POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ThuCung thuCung)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");
        if (id != thuCung.MaTc) return BadRequest();

        ModelState.Remove("MaCnNavigation");
        if (!ModelState.IsValid)
        {
            ViewBag.ChuNuois = _context.ChuNuois.OrderBy(c => c.HoTen).ToList();
            return View(thuCung);
        }

        try
        {
            _context.ThuCungs.Update(thuCung);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.ThuCungs.Any(t => t.MaTc == id)) return NotFound();
            throw;
        }

        TempData["Success"] = $"Đã cập nhật '{thuCung.TenThuCung}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // DELETE POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var tc = await _context.ThuCungs.FindAsync(id);
        if (tc != null)
        {
            _context.ThuCungs.Remove(tc);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xoá '{tc.TenThuCung}'.";
        }
        return RedirectToAction(nameof(Index));
    }
}
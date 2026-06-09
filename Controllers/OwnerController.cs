using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

public class OwnerController : Controller
{
    private readonly PetCareDbContext _context;

    public OwnerController(PetCareDbContext context)
    {
        _context = context;
    }

    private bool DaDangNhap() => HttpContext.Session.GetString("NhanVienId") != null;

    public async Task<IActionResult> Index(string? search)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var query = _context.ChuNuois
            .Include(c => c.ThuCungs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.HoTen.Contains(search) ||
                c.SoDienThoai.Contains(search) ||
                (c.Email != null && c.Email.Contains(search)));
        }

        ViewBag.Search = search;
        return View(await query.OrderBy(c => c.HoTen).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var owner = await _context.ChuNuois
            .Include(c => c.ThuCungs)
            .Include(c => c.HoaDons)
            .FirstOrDefaultAsync(c => c.MaCn == id);

        if (owner == null) return NotFound();

        return View(owner);
    }
}

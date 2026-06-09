using Microsoft.AspNetCore.Mvc;
using PetCareManagement.Data;
using PetCareManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace PetCareManagement.Controllers // Thay PetCareManagement bằng tên project của bạn
{
    public class ChuNuoiController : Controller
    {
        private readonly PetCareDbContext _context;
        // GET: /ChuNuoi/ hoặc /ChuNuoi/Index

        public ChuNuoiController(PetCareDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // mẫu thử hiển thị view 
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int currentId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem thông tin cá nhân.";
                return RedirectToAction("Login", "Auth");
            }

            if (currentId != id)
            {
                return Forbid();
            }

            var owner = await _context.ChuNuois
                .Include(c => c.ThuCungs)
                .FirstOrDefaultAsync(c => c.MaCn == id);

            if (owner == null) return NotFound();

            return View("ThongTinChuNuoi", owner);
        }

        // --- KHU VỰC DÀNH RIÊNG CHO GIAO DIỆN KHÁCH HÀNG ---
        // =======================================================      // 1. Khách xem danh sách dịch vụ
        public IActionResult DanhSachDichVu(string search, string danhMuc)
        {
            // Chỉ lấy những dịch vụ đang mở
            var query = _context.DichVus.Where(dv => dv.TrangThai == true);

            // 1. Lọc theo Danh mục (Khi click từ trang chủ vào)
            if (!string.IsNullOrEmpty(danhMuc))
            {
                query = query.Where(dv => dv.DanhMuc.Contains(danhMuc));
                ViewBag.DanhMuc = danhMuc; // Truyền ra View để đổi Tiêu đề cho đẹp
            }

            // 2. Lọc theo thanh tìm kiếm (Nếu khách tự gõ chữ tìm kiếm)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(dv => dv.TenDichVu.Contains(search) || dv.DanhMuc.Contains(search));
                ViewBag.Search = search;
            }

            var danhSachDichVu = query.ToList();
            return View(danhSachDichVu);
        }

        // 2. Khách xem danh sách tiêm phòng
        // GET: ChuNuoi/DanhSachTiemPhong
        public async Task<IActionResult> DanhSachTiemPhong()
        {
            // Lấy ID của Chủ nuôi đang đăng nhập từ Session thay vì dùng mock
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");

            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem sổ tiêm phòng.";
                return RedirectToAction("Login", "Auth");
            }

            // Lấy lịch sử tiêm phòng của tất cả thú cưng thuộc chủ nuôi ĐANG ĐĂNG NHẬP
            var lichSuTiemPhong = await _context.TiemPhongs
                .Include(t => t.MaTcNavigation)
                .Where(t => t.MaTcNavigation.MaCn == maChuNuoi)
                .OrderByDescending(t => t.NgayTiem)
                .ToListAsync();

            return View(lichSuTiemPhong);
        }

        // 3. GET: ChuNuoi/DatLich (Mở form hiển thị cho khách)
        [HttpGet]
        public IActionResult DatLich()
        {
            // Truyền danh sách dịch vụ ra View để khách chọn trong thẻ <select>
            ViewBag.DichVus = _context.DichVus.ToList();
            return View();
        }

        // 4. Khách hàng xem lịch sử đặt lịch của họ
        // GET: ChuNuoi/LichSuDatLich
        public IActionResult LichSuDatLich()
        {
            // Tạm thời giả lập Mã chủ nuôi đang đăng nhập là 1 giống như bên hàm DatLich
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem lịch sử đặt lịch.";
                return RedirectToAction("Login", "Auth");
            }

            // Lấy danh sách lịch hẹn thuộc về Thú cưng của Chủ nuôi này
            var danhSachLichHen = _context.LichHens
                .Include(l => l.MaTcNavigation)  // Nạp thông tin Thú cưng để lấy tên
                .Include(l => l.MaNvNavigation)  // Nạp thông tin Nhân viên phụ trách
                .Include(l => l.LichHenDichVus)  // Nạp bảng trung gian Chi tiết dịch vụ
                    .ThenInclude(ld => ld.MaDvNavigation) // Nạp tên Dịch vụ tương ứng
                .Where(l => l.MaTcNavigation.MaCn == maChuNuoi) // Lọc lịch hẹn của chủ nuôi này
                .OrderByDescending(l => l.NgayTao) // Lịch mới đặt xếp lên đầu
                .ToList();

            return View(danhSachLichHen);
        }

        // POST: ChuNuoi/HuyLichHen (Khách tự bấm hủy lịch)
        [HttpPost]
        public IActionResult HuyLichHen(int id)
        {
            var lichHen = _context.LichHens.Find(id);
            // Khách chỉ được hủy khi trạng thái là Chờ duyệt hoặc Đã xác nhận nhưng chưa Hoàn thành
            if (lichHen != null && lichHen.TrangThai != "HoanThanh" && lichHen.TrangThai != "Huy")
            {
                lichHen.TrangThai = "Huy";
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(LichSuDatLich));
        }

        // POST: ChuNuoi/DatLich (Xử lý lưu dữ liệu khi khách nhấn nút Hoàn tất)
        [HttpPost]
        public async Task<IActionResult> DatLich(LichHen lichHen, string TenThuCung, string Loai, string Giong, decimal? CanNang, string MauLong, int MaDv)
        {
            try
            {
                // BƯỚC 1: LẤY THÔNG TIN CHỦ NUÔI ĐANG ĐĂNG NHẬP TỪ SESSION
                string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");

                if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
                {
                    TempData["Error"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                // Chuẩn hóa tên thú cưng (Xóa khoảng trắng thừa ở 2 đầu)
                string tenThuCungClean = TenThuCung.Trim();

                // BƯỚC 2: KIỂM TRA THÚ CƯNG ĐÃ TỒN TẠI HAY CHƯA
                // Tìm con thú cưng của Chủ nuôi này, có cùng Tên và cùng Loại (Chó/Mèo)
                var existingPet = await _context.ThuCungs
                    .FirstOrDefaultAsync(t => t.MaCn == maChuNuoi
                                           && t.TenThuCung.ToLower() == tenThuCungClean.ToLower()
                                           && t.Loai == Loai);

                int idThuCungDatLich = 0;

                if (existingPet != null)
                {
                    // TÌM THẤY: Đã có thú cưng này -> Lấy ID của nó
                    idThuCungDatLich = existingPet.MaTc;

                    // (Tùy chọn bổ sung) Cập nhật lại thông tin nếu khách hàng nhập mới
                    bool hasChanges = false;
                    if (CanNang.HasValue && existingPet.CanNang != CanNang) { existingPet.CanNang = CanNang; hasChanges = true; }
                    if (!string.IsNullOrEmpty(MauLong) && existingPet.MauLong != MauLong) { existingPet.MauLong = MauLong; hasChanges = true; }
                    if (!string.IsNullOrEmpty(Giong) && existingPet.Giong != Giong) { existingPet.Giong = Giong; hasChanges = true; }

                    if (hasChanges)
                    {
                        _context.ThuCungs.Update(existingPet);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // KHÔNG TÌM THẤY: Tạo mới thú cưng hoàn toàn
                    var thuCungMoi = new ThuCung
                    {
                        MaCn = maChuNuoi,
                        TenThuCung = tenThuCungClean,
                        Loai = Loai,
                        Giong = Giong,
                        CanNang = CanNang,
                        MauLong = MauLong,
                        NgaySinh = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)) // Mặc định 1 tuổi
                    };

                    _context.ThuCungs.Add(thuCungMoi);
                    await _context.SaveChangesAsync();

                    // Lấy ID của thú cưng vừa mới tạo
                    idThuCungDatLich = thuCungMoi.MaTc;
                }

                // BƯỚC 3: ĐIỀN THÔNG TIN LỊCH HẸN VÀ LIÊN KẾT VỚI THÚ CƯNG (CŨ HOẶC MỚI)
                lichHen.MaTc = idThuCungDatLich; // Trỏ đúng ID đã lấy ở Bước 2
                lichHen.MaNv = null;
                lichHen.TrangThai = "ChoDuyet";
                lichHen.NgayTao = DateTime.Now;

                _context.LichHens.Add(lichHen);
                await _context.SaveChangesAsync();

                // BƯỚC 4: LƯU CHI TIẾT DỊCH VỤ ĐÃ CHỌN
                var dichVuDuocChon = await _context.DichVus.FindAsync(MaDv);
                if (dichVuDuocChon != null)
                {
                    decimal donGia = (Loai == "Chó") ? dichVuDuocChon.GiaCho : dichVuDuocChon.GiaMeo;

                    var lhDv = new LichHenDichVu
                    {
                        MaLh = lichHen.MaLh,
                        MaDv = MaDv,
                        SoLuong = 1,
                        DonGia = donGia
                    };
                    _context.LichHenDichVus.Add(lhDv);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Đặt lịch thành công! Vui lòng chờ nhân viên xác nhận.";
                return RedirectToAction("LichSuDatLich", "ChuNuoi");
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, load lại Form
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi đặt lịch: " + ex.Message);
                ViewBag.DichVus = _context.DichVus.Where(dv => dv.TrangThai == true).ToList();
                return View(lichHen);
            }
        }

        // Danh sach thu cung ma nguoi dung nuoi
        public async Task<IActionResult> DanhSachThuCung()
        {
            // 1. Lấy ID của Chủ nuôi đang đăng nhập từ Session
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");

            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem danh sách thú cưng.";
                return RedirectToAction("Login", "Auth");
            }

            // 2. Lấy danh sách thú cưng NHƯNG PHẢI LỌC theo MaChuNuoi
            var danhSach = await _context.ThuCungs
                .Where(t => t.MaCn == maChuNuoi) // Lọc chỗ này rất quan trọng
                .ToListAsync();

            return View(danhSach);
        }

        // POST: ChuNuoi/XoaThuCung (Chức năng xóa thú cưng dành riêng cho khách)
        [HttpPost]
        public async Task<IActionResult> XoaThuCung(int id)
        {
            // 1. Lấy thú cưng cần xóa
            var thuCung = await _context.ThuCungs.FindAsync(id);
            if (thuCung == null)
            {
                TempData["Error"] = "Không tìm thấy thú cưng này.";
                return RedirectToAction(nameof(DanhSachThuCung));
            }

            // 2. Lấy ID chủ nuôi đang đăng nhập để kiểm tra bảo mật (không cho xóa trộm của người khác)
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (int.TryParse(chuNuoiIdStr, out int maChuNuoi) && thuCung.MaCn == maChuNuoi)
            {
                _context.ThuCungs.Remove(thuCung);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thú cưng khỏi danh sách thành công!";
            }
            else
            {
                TempData["Error"] = "Bạn không có quyền xóa thú cưng này!";
            }

            return RedirectToAction(nameof(DanhSachThuCung));
        }

        // ─────────────────────────────────────────────────────────────────
        // HỒ SƠ CÁ NHÂN
        // ─────────────────────────────────────────────────────────────────

        // GET: ChuNuoi/ThongTinChuNuoi
        public async Task<IActionResult> ThongTinChuNuoi()
        {
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem hồ sơ.";
                return RedirectToAction("Login", "Auth");
            }

            var chuNuoi = await _context.ChuNuois
                .Include(c => c.ThuCungs)
                .Include(c => c.HoaDons)
                .FirstOrDefaultAsync(c => c.MaCn == maChuNuoi);

            if (chuNuoi == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tài khoản.";
                return RedirectToAction("Login", "Auth");
            }

            return View(chuNuoi);
        }

        // GET: ChuNuoi/ChinhSuaHoSo
        public async Task<IActionResult> ChinhSuaHoSo()
        {
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
            {
                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Auth");
            }

            var chuNuoi = await _context.ChuNuois.FindAsync(maChuNuoi);
            if (chuNuoi == null) return RedirectToAction("Login", "Auth");

            return View(chuNuoi);
        }

        // POST: ChuNuoi/ChinhSuaHoSo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChinhSuaHoSo(string hoTen, string soDienThoai, string? email, string? diaChi)
        {
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(soDienThoai))
            {
                ViewBag.Error = "Họ tên và số điện thoại không được để trống.";
                var cnErr = await _context.ChuNuois.FindAsync(maChuNuoi);
                return View(cnErr);
            }

            // Kiểm tra email trùng với tài khoản khác
            if (!string.IsNullOrWhiteSpace(email))
            {
                bool emailTrung = await _context.ChuNuois
                    .AnyAsync(c => c.Email == email.Trim().ToLower() && c.MaCn != maChuNuoi);
                if (emailTrung)
                {
                    ViewBag.Error = "Email này đã được sử dụng bởi tài khoản khác.";
                    var cnErr = await _context.ChuNuois.FindAsync(maChuNuoi);
                    return View(cnErr);
                }
            }

            var chuNuoi = await _context.ChuNuois.FindAsync(maChuNuoi);
            if (chuNuoi == null) return RedirectToAction("Login", "Auth");

            chuNuoi.HoTen       = hoTen.Trim();
            chuNuoi.SoDienThoai = soDienThoai.Trim();
            chuNuoi.Email       = email?.Trim().ToLower();
            chuNuoi.DiaChi      = diaChi?.Trim();

            await _context.SaveChangesAsync();

            // Cập nhật lại tên trong Session
            HttpContext.Session.SetString("ChuNuoiName", chuNuoi.HoTen);

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction(nameof(ThongTinChuNuoi));
        }

        // GET: ChuNuoi/DoiMatKhau
        public IActionResult DoiMatKhau()
        {
            if (HttpContext.Session.GetString("ChuNuoiId") == null)
                return RedirectToAction("Login", "Auth");
            return View();
        }

        // POST: ChuNuoi/DoiMatKhau
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            string chuNuoiIdStr = HttpContext.Session.GetString("ChuNuoiId");
            if (string.IsNullOrEmpty(chuNuoiIdStr) || !int.TryParse(chuNuoiIdStr, out int maChuNuoi))
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(matKhauCu) || string.IsNullOrWhiteSpace(matKhauMoi))
            { ViewBag.Error = "Vui lòng nhập đầy đủ thông tin."; return View(); }

            if (matKhauMoi.Length < 6)
            { ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự."; return View(); }

            if (matKhauMoi != xacNhanMatKhau)
            { ViewBag.Error = "Mật khẩu xác nhận không khớp."; return View(); }

            var chuNuoi = await _context.ChuNuois.FindAsync(maChuNuoi);
            if (chuNuoi == null) return RedirectToAction("Login", "Auth");

            if (chuNuoi.MatKhau != matKhauCu)
            { ViewBag.Error = "Mật khẩu hiện tại không đúng."; return View(); }

            chuNuoi.MatKhau = matKhauMoi;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(ThongTinChuNuoi));
        }

    }
}
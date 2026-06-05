using Microsoft.AspNetCore.Mvc;

namespace PetCareManagement.Controllers // Thay PetCareManagement bằng tên project của bạn
{
    public class ChuNuoiController : Controller
    {
        // GET: /ChuNuoi/ hoặc /ChuNuoi/Index
        public IActionResult Index()
        {
            // Tạm thời chúng ta trả về giao diện trước để test đường dẫn
            // Sau này bạn sẽ viết code gọi Database (lấy danh sách chủ nuôi) ở đây
            return View();
        }
    }
}
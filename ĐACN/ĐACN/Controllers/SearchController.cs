using ĐACN;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace FoodDeliveryDB.Controllers
{
    public class SearchController : Controller
    {
        private readonly FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        public ActionResult Index(string keyword)
        {
            var result = db.MonAns
                .Where(m => keyword == null ||
                            m.TenMon.Contains(keyword) ||
                            m.NhaHang.TenNH.Contains(keyword) ||
                            m.LoaiMonAn.TenLoai.Contains(keyword))
                .ToList();

            ViewBag.Keyword = keyword;
            return View(result);
        }
    }
}
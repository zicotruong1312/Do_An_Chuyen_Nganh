using ĐACN.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;

namespace ĐACN.Controllers
{
    public class HomeController : Controller
    {
        private readonly FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        // ========== TRANG CHỦ ==========
        public ActionResult TrangChu()
        {
            AutoLoginTheoIP();

            var nhaHangList = db.NhaHangs.ToList();
            var danhGiaList = db.DanhGiaNhaHangs
                                .Select(dg => new { dg.MaNH, dg.DiemDG })
                                .ToList();

            var luotMuaDict = db.DonHangs
                                .GroupBy(d => d.MaNH)
                                .Select(g => new
                                {
                                    MaNH = g.Key,
                                    LuotMua = g.Select(d => d.MaDon).Distinct().Count()
                                })
                                .ToList();

            var maxLuotMua = luotMuaDict.Max(l => (int?)l.LuotMua) ?? 1;

            var nhaHangData = nhaHangList.Select(x =>
            {
                double rating = danhGiaList
                    .Where(dg => dg.MaNH == x.MaNH)
                    .Select(dg => (double?)dg.DiemDG)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                int luotMua = luotMuaDict
                    .Where(l => l.MaNH == x.MaNH)
                    .Select(l => (int?)l.LuotMua)
                    .FirstOrDefault() ?? 0;

                double normalizedMua = (luotMua / (double)maxLuotMua) * 10;
                double score = (rating * 0.6) + (normalizedMua * 0.4);

                return new NhaHangViewModel
                {
                    MaNH = x.MaNH,
                    TenNH = x.TenNH,
                    DiaChi = x.DiaChi,
                    TrangThai = x.TrangThai,
                    HinhAnh = x.HinhAnh,
                    Rating = rating,
                    TongLuotMua = luotMua,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Rating)
            .ThenByDescending(x => x.TongLuotMua)
            .ToList();

            var model = new TrangChuViewModel
            {
                DanhMuc = db.LoaiMonAns
                            .Select(x => new LoaiMonAnViewModel
                            {
                                MaLoai = x.MaLoai,
                                TenLoai = x.TenLoai,
                                HinhAnh = x.HinhAnh
                            })
                            .ToList(),
                NhaHang = nhaHangData
            };

            return View(model);
        }

        // ========== TRANG DANH MỤC ==========
        public ActionResult DanhMuc()
        {
            AutoLoginTheoIP();

            var danhMucList = db.LoaiMonAns
                                .Select(x => new LoaiMonAnViewModel
                                {
                                    MaLoai = x.MaLoai,
                                    TenLoai = x.TenLoai,
                                    HinhAnh = x.HinhAnh
                                })
                                .ToList();

            return View(danhMucList);
        }

        // ========== TRANG NHÀ HÀNG ==========
        public ActionResult NhaHang()
        {
            AutoLoginTheoIP();
            var nhaHangData = LoadNhaHangData();
            return View(nhaHangData);
        }

        // ========== PARTIAL: NHÀ HÀNG NỔI BẬT ==========
        public ActionResult _NhaHangNoiBatPartial()
        {
            AutoLoginTheoIP();
            var nhaHangData = LoadNhaHangData();
            var nhaHangNoiBat = nhaHangData
                                 .Where(x => x.Score >= 5.5)
                                 .OrderByDescending(x => x.Score)
                                 .ThenByDescending(x => x.Rating)
                                 .ThenByDescending(x => x.TongLuotMua)
                                 .Take(10)
                                 .ToList();
            return PartialView(nhaHangNoiBat);
        }

        // ========== PARTIAL: DANH MỤC ==========
        public ActionResult _DanhMucPartial()
        {
            AutoLoginTheoIP();

            var danhMucList = db.LoaiMonAns
                                .Select(x => new LoaiMonAnViewModel
                                {
                                    MaLoai = x.MaLoai,
                                    TenLoai = x.TenLoai,
                                    HinhAnh = x.HinhAnh
                                })
                                .ToList();

            return PartialView(danhMucList);
        }

        // ========== LOGIN AJAX ==========
        [HttpPost]
        public JsonResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không được để trống." });

            // Load all users tạm thời để tránh lỗi EF translation
            var allUsers = db.TaiKhoans.ToList();
            var tk = allUsers.FirstOrDefault(x => x.TenDangNhap == username && x.MatKhau == password);

            if (tk == null)
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng." });

            // Lưu session và cookie theo IP
            Session["TaiKhoan"] = tk;

            if (tk.VaiTro == "KhachHang")
            {
                var allKhachHang = db.KhachHangs.ToList();
                var maKH = allKhachHang.FirstOrDefault(k => k.MaTK == tk.MaTK)?.MaKH;

                if (!string.IsNullOrEmpty(maKH))
                    Session["MaKH"] = maKH;
            }

            // Lưu cookie IP
            var ip = LayDiaChiIP();
            Response.Cookies.Add(new HttpCookie("ZFoodLoginIP", ip)
            {
                Expires = DateTime.Now.AddDays(30)
            });

            Response.Cookies.Add(new HttpCookie("ZFoodUser", tk.TenDangNhap)
            {
                Expires = DateTime.Now.AddDays(30)
            });

            return Json(new { success = true });
        }

        // ========== LOGOUT ==========
        public ActionResult Logout()
        {
            Session.Clear();

            if (Request.Cookies["ZFoodLoginIP"] != null)
            {
                var c = new HttpCookie("ZFoodLoginIP") { Expires = DateTime.Now.AddDays(-1) };
                Response.Cookies.Add(c);
            }

            if (Request.Cookies["ZFoodUser"] != null)
            {
                var c = new HttpCookie("ZFoodUser") { Expires = DateTime.Now.AddDays(-1) };
                Response.Cookies.Add(c);
            }

            return RedirectToAction("TrangChu");
        }

        // ========== HÀM HỖ TRỢ: LẤY IP ==========
        private string LayDiaChiIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
                ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }

        // ========== HÀM TỰ ĐỘNG LOGIN THEO IP ==========
        private void AutoLoginTheoIP()
        {
            if (Session["TaiKhoan"] != null)
                return;

            var cookieIP = Request.Cookies["ZFoodLoginIP"];
            var cookieUser = Request.Cookies["ZFoodUser"];

            if (cookieIP != null && cookieUser != null && !string.IsNullOrEmpty(cookieUser.Value))
            {
                string currentIP = LayDiaChiIP();
                if (cookieIP.Value == currentIP)
                {
                    var allUsers = db.TaiKhoans.ToList();
                    var tk = allUsers.FirstOrDefault(x => x.TenDangNhap == cookieUser.Value);
                    if (tk != null)
                    {
                        Session["TaiKhoan"] = tk;

                        if (tk.VaiTro == "KhachHang")
                        {
                            var allKhachHang = db.KhachHangs.ToList();
                            var maKH = allKhachHang.FirstOrDefault(k => k.MaTK == tk.MaTK)?.MaKH;
                            if (!string.IsNullOrEmpty(maKH))
                                Session["MaKH"] = maKH;
                        }
                    }
                }
            }
        }

        // ========== HÀM HỖ TRỢ: LOAD NHÀ HÀNG ==========
        private List<NhaHangViewModel> LoadNhaHangData()
        {
            var nhaHangList = db.NhaHangs.ToList();
            var danhGiaList = db.DanhGiaNhaHangs
                                .Select(dg => new { dg.MaNH, dg.DiemDG })
                                .ToList();

            var luotMuaDict = db.DonHangs
                                .GroupBy(d => d.MaNH)
                                .Select(g => new
                                {
                                    MaNH = g.Key,
                                    LuotMua = g.Select(d => d.MaDon).Distinct().Count()
                                })
                                .ToList();

            var maxLuotMua = luotMuaDict.Max(l => (int?)l.LuotMua) ?? 1;

            var nhaHangData = nhaHangList.Select(x =>
            {
                double rating = danhGiaList
                    .Where(dg => dg.MaNH == x.MaNH)
                    .Select(dg => (double?)dg.DiemDG)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                int luotMua = luotMuaDict
                    .Where(l => l.MaNH == x.MaNH)
                    .Select(l => (int?)l.LuotMua)
                    .FirstOrDefault() ?? 0;

                double normalizedMua = (luotMua / (double)maxLuotMua) * 10;
                double score = (rating * 0.6) + (normalizedMua * 0.4);

                return new NhaHangViewModel
                {
                    MaNH = x.MaNH,
                    TenNH = x.TenNH,
                    DiaChi = x.DiaChi,
                    TrangThai = x.TrangThai,
                    HinhAnh = x.HinhAnh,
                    Rating = rating,
                    TongLuotMua = luotMua,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Rating)
            .ThenByDescending(x => x.TongLuotMua)
            .ToList();

            return nhaHangData;
        }
    }
}
using ĐACN;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FoodDeliveryDB.Controllers
{
    public class NhaHangController : Controller
    {
        private readonly FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        // ==========================
        // KIỂM TRA ĐĂNG NHẬP CHỦ NHÀ HÀNG
        // ==========================
        private bool CheckLogin()
        {
            var tk = Session["TaiKhoan"] as TaiKhoan;
            if (tk == null || tk.VaiTro != "NhaHang")
                return false;

            // Gán MaNH nếu chưa có
            if (Session["MaNH"] == null)
            {
                var nh = db.NhaHangs.FirstOrDefault(n => n.MaTK == tk.MaTK);
                if (nh != null)
                    Session["MaNH"] = nh.MaNH;
            }

            return true;
        }

        private ActionResult RedirectLogin()
        {
            return RedirectToAction("Login", "Account");
        }

        // ==========================
        // INDEX -> THỐNG KÊ
        // ==========================
        public ActionResult Index()
        {
            return RedirectToAction("ThongKe");
        }

        // ==========================
        // THỐNG KÊ
        // ==========================
        public ActionResult ThongKe()
        {
            if (!CheckLogin())
                return RedirectLogin();

            var maNH = Session["MaNH"].ToString();

            // Doanh thu theo danh mục
            var doanhThuTheoDanhMuc = db.ChiTietDonHangs
                .Where(ct => ct.DonHang.MaNH == maNH)
                .GroupBy(ct => ct.MonAn.MaLoai)
                .Select(g => new
                {
                    Loai = g.FirstOrDefault().MonAn.LoaiMonAn.TenLoai,
                    DoanhThu = g.Sum(x => x.SoLuong * x.DonGia)
                }).ToList();

            ViewBag.LabelsLoai = doanhThuTheoDanhMuc.Select(x => x.Loai).ToList();
            ViewBag.DataDoanhThuLoai = doanhThuTheoDanhMuc.Select(x => x.DoanhThu).ToList();

            // Doanh thu theo tháng
            var doanhThuTheoThang = db.DonHangs
                .Where(d => d.MaNH == maNH && d.ThoiGianDat != null)
                .GroupBy(d => d.ThoiGianDat.Value.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongDoanhThu = g.Sum(x => x.TongTien)
                }).ToList();

            ViewBag.LabelsThoiGian = doanhThuTheoThang.Select(x => "Tháng " + x.Thang).ToList();
            ViewBag.DataDoanhThu = doanhThuTheoThang.Select(x => x.TongDoanhThu).ToList();

            // Trạng thái đơn hàng
            var dataTrangThai = db.DonHangs
                .Where(d => d.MaNH == maNH)
                .GroupBy(d => d.TrangThai)
                .Select(g => g.Count()).ToList();

            ViewBag.DataTrangThaiDonHang = dataTrangThai;

            // Top sản phẩm bán chạy
            var topSanPham = db.ChiTietDonHangs
                .Where(ct => ct.DonHang.MaNH == maNH)
                .GroupBy(ct => ct.MaMon)
                .Select(g => new
                {
                    TenMon = g.FirstOrDefault().MonAn.TenMon,
                    Gia = g.FirstOrDefault().MonAn.Gia,
                    SoLuongBan = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToList();

            ViewBag.TopSanPham = topSanPham;

            return View();
        }

        // ==========================
        // THÔNG TIN CỬA HÀNG (GET)
        // ==========================
        public ActionResult ThongTinCuaHang()
        {
            if (!CheckLogin())
                return RedirectLogin();

            string maNH = Session["MaNH"].ToString();
            var nhaHang = db.NhaHangs.FirstOrDefault(n => n.MaNH == maNH);

            if (nhaHang == null)
                return HttpNotFound();

            var tk = db.TaiKhoans.FirstOrDefault(t => t.MaTK == nhaHang.MaTK);

            ViewBag.MatKhauHienTai = tk?.MatKhau ?? "";

            return View(nhaHang);
        }



        // ==========================
        // THÔNG TIN CỬA HÀNG (POST)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatThongTinCuaHang(
            NhaHang model,
            HttpPostedFileBase HinhAnhMoi,
            string MatKhauCu,
            string MatKhauMoi,
            string XacNhanMatKhau)
        {
            if (!CheckLogin())
                return RedirectLogin();

            var nhaHang = db.NhaHangs.FirstOrDefault(n => n.MaNH == model.MaNH);
            if (nhaHang == null)
                return HttpNotFound();

            var tk = db.TaiKhoans.FirstOrDefault(t => t.MaTK == nhaHang.MaTK);

            // -----------------------------
            // VALIDATE MẬT KHẨU
            // -----------------------------
            if (!string.IsNullOrEmpty(MatKhauMoi))
            {
                if (tk.MatKhau != MatKhauCu)
                {
                    ViewBag.Error = "❌ Mật khẩu cũ không chính xác!";
                    ViewBag.MatKhauHienTai = tk.MatKhau;
                    return View("ThongTinCuaHang", nhaHang);
                }

                if (MatKhauMoi != XacNhanMatKhau)
                {
                    ViewBag.Error = "❌ Xác nhận mật khẩu không khớp!";
                    ViewBag.MatKhauHienTai = tk.MatKhau;
                    return View("ThongTinCuaHang", nhaHang);
                }

                tk.MatKhau = MatKhauMoi;
            }

            // -----------------------------
            // CẬP NHẬT THÔNG TIN NHÀ HÀNG
            // -----------------------------
            nhaHang.TenNH = model.TenNH;
            nhaHang.DiaChi = model.DiaChi;
            nhaHang.SDT = model.SDT;
            nhaHang.MoTa = model.MoTa;   // <== BẠN ĐANG BỊ THIẾU DÒNG NÀY

            // -----------------------------
            // CẬP NHẬT HÌNH ẢNH
            // -----------------------------
            if (HinhAnhMoi != null && HinhAnhMoi.ContentLength > 0)
            {
                string fileName = Path.GetFileName(HinhAnhMoi.FileName);
                string savePath = Server.MapPath("~/images/nhahang/" + fileName);
                HinhAnhMoi.SaveAs(savePath);

                nhaHang.HinhAnh = "~/images/nhahang/" + fileName;
            }

            // -----------------------------
            // LƯU VÀ TRẢ VỀ THÔNG TIN
            // -----------------------------
            db.SaveChanges();

            ViewBag.Success = "✔ Cập nhật thông tin thành công!";
            ViewBag.MatKhauHienTai = tk?.MatKhau ?? "";

            return View("ThongTinCuaHang", nhaHang);
        }


        // ==========================
        // QUẢN LÝ SẢN PHẨM
        // ==========================
        public ActionResult QuanLySanPham()
        {
            if (!CheckLogin())
                return RedirectLogin();

            var maNH = Session["MaNH"].ToString();

            var monAn = db.MonAns
                          .Where(m => m.MaNH == maNH)
                          .Include("LoaiMonAn")
                          .ToList();

            return View(monAn);
        }

        // Thêm sản phẩm
        [HttpGet]
        public ActionResult ThemSanPham()
        {
            if (!CheckLogin())
                return RedirectLogin();

            ViewBag.MaLoai = new SelectList(db.LoaiMonAns.ToList(), "MaLoai", "TenLoai");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(MonAn monAn, HttpPostedFileBase HinhAnhFile)
        {
            if (!CheckLogin())
                return RedirectLogin();

            try
            {
                if (ModelState.IsValid)
                {
                    if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
                    {
                        string fileName = Path.GetFileName(HinhAnhFile.FileName);
                        string path = Path.Combine(Server.MapPath("~/images/monan"), fileName);
                        HinhAnhFile.SaveAs(path);

                        monAn.HinhAnh = fileName;
                    }

                    monAn.MaMon = Guid.NewGuid().ToString("N").Substring(0, 10);
                    monAn.TrangThai = true;
                    monAn.MaNH = Session["MaNH"].ToString();

                    db.MonAns.Add(monAn);
                    db.SaveChanges();

                    TempData["Success"] = "Thêm món ăn thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi thêm: " + ex.Message;
            }

            ViewBag.MaLoai = new SelectList(db.LoaiMonAns.ToList(), "MaLoai", "TenLoai", monAn.MaLoai);
            return View(monAn);
        }

        // ==========================
        // CHỈNH SỬA SẢN PHẨM
        // ==========================
        [HttpGet]
        public ActionResult ChinhSuaSanPham(string id)
        {
            if (!CheckLogin())
                return RedirectLogin();

            var mon = db.MonAns.Find(id);
            if (mon == null) return HttpNotFound();

            ViewBag.MaLoai = new SelectList(db.LoaiMonAns.ToList(), "MaLoai", "TenLoai", mon.MaLoai);
            return View(mon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaSanPham(MonAn monAn, HttpPostedFileBase HinhAnhFile)
        {
            if (!CheckLogin())
                return RedirectLogin();

            try
            {
                var old = db.MonAns.Find(monAn.MaMon);
                if (old == null) return HttpNotFound();

                old.TenMon = monAn.TenMon;
                old.MaLoai = monAn.MaLoai;
                old.Gia = monAn.Gia;
                old.MoTa = monAn.MoTa;
                old.TrangThai = monAn.TrangThai;

                if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(HinhAnhFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/images/monan"), fileName);
                    HinhAnhFile.SaveAs(path);

                    old.HinhAnh = fileName;
                }

                db.SaveChanges();
                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction("QuanLySanPham");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi cập nhật: " + ex.Message;
                ViewBag.MaLoai = new SelectList(db.LoaiMonAns.ToList(), "MaLoai", "TenLoai", monAn.MaLoai);
                return View(monAn);
            }
        }

        // ==========================
        // XÓA SẢN PHẨM
        // ==========================
        public ActionResult XoaSanPham(string id)
        {
            if (!CheckLogin())
                return RedirectLogin();

            try
            {
                var mon = db.MonAns.Find(id);
                if (mon == null) return HttpNotFound();

                db.MonAns.Remove(mon);
                db.SaveChanges();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi xóa: " + ex.Message;
            }
            return RedirectToAction("QuanLySanPham");
        }

        // ===========================
        // DANH SÁCH ĐƠN HÀNG
        // ===========================
        [HttpGet]
        public ActionResult DanhSachDonHang(DateTime? tuNgay, DateTime? denNgay)
        {
            if (Session["MaNH"] == null)
                return RedirectToAction("Login", "Account");

            string maNH = Session["MaNH"].ToString();

            // Chỉ lấy đơn hàng của nhà hàng đang đăng nhập
            var query = db.DonHangs
                          .Include(d => d.KhachHang)
                          .Include(d => d.Shipper)
                          .Where(d => d.MaNH == maNH);

            // Lọc theo ngày
            if (tuNgay.HasValue)
                query = query.Where(d => d.ThoiGianDat >= tuNgay.Value);

            if (denNgay.HasValue)
                query = query.Where(d => d.ThoiGianDat <= denNgay.Value);

            // Lấy danh sách
            var donHangs = query
                .OrderByDescending(d => d.ThoiGianDat)
                .ToList();

            // Truyền lại giá trị để hiển thị filter
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            return View(donHangs);
        }

        [HttpGet]
        public ActionResult ChiTietDonHang(string id)
        {
            if (!CheckLogin())
                return RedirectLogin();

            var donHang = db.DonHangs
                            .Include("KhachHang")
                            .Include("Shipper")
                            .FirstOrDefault(d => d.MaDon == id);

            if (donHang == null)
                return HttpNotFound();

            var chiTiet = db.ChiTietDonHangs
                            .Include("MonAn")
                            .Where(ct => ct.MaDon == id)
                            .ToList();

            ViewBag.ChiTiet = chiTiet;
            return View(donHang);
        }
    }
}

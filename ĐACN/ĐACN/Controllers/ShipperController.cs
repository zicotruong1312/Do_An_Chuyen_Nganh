using ĐACN.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ĐACN.Controllers
{
    public class ShipperController : Controller
    {
        private FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        #region ===================== AUTO LOGIN THEO COOKIE =====================
        public void AutoLoginTheoIP()
        {
            if (Session["TaiKhoan"] != null)
                return;

            var cookieIP = Request.Cookies["ZFoodLoginIP"];
            var cookieUser = Request.Cookies["ZFoodUser"];

            if (cookieIP != null && cookieUser != null)
            {
                string currentIP = LayDiaChiIP();
                if (cookieIP.Value == currentIP)
                {
                    var tk = db.TaiKhoans.FirstOrDefault(x => x.TenDangNhap == cookieUser.Value);
                    if (tk != null)
                    {
                        Session["TaiKhoan"] = tk;

                        if (tk.VaiTro == "Shipper")
                        {
                            var shipper = db.Shippers
                                            .Include("TaiKhoan")
                                            .FirstOrDefault(s => s.MaTK == tk.MaTK);

                            if (shipper != null)
                            {
                                Session["MaShipper"] = shipper.MaShipper;
                                Session["Shipper"] = shipper;
                            }
                        }
                    }
                }
            }
        }

        private string LayDiaChiIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
                ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }
        #endregion

        #region ===================== KIỂM TRA LOGIN SHIPPER =====================
        private bool KiemTraDangNhap()
        {
            AutoLoginTheoIP();
            var tk = Session["TaiKhoan"] as TaiKhoan;
            if (tk == null || tk.VaiTro != "Shipper" || tk.TrangThai != true)
                return false;

            if (Session["MaShipper"] == null)
            {
                var shipper = db.Shippers.FirstOrDefault(s => s.MaTK == tk.MaTK);
                if (shipper != null)
                    Session["MaShipper"] = shipper.MaShipper;
            }

            return Session["MaShipper"] != null;
        }

        private Shipper LayShipper()
        {
            string maShipper = Session["MaShipper"] as string;
            if (string.IsNullOrEmpty(maShipper))
                return null;

            return db.Shippers.Include("TaiKhoan").FirstOrDefault(s => s.MaShipper == maShipper);
        }
        #endregion

        #region ===================== TRANG ĐĂNG NHẬP =====================
        [HttpGet]
        public ActionResult Login()
        {
            if (KiemTraDangNhap())
                return RedirectToAction("Index");

            return RedirectToAction("Login", "Account");
        }
        #endregion

        #region ===================== DANH SÁCH ĐƠN CHỜ NHẬN =====================
        public ActionResult Index()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";

            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }

            var danhSachMaDon = db.DonHangs
                .Where(d => string.IsNullOrEmpty(d.MaShipper))
                .Select(d => d.MaDon)
                .ToList();

            var chiTietMonAn = (from ctdh in db.ChiTietDonHangs
                                join mon in db.MonAns on ctdh.MaMon equals mon.MaMon
                                where danhSachMaDon.Contains(ctdh.MaDon)
                                select new
                                {
                                    ctdh.MaDon,
                                    mon.TenMon,
                                    ctdh.SoLuong
                                }).ToList();

            var orders = (from d in db.DonHangs
                          where string.IsNullOrEmpty(d.MaShipper)
                          join kh in db.KhachHangs on d.MaKH equals kh.MaKH into khGroup
                          from kh in khGroup.DefaultIfEmpty()
                          join nh in db.NhaHangs on d.MaNH equals nh.MaNH into nhGroup
                          from nh in nhGroup.DefaultIfEmpty()
                          select new OrderCardViewModel
                          {
                              MaDon = d.MaDon,
                              TongTien = d.TongTien,
                              TenNhaHang = nh.TenNH,
                              DiaChiNhaHang = nh.DiaChi,
                              TenKhachHang = kh.TenKH,
                              DiaChiKhachHang = d.DiaChi,
                              TrangThai = d.TrangThai,
                              ThoiGianDat = d.ThoiGianDat,
                              SDTNhaHang = nh.SDT,
                              SDTKhachHang = d.Sdt,
                              SoLuongMon = 0,
                              DanhSachMonTomTat = ""
                          })
                          .OrderByDescending(d => d.ThoiGianDat)
                          .ToList();

            foreach (var order in orders)
            {
                var monAnTrongDon = chiTietMonAn.Where(m => m.MaDon == order.MaDon).ToList();
                order.SoLuongMon = monAnTrongDon.Sum(m => m.SoLuong ?? 0);
                order.DanhSachMonTomTat = string.Join(", ", monAnTrongDon
                    .GroupBy(m => m.TenMon)
                    .Select(g => $"{g.Key} x{g.Sum(x => x.SoLuong ?? 0)}")
                    .Take(3));
            }

            return View(orders);
        }
        #endregion

        #region ===================== DANH SÁCH ĐƠN ĐÃ NHẬN =====================
        public ActionResult Accepted()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";

            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Login", "Account");
            }

            var danhSachMaDon = db.DonHangs
                .Where(d => d.MaShipper == shipper.MaShipper)
                .Select(d => d.MaDon)
                .ToList();

            var chiTietMonAnData = (from ctdh in db.ChiTietDonHangs
                                    join mon in db.MonAns on ctdh.MaMon equals mon.MaMon
                                    where danhSachMaDon.Contains(ctdh.MaDon)
                                    select new
                                    {
                                        ctdh.MaDon,
                                        MaMon = mon.MaMon,
                                        TenMon = mon.TenMon,
                                        SoLuong = ctdh.SoLuong ?? 0,
                                        DonGia = ctdh.DonGia ?? 0,
                                        HinhAnh = mon.HinhAnh,
                                        MoTa = mon.MoTa
                                    }).ToList();

            var ordersTemp = (from d in db.DonHangs
                              where d.MaShipper == shipper.MaShipper
                              join kh in db.KhachHangs on d.MaKH equals kh.MaKH into khGroup
                              from kh in khGroup.DefaultIfEmpty()
                              join nh in db.NhaHangs on d.MaNH equals nh.MaNH into nhGroup
                              from nh in nhGroup.DefaultIfEmpty()
                              select new AcceptedOrderView
                              {
                                  MaDon = d.MaDon,
                                  TongTien = d.TongTien,
                                  TenNhaHang = nh.TenNH,
                                  DiaChiNhaHang = nh.DiaChi,
                                  TenKhachHang = kh.TenKH,
                                  DiaChiKhachHang = d.DiaChi,
                                  SDTKhachHang = d.Sdt,
                                  TrangThai = d.TrangThai,
                                  ThoiGianDat = d.ThoiGianDat,
                                  SDTNhaHang = nh.SDT,
                                  DiaChi = d.DiaChi,
                                  Sdt = d.Sdt,
                                  SoLuongMon = 0,
                                  DanhSachMonTomTat = ""
                              }).ToList();

            var orders = ordersTemp
                .Where(o => string.IsNullOrEmpty(o.TrangThai) || !o.TrangThai.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.ThoiGianDat)
                .ToList();

            foreach (var order in orders)
            {
                var monAnTrongDon = chiTietMonAnData.Where(m => m.MaDon == order.MaDon).ToList();
                order.SoLuongMon = monAnTrongDon.Sum(m => m.SoLuong);
                order.DanhSachMonTomTat = string.Join(", ", monAnTrongDon
                    .GroupBy(m => m.TenMon)
                    .Select(g => $"{g.Key} x{g.Sum(x => x.SoLuong)}")
                    .Take(3));

                order.ChiTietMonAn = monAnTrongDon.Select(x => new ChiTietMonAnViewModel
                {
                    MaMon = x.MaMon,
                    TenMon = x.TenMon,
                    SoLuong = x.SoLuong,
                    DonGia = x.DonGia,
                    ThanhTien = x.SoLuong * x.DonGia,
                    HinhAnh = x.HinhAnh,
                    MoTa = x.MoTa
                }).ToList();
            }

            return View(orders);
        }
        #endregion

        #region ===================== XEM CHI TIẾT ĐƠN HÀNG =====================
        [HttpGet]
        public ActionResult Details(string maDon)
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";

            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để xem chi tiết đơn hàng.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDon))
                return RedirectToAction("Index");

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Login", "Account");
            }

            var donHangInfo = db.DonHangs
                                .Where(d => d.MaDon == maDon)
                                .Select(d => new { d.MaDon, d.MaKH, d.MaNH, d.MaShipper, d.TrangThai, d.TongTien, d.ThoiGianDat })
                                .FirstOrDefault();

            if (donHangInfo == null)
                return HttpNotFound();

            if (!string.IsNullOrEmpty(donHangInfo.MaShipper) && donHangInfo.MaShipper != shipper.MaShipper)
            {
                TempData["Message"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index");
            }

            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKH == donHangInfo.MaKH);
            var nhaHang = db.NhaHangs.FirstOrDefault(nh => nh.MaNH == donHangInfo.MaNH);

            var chiTietData = (from ctdh in db.ChiTietDonHangs
                               join mon in db.MonAns on ctdh.MaMon equals mon.MaMon
                               where ctdh.MaDon == maDon
                               select new
                               {
                                   MaMon = mon.MaMon,
                                   TenMon = mon.TenMon,
                                   SoLuong = ctdh.SoLuong ?? 0,
                                   DonGia = ctdh.DonGia ?? 0,
                                   HinhAnh = mon.HinhAnh,
                                   MoTa = mon.MoTa
                               }).ToList();

            var chiTietMonAn = chiTietData.Select(x => new ChiTietMonAnViewModel
            {
                MaMon = x.MaMon,
                TenMon = x.TenMon,
                SoLuong = x.SoLuong,
                DonGia = x.DonGia,
                ThanhTien = x.SoLuong * x.DonGia,
                HinhAnh = x.HinhAnh,
                MoTa = x.MoTa
            }).ToList();

            var donHang = new DonHangDetailsViewModel
            {
                MaDon = donHangInfo.MaDon,
                MaKH = donHangInfo.MaKH,
                MaNH = donHangInfo.MaNH,
                MaShipper = donHangInfo.MaShipper,
                TrangThai = donHangInfo.TrangThai ?? "Chờ xác nhận",
                TongTien = donHangInfo.TongTien,
                ThoiGianDat = donHangInfo.ThoiGianDat,
                KhachHang = khachHang != null ? new KhachHangInfoViewModel
                {
                    TenKH = khachHang.TenKH ?? "N/A",
                    DiaChi = khachHang.DiaChi ?? "N/A",
                    SDT = khachHang.SDT ?? "N/A"
                } : null,
                NhaHang = nhaHang != null ? new NhaHangInfoViewModel
                {
                    TenNH = nhaHang.TenNH ?? "N/A",
                    DiaChi = nhaHang.DiaChi ?? "N/A",
                    SDT = nhaHang.SDT ?? "N/A"
                } : null,
                ChiTietMonAn = chiTietMonAn
            };

            return View(donHang);
        }
        #endregion

        #region ===================== NHẬN, CẬP NHẬT, HỦY ĐƠN =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Accept(string maDon)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để nhận đơn hàng.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDon))
                return RedirectToAction("Index");

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Index");
            }

            var donHangInfo = db.DonHangs
                .Where(d => d.MaDon == maDon && string.IsNullOrEmpty(d.MaShipper))
                .Select(d => new { d.MaKH, d.KhachHang.DiaChi, d.KhachHang.SDT })
                .FirstOrDefault();

            if (donHangInfo != null)
            {
                int rowsAffected = db.Database.ExecuteSqlCommand(
                    "UPDATE DonHang SET MaShipper = @p0, TrangThai = @p1, DiaChi = @p2, Sdt = @p3 " +
                    "WHERE MaDon = @p4 AND (MaShipper IS NULL OR MaShipper = '')",
                    shipper.MaShipper,
                    "Đang lấy món",
                    donHangInfo.DiaChi,
                    donHangInfo.SDT,
                    maDon
                );

                TempData["Message"] = rowsAffected > 0 ? "Bạn đã nhận đơn thành công." : "Đơn đã được nhận bởi shipper khác hoặc không tồn tại.";
            }
            else
            {
                TempData["Message"] = "Đơn đã được nhận bởi shipper khác hoặc không tồn tại.";
            }

            return RedirectToAction("Accepted");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(string maDon, string trangThai)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để cập nhật trạng thái.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(trangThai))
                return RedirectToAction("Accepted");

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Accepted");
            }

            int rowsAffected = db.Database.ExecuteSqlCommand(
                "UPDATE DonHang SET TrangThai = @p0 WHERE MaDon = @p1 AND MaShipper = @p2",
                trangThai,
                maDon,
                shipper.MaShipper
            );

            TempData["Message"] = rowsAffected > 0 ? "Cập nhật trạng thái thành công." : "Không thể cập nhật trạng thái.";
            return RedirectToAction("Accepted");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(string maDon)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để hủy đơn hàng.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDon))
                return RedirectToAction("Accepted");

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Accepted");
            }

            int rowsAffected = db.Database.ExecuteSqlCommand(
                "UPDATE DonHang SET MaShipper = NULL, TrangThai = N'Chờ xác nhận' " +
                "WHERE MaDon = @p0 AND MaShipper = @p1",
                maDon,
                shipper.MaShipper
            );

            TempData["Message"] = rowsAffected > 0 ? "Đã hủy nhận đơn. Đơn đã quay lại danh sách chờ." : "Không thể hủy đơn.";
            return RedirectToAction("Accepted");
        }
        #endregion

        #region ===================== THÔNG TIN CÁ NHÂN =====================
        [HttpGet]
        public new ActionResult Profile()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";

            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để xem thông tin cá nhân.";
                return RedirectToAction("Login", "Account");
            }

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Login", "Account");
            }

            var tongThuNhap = db.DonHangs
                .Where(d => d.MaShipper == shipper.MaShipper && d.TrangThai == "Hoàn thành")
                .Select(d => d.TongTien ?? 0)
                .Sum();

            ViewBag.TongThuNhap = tongThuNhap;
            return View(shipper);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public new ActionResult Profile(string tenShipper, string sdt, string bienSoXe, string username, string password, HttpPostedFileBase avatar)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Message"] = "Vui lòng đăng nhập để cập nhật thông tin.";
                return RedirectToAction("Login", "Account");
            }

            var shipper = LayShipper();
            if (shipper == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin shipper.";
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrWhiteSpace(tenShipper))
                shipper.TenShipper = tenShipper;

            if (!string.IsNullOrWhiteSpace(sdt))
                shipper.SDT = sdt;

            if (!string.IsNullOrWhiteSpace(bienSoXe))
                shipper.BienSoXe = bienSoXe;

            if (shipper.TaiKhoan != null)
            {
                if (!string.IsNullOrWhiteSpace(username))
                    shipper.TaiKhoan.TenDangNhap = username;

                if (!string.IsNullOrWhiteSpace(password))
                    shipper.TaiKhoan.MatKhau = password;
            }

            if (avatar != null && avatar.ContentLength > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var ext = Path.GetExtension(avatar.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Message"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, bmp).";
                    return RedirectToAction("Profile");
                }

                if (avatar.ContentLength > 5 * 1024 * 1024)
                {
                    TempData["Message"] = "Kích thước file không được vượt quá 5MB.";
                    return RedirectToAction("Profile");
                }

                var fileName = Path.GetFileNameWithoutExtension(avatar.FileName) + "_" + DateTime.Now.Ticks + ext;
                var path = Path.Combine(Server.MapPath("~/images/shipper/"), fileName);

                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                if (!string.IsNullOrEmpty(shipper.HinhAnh))
                {
                    var oldPath = Server.MapPath("~" + shipper.HinhAnh);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                avatar.SaveAs(path);
                shipper.HinhAnh = "/images/shipper/" + fileName;
            }

            db.SaveChanges();
            db.Entry(shipper).Reload();
            if (shipper.TaiKhoan != null) db.Entry(shipper.TaiKhoan).Reload();
            Session["Shipper"] = shipper;

            TempData["Message"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("Profile");
        }
        #endregion

        #region ===================== THU NHẬP & LỊCH SỬ =====================
        [HttpGet]
        public ActionResult Income()
        {
            if (!KiemTraDangNhap()) return RedirectToAction("Login", "Account");

            var shipper = LayShipper();
            if (shipper == null) return RedirectToAction("Login", "Account");

            var completedOrders = db.DonHangs
                .Where(d => d.MaShipper == shipper.MaShipper && d.TrangThai == "Hoàn thành")
                .Include("KhachHang").Include("NhaHang")
                .OrderByDescending(d => d.ThoiGianDat)
                .Select(d => new
                {
                    d.MaDon,
                    TongTien = d.TongTien,
                    d.ThoiGianDat,
                    TenKhachHang = d.KhachHang.TenKH,
                    TenNhaHang = d.NhaHang.TenNH
                }).ToList();

            ViewBag.TongThuNhap = completedOrders.Sum(d => d.TongTien);
            ViewBag.TongDonHoanThanh = completedOrders.Count;
            ViewBag.DonHoanThanh = completedOrders;

            return View();
        }

        [HttpGet]
        public ActionResult History(string week)
        {
            if (!KiemTraDangNhap()) return RedirectToAction("Login", "Account");

            var shipper = LayShipper();
            if (shipper == null) return RedirectToAction("Login", "Account");

            DateTime monday;
            if (!string.IsNullOrEmpty(week) && DateTime.TryParse(week, out DateTime weekDate))
            {
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)weekDate.DayOfWeek + 7) % 7;
                if (daysUntilMonday == 0 && weekDate.DayOfWeek != DayOfWeek.Monday) daysUntilMonday = 7;
                monday = weekDate.AddDays(-daysUntilMonday).Date;
            }
            else
            {
                DateTime today = DateTime.Now;
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
                if (daysUntilMonday == 0 && today.DayOfWeek != DayOfWeek.Monday) daysUntilMonday = 7;
                monday = today.AddDays(-daysUntilMonday).Date;
            }

            DateTime sunday = monday.AddDays(6).Date.AddDays(1).AddTicks(-1);

            var weekOrders = db.DonHangs
                .Where(d => d.MaShipper == shipper.MaShipper &&
                            d.ThoiGianDat.HasValue &&
                            d.ThoiGianDat.Value >= monday &&
                            d.ThoiGianDat.Value <= sunday &&
                            d.TrangThai == "Hoàn thành")
                .Select(d => new { d.MaDon, TongTien = d.TongTien ?? 0, d.ThoiGianDat })
                .ToList();

            var ordersByDate = weekOrders
                .GroupBy(o => o.ThoiGianDat.Value.Date)
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), Total = g.Sum(x => x.TongTien) });

            ViewBag.Monday = monday;
            ViewBag.OrdersByDate = ordersByDate;

            return View();
        }
        #endregion

        #region ===================== AJAX =====================
        [HttpGet]
        public JsonResult GetOrdersByDate(string date)
        {
            if (!KiemTraDangNhap()) return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);

            var shipper = LayShipper();
            if (shipper == null) return Json(new { success = false, message = "Không tìm thấy thông tin shipper." }, JsonRequestBehavior.AllowGet);

            if (!DateTime.TryParse(date, out DateTime selectedDate))
                return Json(new { success = false, message = "Ngày không hợp lệ." }, JsonRequestBehavior.AllowGet);

            DateTime startDate = selectedDate.Date;
            DateTime endDate = startDate.AddDays(1).AddTicks(-1);

            var orders = db.DonHangs
                .Where(d => d.MaShipper == shipper.MaShipper &&
                            d.ThoiGianDat.HasValue &&
                            d.ThoiGianDat.Value >= startDate &&
                            d.ThoiGianDat.Value <= endDate &&
                            d.TrangThai == "Hoàn thành")
                .Select(d => new
                {
                    d.MaDon,
                    TongTien = d.TongTien ?? 0,
                    ThoiGianDat = d.ThoiGianDat,
                    TenKhachHang = d.KhachHang.TenKH,
                    TenNhaHang = d.NhaHang.TenNH
                })
                .OrderByDescending(d => d.ThoiGianDat)
                .ToList();

            return Json(new
            {
                success = true,
                orders = orders.Select(o => new
                {
                    o.MaDon,
                    o.TongTien,
                    ThoiGianDat = o.ThoiGianDat?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    o.TenKhachHang,
                    o.TenNhaHang
                })
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLocation(string maDon)
        {
            var don = db.DonHangs.FirstOrDefault(x => x.MaDon == maDon);
            if (don == null) return Json(null, JsonRequestBehavior.AllowGet);

            return Json(new { latitude = don.Latitude, longitude = don.Longitude }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateLocation(double latitude, double longitude)
        {
            var shipper = LayShipper();
            if (shipper == null) return Json(new { success = false });

            shipper.Latitude = latitude;
            shipper.Longitude = longitude;
            db.SaveChanges();

            return Json(new { success = true });
        }
        #endregion

        #region ===================== LOGOUT =====================
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

            return RedirectToAction("Login", "Account");
        }
        #endregion
    }
}
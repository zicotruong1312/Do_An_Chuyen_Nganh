using ĐACN.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace ĐACN.Controllers
{
    public class KhachHangController : Controller
    {
        private FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        // Thay bằng API key của bạn
        private const string ORS_API_KEY = "YOUR_ORS_API_KEY";

        // ===================== HỖ TRỢ LẤY IP =====================
        public string LayDiaChiIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
                ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }

        // ===================== AUTO LOGIN THEO COOKIE =====================
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
                        if (tk.VaiTro == "KhachHang")
                        {
                            var maKH = db.KhachHangs
                                         .Where(k => k.MaTK == tk.MaTK)
                                         .Select(k => k.MaKH)
                                         .FirstOrDefault();
                            if (!string.IsNullOrEmpty(maKH))
                                Session["MaKH"] = maKH;
                        }
                    }
                }
            }
        }

        // ===================== KIỂM TRA LOGIN =====================
        private bool KiemTraDangNhap()
        {
            AutoLoginTheoIP();
            return Session["MaKH"] != null;
        }

        // ===================== XÓA LỊCH SỬ GIỎ HÀNG QUÁ HẠN =====================
        private void XoaLichSuQuaHan()
        {
            DateTime han = DateTime.Now.AddDays(-5);
            var oldItems = db.LichSuGioHangs.Where(x => x.ThoiGianChon < han).ToList();
            if (oldItems.Any())
            {
                db.LichSuGioHangs.RemoveRange(oldItems);
                db.SaveChanges();
            }
        }

        // ===================== XEM MENU =====================
        public ActionResult XemMenu(string id)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Msg"] = "Vui lòng đăng nhập để xem menu!";
                return RedirectToAction("TrangChu", "Home");
            }

            var nhaHang = db.NhaHangs.Find(id);
            if (nhaHang == null) return HttpNotFound();

            var dsMon = db.MonAns
                .Where(m => m.MaNH == id)
                .Select(m => new MonAnViewModel
                {
                    MaMon = m.MaMon,
                    TenMon = m.TenMon,
                    Gia = m.Gia ?? 0,
                    MoTa = m.MoTa,
                    HinhAnh = m.HinhAnh
                }).ToList();

            ViewBag.NhaHang = nhaHang;
            return View(dsMon);
        }

        // ===================== THÊM VÀO GIỎ =====================
        public ActionResult ThemVaoGio(string id)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Msg"] = "Vui lòng đăng nhập để thêm món!";
                return RedirectToAction("TrangChu", "Home");
            }

            XoaLichSuQuaHan();

            var mon = db.MonAns.Find(id);
            if (mon == null) return HttpNotFound();

            string maKH = Session["MaKH"] as string;

            var lsgh = db.LichSuGioHangs.FirstOrDefault(x => x.MaKH == maKH && x.MaMon == mon.MaMon);
            if (lsgh == null)
            {
                string maGH = "GH" + (db.LichSuGioHangs.Count() + 1).ToString().PadLeft(5, '0');
                db.LichSuGioHangs.Add(new LichSuGioHang
                {
                    MaGH = maGH,
                    MaKH = maKH,
                    MaNH = mon.MaNH,
                    MaMon = mon.MaMon,
                    SoLuong = 1,
                    DonGia = mon.Gia ?? 0,
                    TongTien = mon.Gia ?? 0,
                    ThoiGianChon = DateTime.Now
                });
            }
            else
            {
                lsgh.SoLuong += 1;
                lsgh.TongTien = lsgh.SoLuong * lsgh.DonGia;
                lsgh.ThoiGianChon = DateTime.Now;
            }

            db.SaveChanges();
            return RedirectToAction("XemMenu", new { id = mon.MaNH });
        }

        // ===================== CẬP NHẬT SỐ LƯỢNG =====================
        [HttpPost]
        public ActionResult CapNhatSoLuong(string id, int soLuong)
        {
            if (!KiemTraDangNhap())
                return RedirectToAction("TrangChu", "Home");

            string maKH = Session["MaKH"] as string;
            var lsgh = db.LichSuGioHangs.FirstOrDefault(x => x.MaKH == maKH && x.MaMon == id);

            if (lsgh != null)
            {
                if (soLuong <= 0)
                    db.LichSuGioHangs.Remove(lsgh);
                else
                {
                    lsgh.SoLuong = soLuong;
                    lsgh.TongTien = soLuong * lsgh.DonGia;
                }
                db.SaveChanges();
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        // ===================== XÓA KHỎI GIỎ =====================
        [HttpPost]
        public ActionResult XoaKhoiGio(string id)
        {
            if (!KiemTraDangNhap())
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            string maKH = Session["MaKH"] as string;
            var lsgh = db.LichSuGioHangs.FirstOrDefault(x => x.MaKH == maKH && x.MaMon == id);

            if (lsgh != null)
            {
                db.LichSuGioHangs.Remove(lsgh);
                db.SaveChanges();
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        // ===================== XEM GIỎ HÀNG =====================
        public ActionResult XemGioHang()
        {
            if (!KiemTraDangNhap()) return RedirectToAction("TrangChu", "Home");

            string maKH = Session["MaKH"] as string;
            XoaLichSuQuaHan();

            var cart = (from ls in db.LichSuGioHangs
                        join m in db.MonAns on ls.MaMon equals m.MaMon
                        where ls.MaKH == maKH
                        select new CartItem
                        {
                            MaMon = ls.MaMon,
                            TenMon = m.TenMon,
                            Gia = ls.DonGia,
                            SoLuong = ls.SoLuong,
                            MaNH = ls.MaNH,
                            TenNH = ls.NhaHang.TenNH,
                            ThanhTien = ls.TongTien,
                            HinhAnh = m.HinhAnh
                        }).ToList();

            return View(cart);
        }

        // ===================== ĐẶT HÀNG =====================
        [HttpPost]
        public ActionResult DatHang(string maNH, string diaChi, string sdt)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Msg"] = "Vui lòng đăng nhập để đặt hàng!";
                return RedirectToAction("TrangChu", "Home");
            }

            string maKH = Session["MaKH"] as string;

            var cart = db.LichSuGioHangs
                         .Where(x => x.MaKH == maKH && x.MaNH == maNH)
                         .ToList();

            if (!cart.Any())
            {
                TempData["Msg"] = "Giỏ hàng nhà hàng này trống!";
                return RedirectToAction("XemGioHang");
            }

            // Tạo mã đơn
            string maDon = "DH" + (db.DonHangs.Count() + 1).ToString().PadLeft(5, '0');

            var don = new DonHang
            {
                MaDon = maDon,
                MaKH = maKH,
                MaNH = maNH,
                DiaChi = diaChi,
                Sdt = sdt,
                TrangThai = "Chờ xác nhận",
                TongTien = cart.Sum(x => x.TongTien),
                ThoiGianDat = DateTime.Now
            };

            db.DonHangs.Add(don);
            db.SaveChanges();

            foreach (var item in cart)
            {
                db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaDon = maDon,
                    MaMon = item.MaMon,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia
                });

                db.LichSuGioHangs.Remove(item);
            }

            db.SaveChanges();

            TempData["Msg"] = "Đặt hàng thành công!";
            return RedirectToAction("TrangChu", "Home");
        }

        // ===================== ĐƠN HÀNG CỦA TÔI =====================
        public ActionResult DonHangCuaToi()
        {
            if (!KiemTraDangNhap())
            {
                TempData["Msg"] = "Vui lòng đăng nhập để xem đơn hàng!";
                return RedirectToAction("TrangChu", "Home");
            }

            string maKH = Session["MaKH"] as string;

            var donHangDangXuLy = db.DonHangs
                .Where(d => d.MaKH == maKH && (d.TrangThai == "Chờ xác nhận" || d.TrangThai == "Đang giao" || d.TrangThai == "Đang lấy món"))
                .OrderByDescending(d => d.ThoiGianDat)
                .Select(d => new DonHangModel
                {
                    MaDon = d.MaDon,
                    MaKH = d.MaKH,
                    MaNH = d.MaNH,
                    TrangThai = d.TrangThai,
                    TongTien = d.TongTien ?? 0,
                    ThoiGianDat = d.ThoiGianDat ?? DateTime.Now
                }).ToList();

            var lichSuDonHang = db.DonHangs
                .Where(d => d.MaKH == maKH && (d.TrangThai == "Hoàn tất" || d.TrangThai == "Đã hủy"))
                .OrderByDescending(d => d.ThoiGianDat)
                .Select(d => new DonHangModel
                {
                    MaDon = d.MaDon,
                    MaKH = d.MaKH,
                    MaNH = d.MaNH,
                    TrangThai = d.TrangThai,
                    TongTien = d.TongTien ?? 0,
                    ThoiGianDat = d.ThoiGianDat ?? DateTime.Now
                }).ToList();

            return View(new DonHangTongHopViewModel
            {
                DonHangDangXuLy = donHangDangXuLy,
                LichSuDonHang = lichSuDonHang
            });
        }

        // ===================== THEO DÕI ĐƠN HÀNG =====================
        public ActionResult TheoDoiDonHang(string maDon)
        {
            if (!KiemTraDangNhap())
            {
                TempData["Msg"] = "Vui lòng đăng nhập để theo dõi đơn hàng!";
                return RedirectToAction("TrangChu", "Home");
            }

            string maKH = Session["MaKH"] as string;

            // Load DonHang kèm KhachHang, NhaHang, Shipper
            var donHangEntity = db.DonHangs
                     .Include("KhachHang")
                     .Include("NhaHang")
                     .Include("Shipper")
                     .FirstOrDefault(d => d.MaDon == maDon && d.MaKH == maKH);
        
            if (donHangEntity == null)
                return HttpNotFound();

            // Mapping sang viewmodel
            var donHang = new TheoDoiDonHangViewModel
            {
                MaDon = donHangEntity.MaDon,
                TenKH = donHangEntity.KhachHang?.TenKH,
                DiaChi = donHangEntity.DiaChi,       // Lấy trực tiếp từ DonHang
                Sdt = donHangEntity.Sdt,             // Lấy trực tiếp từ DonHang
                TenNH = donHangEntity.NhaHang?.TenNH,
                MaShipper = donHangEntity.MaShipper,
                TrangThai = donHangEntity.TrangThai,
                TongTien = donHangEntity.TongTien ?? 0,
                ThoiGianDat = donHangEntity.ThoiGianDat ?? DateTime.Now,
                NhaHangLatitude = donHangEntity.NhaHang?.Latitude,
                NhaHangLongitude = donHangEntity.NhaHang?.Longitude,
                KhachHangLatitude = donHangEntity.Latitude ?? donHangEntity.KhachHang?.Latitude,
                KhachHangLongitude = donHangEntity.Longitude ?? donHangEntity.KhachHang?.Longitude
            };

            var chiTiet = db.ChiTietDonHangs
                .Where(c => c.MaDon == maDon)
                .Select(c => new ChiTietDonHangModel
                {
                    TenMon = c.MonAn.TenMon,
                    SoLuong = c.SoLuong ?? 0,
                    DonGia = c.DonGia ?? 0,
                    TongTien = (c.SoLuong ?? 0) * (c.DonGia ?? 0)
                }).ToList();

            var model = new TheoDoiDonHangFullViewModel
            {
                DonHang = donHang,
                ChiTietDonHang = chiTiet
            };

            return View(model);
        }

        // ===================== CHI TIẾT ĐƠN HÀNG (AJAX) =====================
        public JsonResult ChiTietDonHang(string id)
        {
            if (!KiemTraDangNhap())
                return Json(new { success = false, message = "Vui lòng đăng nhập!" }, JsonRequestBehavior.AllowGet);

            var chiTiet = (from ctdh in db.ChiTietDonHangs
                           join mon in db.MonAns on ctdh.MaMon equals mon.MaMon
                           where ctdh.MaDon == id
                           select new
                           {
                               TenMon = mon.TenMon,
                               SoLuong = ctdh.SoLuong ?? 0,
                               DonGia = ctdh.DonGia ?? 0
                           }).ToList();

            return Json(chiTiet, JsonRequestBehavior.AllowGet);
        }

        // ===================== MON AN THEO LOAI =====================
        public ActionResult MonAnTheoLoai(string maLoai)
        {
            if (string.IsNullOrEmpty(maLoai))
                return RedirectToAction("DanhMuc");

            AutoLoginTheoIP();

            var loaiMonAn = db.LoaiMonAns
                              .Where(l => l.MaLoai == maLoai)
                              .Select(l => new LoaiMonAnViewModel
                              {
                                  MaLoai = l.MaLoai,
                                  TenLoai = l.TenLoai,
                                  HinhAnh = l.HinhAnh
                              }).FirstOrDefault();

            if (loaiMonAn == null)
                return HttpNotFound();

            var nhaHangList = (from m in db.MonAns
                               where m.MaLoai == maLoai
                               join nh in db.NhaHangs on m.MaNH equals nh.MaNH
                               select nh)
                  .Distinct()
                  .Select(nh => new NhaHangViewModel
                  {
                      MaNH = nh.MaNH,
                      TenNH = nh.TenNH,
                      DiaChi = nh.DiaChi,
                      TrangThai = nh.TrangThai,
                      HinhAnh = nh.HinhAnh,
                      TongLuotMua = db.ChiTietDonHangs
                                     .Where(c => db.MonAns
                                                   .Where(mm => mm.MaNH == nh.MaNH && mm.MaLoai == maLoai)
                                                   .Select(mm => mm.MaMon)
                                                   .Contains(c.MaMon))
                                     .Sum(c => (int?)c.SoLuong) ?? 0
                  }).ToList();

            return View(new MonAnTheoLoaiViewModel
            {
                LoaiMonAn = loaiMonAn,
                NhaHang = nhaHangList
            });
        }

        // ===================== API LẤY VỊ TRÍ SHIPPER =====================
        [HttpGet]
        public JsonResult GetShipperLocation(string maDon)
        {
            var donHang = db.DonHangs.FirstOrDefault(d => d.MaDon == maDon);
            if (donHang == null || string.IsNullOrEmpty(donHang.MaShipper))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var shipper = db.Shippers.FirstOrDefault(s => s.MaShipper == donHang.MaShipper);
            if (shipper == null || shipper.Latitude == null || shipper.Longitude == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                latitude = shipper.Latitude,
                longitude = shipper.Longitude
            }, JsonRequestBehavior.AllowGet);
        }

        // ===================== API LẤY ROUTE THỰC TẾ =====================
        // Trả về coords (mảng lat/lng), distance (m) và duration (s)
        [HttpGet]
        public JsonResult GetShipperRoute(string maDon)
        {
            if (!KiemTraDangNhap())
                return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);

            var donHang = db.DonHangs.FirstOrDefault(d => d.MaDon == maDon);
            if (donHang == null || string.IsNullOrEmpty(donHang.MaShipper))
                return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);

            var shipper = db.Shippers.FirstOrDefault(s => s.MaShipper == donHang.MaShipper);
            if (shipper == null)
                return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);

            // Lấy vị trí shipper
            var latShipper = shipper.Latitude ?? donHang.ShipperLatitude ?? 0;
            var lngShipper = shipper.Longitude ?? donHang.ShipperLongitude ?? 0;

            // Lấy vị trí khách (ưu tiên DonHang.Latitude/Longtitude sau đến KhachHang)
            double latKH = 0, lngKH = 0;
            if (donHang.Latitude.HasValue && donHang.Longitude.HasValue)
            {
                latKH = donHang.Latitude.Value;
                lngKH = donHang.Longitude.Value;
            }
            else if (donHang.MaKH != null)
            {
                var kh = db.KhachHangs.FirstOrDefault(k => k.MaKH == donHang.MaKH);
                if (kh != null)
                {
                    latKH = kh.Latitude ?? 0;
                    lngKH = kh.Longitude ?? 0;
                }
            }

            // Nếu không có tọa độ hợp lệ -> trả null
            if (latShipper == 0 && lngShipper == 0) return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);
            if (latKH == 0 && lngKH == 0) return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", ORS_API_KEY);

                    // ORS accepts coordinates as lng,lat
                    var url = $"https://api.openrouteservice.org/v2/directions/driving-car?start={lngShipper},{latShipper}&end={lngKH},{latKH}";

                    var response = client.GetAsync(url).Result;
                    if (!response.IsSuccessStatusCode)
                        return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);

                    var json = response.Content.ReadAsStringAsync().Result;
                    var obj = JObject.Parse(json);

                    var coords = obj["features"]?[0]?["geometry"]?["coordinates"];
                    var summary = obj["features"]?[0]?["properties"]?["summary"];

                    if (coords == null)
                        return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);

                    // coords is array of [lng, lat] pairs; convert to list of {lat, lng}
                    var route = new List<object>();
                    foreach (var c in coords)
                    {
                        double lng = c[0].Value<double>();
                        double lat = c[1].Value<double>();
                        route.Add(new { lat = lat, lng = lng });
                    }

                    double distance = summary?["distance"]?.Value<double>() ?? 0; // meters
                    double duration = summary?["duration"]?.Value<double>() ?? 0; // seconds

                    return Json(new
                    {
                        route = route,
                        distance = distance,
                        duration = duration
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetShipperRoute error: " + ex.ToString());
                return Json(new { route = (object)null }, JsonRequestBehavior.AllowGet);
            }
        }

        // ===================== API LẤY FULL TRACKING INFO =====================
        // ===================== HÀM CHUYỂN TIẾNG VIỆT CÓ DẤU -> KHÔNG DẤU =====================
        // ===================== HÀM LOẠI BỎ DẤU =====================
        private string LoaiBoDauTiengViet(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string[] vietChars = new string[]
            {
        "aAeEoOuUiIdDyY",
        "áàạảãâấầậẩẫăắằặẳẵ",
        "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
        "éèẹẻẽêếềệểễ",
        "ÉÈẸẺẼÊẾỀỆỂỄ",
        "óòọỏõôốồộổỗơớờợởỡ",
        "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
        "úùụủũưứừựửữ",
        "ÚÙỤỦŨƯỨỪỰỬỮ",
        "íìịỉĩ",
        "ÍÌỊỈĨ",
        "đ",
        "Đ",
        "ýỳỵỷỹ",
        "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietChars.Length; i++)
            {
                for (int j = 0; j < vietChars[i].Length; j++)
                {
                    text = text.Replace(vietChars[i][j], vietChars[0][i - 1]);
                }
            }
            return text;
        }

        // ===================== HÀM GEO CODE =====================
        private (double? lat, double? lng) GeoCode(string address)
        {
            if (string.IsNullOrEmpty(address)) return (null, null);

            try
            {
                // 1. Loại bỏ dấu
                string cleanedAddress = LoaiBoDauTiengViet(address);

                // 2. Thêm city/country nếu cần
                if (!cleanedAddress.ToLower().Contains("hcm") && !cleanedAddress.ToLower().Contains("tphcm"))
                    cleanedAddress += ", Ho Chi Minh, Vietnam";

                using (var client = new HttpClient())
                {
                    var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(cleanedAddress)}";
                    client.DefaultRequestHeaders.Add("User-Agent", "ZFoodApp");

                    var resp = client.GetAsync(url).Result;
                    if (!resp.IsSuccessStatusCode) return (null, null);

                    var json = resp.Content.ReadAsStringAsync().Result;
                    var arr = JArray.Parse(json);
                    if (arr.Count == 0) return (null, null);

                    double lat = arr[0]["lat"].Value<double>();
                    double lng = arr[0]["lon"].Value<double>();
                    return (lat, lng);
                }
            }
            catch
            {
                return (null, null);
            }
        }

        // ===================== API LẤY FULL TRACKING INFO =====================
        [HttpGet]
        public JsonResult GetTrackingInfo(string maDon)
        {
            if (!KiemTraDangNhap())
                return Json(new { success = false, message = "Chưa đăng nhập" }, JsonRequestBehavior.AllowGet);

            var don = db.DonHangs.FirstOrDefault(d => d.MaDon == maDon);
            if (don == null) return Json(new { success = false, message = "Không tìm thấy đơn" }, JsonRequestBehavior.AllowGet);

            var nhaHang = db.NhaHangs.FirstOrDefault(n => n.MaNH == don.MaNH);
            var khachHang = db.KhachHangs.FirstOrDefault(k => k.MaKH == don.MaKH);

            double restLat = 0, restLng = 0;
            if (nhaHang != null)
            {
                if (nhaHang.Latitude.HasValue && nhaHang.Longitude.HasValue)
                {
                    restLat = nhaHang.Latitude.Value;
                    restLng = nhaHang.Longitude.Value;
                }
                else
                {
                    var coords = GeoCode(nhaHang.DiaChi);
                    restLat = coords.lat ?? 0;
                    restLng = coords.lng ?? 0;
                }
            }

            double custLat = 0, custLng = 0;
            if (don.Latitude.HasValue && don.Longitude.HasValue)
            {
                custLat = don.Latitude.Value;
                custLng = don.Longitude.Value;
            }
            else if (khachHang != null && khachHang.Latitude.HasValue && khachHang.Longitude.HasValue)
            {
                custLat = khachHang.Latitude.Value;
                custLng = khachHang.Longitude.Value;
            }
            else
            {
                var coords = GeoCode(don.DiaChi);
                custLat = coords.lat ?? 0;
                custLng = coords.lng ?? 0;
            }

            var restaurant = (restLat != 0 && restLng != 0) ? new { lat = restLat, lng = restLng, name = nhaHang?.TenNH } : null;
            var customer = (custLat != 0 && custLng != 0) ? new { lat = custLat, lng = custLng, name = "Khách hàng" } : null;

            double shipLat = 0, shipLng = 0;
            if (!string.IsNullOrEmpty(don.MaShipper))
            {
                var shipper = db.Shippers.FirstOrDefault(s => s.MaShipper == don.MaShipper);
                if (shipper != null && shipper.Latitude.HasValue && shipper.Longitude.HasValue)
                {
                    shipLat = shipper.Latitude.Value;
                    shipLng = shipper.Longitude.Value;
                }
            }
            var shipperMarker = (shipLat != 0 && shipLng != 0) ? new { lat = shipLat, lng = shipLng, maShipper = don.MaShipper } : null;

            return Json(new { success = true, restaurant, customer, shipper = shipperMarker }, JsonRequestBehavior.AllowGet);
        }
    }
}
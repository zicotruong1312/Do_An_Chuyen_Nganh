using ĐACN;
using OfficeOpenXml; // nếu export Excel
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;


namespace FoodDeliveryDB.Controllers
{
    public class AdminController : Controller
    {
        private readonly FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        // 🟢 Danh sách cửa hàng
        public ActionResult DanhSachCuaHang(string keyword, string status)
        {
            var ds = db.NhaHangs.Include("TaiKhoan").AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                ds = ds.Where(nh => nh.TenNH.Contains(keyword) || nh.DiaChi.Contains(keyword));

            if (!string.IsNullOrEmpty(status))
                ds = ds.Where(nh => nh.TrangThai == status);

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;

            return View(ds.ToList());
        }

        // 🟢 Chi tiết nhà hàng (chế độ xem hoặc chỉnh sửa)
        public ActionResult ChiTietNhaHang(string id, bool? edit)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("DanhSachCuaHang");

            var nh = db.NhaHangs.Include("TaiKhoan").FirstOrDefault(n => n.MaNH == id);
            if (nh == null)
                return HttpNotFound();

            ViewBag.EditMode = edit ?? false;
            return View(nh);
        }

        [HttpPost]
        public ActionResult SuaNhaHang(string MaNH, string TenNH, string DiaChi, string SDT, string TrangThai,
                        string TenDangNhap, string MatKhau, HttpPostedFileBase HinhNhaHangFile)
        {
            var nhaHang = db.NhaHangs.Include("TaiKhoan").FirstOrDefault(n => n.MaNH == MaNH);
            if (nhaHang == null)
                return HttpNotFound();

            nhaHang.TenNH = TenNH;
            nhaHang.DiaChi = DiaChi;
            nhaHang.SDT = SDT;
            nhaHang.TrangThai = TrangThai;

            // ✅ Upload ảnh mới nếu có
            if (HinhNhaHangFile != null && HinhNhaHangFile.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/images/nhahang/");
                Directory.CreateDirectory(folderPath);

                string fileName = Path.GetFileNameWithoutExtension(HinhNhaHangFile.FileName);
                string extension = Path.GetExtension(HinhNhaHangFile.FileName);
                string newFileName = fileName + "_" + DateTime.Now.Ticks + extension;
                string savePath = Path.Combine(folderPath, newFileName);

                HinhNhaHangFile.SaveAs(savePath);
                nhaHang.HinhAnh = "~/images/nhahang/" + newFileName;
            }

            // ✅ Cập nhật tài khoản
            if (nhaHang.TaiKhoan != null)
            {
                nhaHang.TaiKhoan.TenDangNhap = TenDangNhap;
                if (!string.IsNullOrEmpty(MatKhau))
                    nhaHang.TaiKhoan.MatKhau = MatKhau;
            }

            db.SaveChanges();
            TempData["Message"] = "✅ Cập nhật thông tin cửa hàng thành công!";
            // ✅ Reload lại chi tiết ngay (có ảnh mới)
            return RedirectToAction("ChiTietNhaHang", new { id = nhaHang.MaNH });
        }


        [HttpPost]
        public ActionResult XoaNhaHang(string id)
        {
            var nh = db.NhaHangs.Find(id);
            if (nh != null)
            {
                db.NhaHangs.Remove(nh);
                db.SaveChanges();
                TempData["Message"] = "🗑️ Đã xóa nhà hàng thành công!";
            }
            return RedirectToAction("DanhSachCuaHang");
        }

        // 🟢 Hiển thị form thêm cửa hàng
        [HttpGet]
        public ActionResult ThemNhaHang()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ThemNhaHang(NhaHang nh, string TenDangNhap, string MatKhau, HttpPostedFileBase HinhNhaHangFile)
        {
            // 1️⃣ Tạo tài khoản
            TaiKhoan tk = new TaiKhoan
            {
                MaTK = "TK" + Guid.NewGuid().ToString("N").Substring(0, 6),
                TenDangNhap = TenDangNhap,
                MatKhau = MatKhau,
                VaiTro = "CuaHang",
                TrangThai = true
            };
            db.TaiKhoans.Add(tk);
            db.SaveChanges();

            // 2️⃣ Tạo nhà hàng
            nh.MaNH = "NH" + Guid.NewGuid().ToString("N").Substring(0, 6);
            nh.MaTK = tk.MaTK;

            // 3️⃣ Upload ảnh (giống logic Shipper)
            string folderPath = Server.MapPath("~/images/nhahang/");
            Directory.CreateDirectory(folderPath);

            if (HinhNhaHangFile != null && HinhNhaHangFile.ContentLength > 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(HinhNhaHangFile.FileName);
                string extension = Path.GetExtension(HinhNhaHangFile.FileName);
                string newFileName = fileName + "_" + DateTime.Now.Ticks + extension;
                string savePath = Path.Combine(folderPath, newFileName);

                HinhNhaHangFile.SaveAs(savePath);
                nh.HinhAnh = "~/images/nhahang/" + newFileName;
            }
            else
            {
                nh.HinhAnh = "~/images/default-restaurant.png"; // fallback
            }

            db.NhaHangs.Add(nh);
            db.SaveChanges();

            TempData["Message"] = "✅ Thêm nhà hàng mới thành công!";
            return RedirectToAction("DanhSachCuaHang");
        }


        [HttpPost]
        public ActionResult DoiTrangThaiNhaHang(string id)
        {
            var nh = db.NhaHangs.Include("TaiKhoan").FirstOrDefault(n => n.MaNH == id);
            if (nh == null || nh.TaiKhoan == null)
                return HttpNotFound();

            nh.TaiKhoan.TrangThai = !(nh.TaiKhoan.TrangThai ?? false);
            nh.TrangThai = nh.TaiKhoan.TrangThai == true ? "Đang mở cửa" : "Đã đóng cửa";
            db.SaveChanges();

            TempData["Message"] = nh.TaiKhoan.TrangThai == true
                ? $"✅ Đã mở lại cửa hàng {nh.TenNH}"
                : $"🔒 Đã khóa cửa hàng {nh.TenNH}";

            return RedirectToAction("DanhSachCuaHang");
        }




        // 🟢 4️⃣ Trang Cấp phép cửa hàng
        public ActionResult CapPhepCuaHang()
        {
            var choXacNhan = db.NhaHangs
                .Where(n => n.TrangThai == "Chờ xác nhận")
                .ToList();
            return View(choXacNhan);
        }

        // 🟢 5️⃣ Chấp nhận cửa hàng
        [HttpPost]
        public ActionResult ChapNhan(string id)
        {
            var nhaHang = db.NhaHangs.FirstOrDefault(n => n.MaNH == id);
            if (nhaHang != null)
            {
                nhaHang.TrangThai = "Đang mở cửa";
                db.SaveChanges();

                SendEmailToRestaurant(nhaHang.SDT, nhaHang.TenNH, true);
                TempData["Success"] = $"✅ Đã cấp phép cho cửa hàng {nhaHang.TenNH}";
            }
            return RedirectToAction("CapPhepCuaHang");
        }

        // 🛑 6️⃣ Từ chối cửa hàng
        [HttpPost]
        public ActionResult TuChoi(string id)
        {
            var nhaHang = db.NhaHangs.FirstOrDefault(n => n.MaNH == id);
            if (nhaHang != null)
            {
                nhaHang.TrangThai = "Đã đóng cửa";
                db.SaveChanges();

                SendEmailToRestaurant(nhaHang.SDT, nhaHang.TenNH, false);
                TempData["Error"] = $"❌ Đã từ chối cửa hàng {nhaHang.TenNH}";
            }
            return RedirectToAction("CapPhepCuaHang");
        }

        // 📧 7️⃣ Gửi mail thông báo
        private void SendEmailToRestaurant(string email, string tenNhaHang, bool chapNhan)
        {
            try
            {
                string subject = chapNhan ? "Cửa hàng đã được phê duyệt" : "Cửa hàng bị từ chối";
                string body = chapNhan
                    ? $"Xin chào {tenNhaHang}, cửa hàng của bạn đã được phê duyệt và có thể hoạt động trên hệ thống FoodDelivery."
                    : $"Xin chào {tenNhaHang}, rất tiếc cửa hàng của bạn chưa được phê duyệt trên hệ thống FoodDelivery.";

                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                mail.From = new MailAddress("lynki1509@gmail.com");
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("lynki1509@gmail.com", "123456");
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không thể gửi email: " + ex.Message);
            }
        }

        public ActionResult DanhSachShipper(string keyword)
        {
            var shippers = db.Shippers.Include("TaiKhoan").AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                shippers = shippers.Where(s => s.TenShipper.Contains(keyword) || s.SDT.Contains(keyword));
            }

            // Sắp xếp theo MaShipper tăng dần (thứ tự bé → lớn)
            shippers = shippers.OrderBy(s => s.MaShipper);

            return View(shippers.ToList());
        }

        public ActionResult ChiTietShipper(string id, bool? edit)
        {
            var shipper = db.Shippers.Include("TaiKhoan").FirstOrDefault(s => s.MaShipper == id);
            if (shipper == null)
                return HttpNotFound();

            ViewBag.EditMode = edit ?? false;
            return View(shipper);
        }

        // 🟢 Hiển thị form thêm Shipper
        [HttpGet]
        public ActionResult ThemShipper()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ThemShipper(Shipper shipper, string TenDangNhap, string MatKhau, string VaiTro, bool TrangThai, HttpPostedFileBase HinhShipperFile)
        {
            using (var db = new FoodDeliveryDBEntities())
            {
                // 1. Tạo tài khoản mới
                var taiKhoan = new TaiKhoan
                {
                    MaTK = "TK" + DateTime.Now.Ticks,
                    TenDangNhap = TenDangNhap,
                    MatKhau = MatKhau,
                    VaiTro = VaiTro,
                    TrangThai = TrangThai
                };
                db.TaiKhoans.Add(taiKhoan);
                db.SaveChanges();

                // 2. Tạo Shipper mới
                shipper.MaShipper = "SP" + DateTime.Now.Ticks;
                shipper.MaTK = taiKhoan.MaTK;

                // 3. Upload ảnh nếu có
                if (HinhShipperFile != null && HinhShipperFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(HinhShipperFile.FileName);
                    string extension = Path.GetExtension(HinhShipperFile.FileName);
                    string newFileName = fileName + "_" + DateTime.Now.Ticks + extension;
                    string savePath = Path.Combine(Server.MapPath("~/Content/images/shipper/"), newFileName);

                    Directory.CreateDirectory(Server.MapPath("~/Content/images/shipper/"));
                    HinhShipperFile.SaveAs(savePath);

                    shipper.HinhAnh = "/Content/images/shipper/" + newFileName;
                }
                else
                {
                    // Ảnh mặc định nếu không upload
                    shipper.HinhAnh = "/Content/images/default-avatar.png";
                }

                db.Shippers.Add(shipper);
                db.SaveChanges();

                TempData["Message"] = "Thêm Shipper mới thành công!";
                return RedirectToAction("DanhSachShipper");
            }
        }


        [HttpPost]
        public ActionResult SuaShipper(Shipper model, string TenDangNhap, string MatKhau, string VaiTro, bool TrangThai, HttpPostedFileBase HinhShipperFile)
        {
            var shipper = db.Shippers.Include("TaiKhoan").FirstOrDefault(s => s.MaShipper == model.MaShipper);
            if (shipper == null)
                return HttpNotFound();

            shipper.TenShipper = model.TenShipper;
            shipper.SDT = model.SDT;
            shipper.BienSoXe = model.BienSoXe;

            // ✅ Xử lý ảnh upload
            if (HinhShipperFile != null && HinhShipperFile.ContentLength > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var fileExtension = Path.GetExtension(HinhShipperFile.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Message"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, bmp).";
                    return RedirectToAction("ChiTietShipper", new { id = model.MaShipper, edit = true });
                }
                
                // Validate file size (max 5MB)
                if (HinhShipperFile.ContentLength > 5 * 1024 * 1024)
                {
                    TempData["Message"] = "Kích thước file không được vượt quá 5MB.";
                    return RedirectToAction("ChiTietShipper", new { id = model.MaShipper, edit = true });
                }

                string fileName = Path.GetFileNameWithoutExtension(HinhShipperFile.FileName);
                string extension = Path.GetExtension(HinhShipperFile.FileName);
                string newFileName = fileName + "_" + DateTime.Now.Ticks + extension;
                string savePath = Path.Combine(Server.MapPath("~/Content/images/shipper/"), newFileName);

                Directory.CreateDirectory(Server.MapPath("~/Content/images/shipper/"));
                
                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(shipper.HinhAnh))
                {
                    var oldPath = Server.MapPath("~" + shipper.HinhAnh);
                    if (System.IO.File.Exists(oldPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldPath);
                        }
                        catch
                        {
                            // Ignore lỗi xóa file cũ
                        }
                    }
                }
                
                HinhShipperFile.SaveAs(savePath);
                shipper.HinhAnh = "/Content/images/shipper/" + newFileName;
            }


            // Cập nhật tài khoản
            if (shipper.TaiKhoan != null)
            {
                shipper.TaiKhoan.TenDangNhap = TenDangNhap;
                shipper.TaiKhoan.MatKhau = MatKhau;
                shipper.TaiKhoan.VaiTro = VaiTro;
                shipper.TaiKhoan.TrangThai = TrangThai;
            }

            db.SaveChanges();

            TempData["Message"] = "Cập nhật Shipper thành công!";
            // ✅ Sau khi lưu, chuyển hướng lại trang chi tiết (sẽ load lại dữ liệu mới)
            return RedirectToAction("ChiTietShipper", new { id = model.MaShipper });
        }

        [HttpPost]
        public ActionResult XoaShipper(string id)
        {
            var shipper = db.Shippers.Find(id);
            if (shipper != null)
            {
                db.Shippers.Remove(shipper);
                db.SaveChanges();
                TempData["Message"] = "Xóa Shipper thành công!";
            }
            return RedirectToAction("DanhSachShipper");
        }


        // 🟢🛑 Khóa hoặc mở tài khoản Shipper
        [HttpPost]
        public ActionResult DoiTrangThaiShipper(string id)
        {
            var shipper = db.Shippers.Include("TaiKhoan").FirstOrDefault(s => s.MaShipper == id);
            if (shipper == null || shipper.TaiKhoan == null)
                return HttpNotFound();

            // Đảo ngược trạng thái hiện tại (nếu null thì mặc định là false)
            shipper.TaiKhoan.TrangThai = !(shipper.TaiKhoan.TrangThai ?? false);
            db.SaveChanges();

            TempData["Message"] = shipper.TaiKhoan.TrangThai == true
                ? $"✅ Đã mở khóa tài khoản cho shipper {shipper.TenShipper}"
                : $"🔒 Đã khóa tài khoản shipper {shipper.TenShipper}";

            return RedirectToAction("DanhSachShipper");
        }
        // 📋 Danh sách người dùng
        public ActionResult DanhSachNguoiDung(string keyword)
        {
            var ds = db.KhachHangs.Include("TaiKhoan").AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                ds = ds.Where(k => k.TenKH.Contains(keyword) || k.SDT.Contains(keyword));

            ViewBag.Keyword = keyword;
            return View(ds.ToList());
        }

        public ActionResult ChiTietNguoiDung(string id, bool? edit)
        {
            var kh = db.KhachHangs.Find(id);
            ViewBag.EditMode = edit ?? false;
            return View(kh);
        }


        // ➕ Thêm người dùng
        [HttpGet]
        public ActionResult ThemNguoiDung()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ThemNguoiDung(KhachHang kh)
        {
            string tenDangNhap = Request["TenDangNhap"];
            string matKhau = Request["MatKhau"];

            // ✅ Tạo tài khoản mới
            TaiKhoan tk = new TaiKhoan
            {
                MaTK = "TK" + Guid.NewGuid().ToString("N").Substring(0, 6),
                TenDangNhap = tenDangNhap,
                MatKhau = matKhau,
                VaiTro = "NguoiDung",
                TrangThai = true
            };

            db.TaiKhoans.Add(tk);

            // ✅ Gán vào khách hàng
            kh.MaKH = "KH" + Guid.NewGuid().ToString("N").Substring(0, 6);
            kh.MaTK = tk.MaTK;

            db.KhachHangs.Add(kh);
            db.SaveChanges();

            TempData["Success"] = "Thêm người dùng mới thành công!";
            return RedirectToAction("DanhSachNguoiDung");
        }

        // ✏️ Sửa người dùng
        [HttpGet]
        public ActionResult SuaNguoiDung(string id)
        {
            var kh = db.KhachHangs.Include("TaiKhoan").FirstOrDefault(k => k.MaKH == id);
            if (kh == null) return HttpNotFound();

            return View(kh);
        }
        [HttpPost]
        public ActionResult SuaNguoiDung(KhachHang model, string TenDangNhap, string MatKhau, string VaiTro, string TrangThai)
        {
            var kh = db.KhachHangs.Include("TaiKhoan").FirstOrDefault(k => k.MaKH == model.MaKH);
            if (kh != null)
            {
                kh.TenKH = model.TenKH;
                kh.SDT = model.SDT;
                kh.DiaChi = model.DiaChi;

                if (kh.TaiKhoan != null)
                {
                    kh.TaiKhoan.TenDangNhap = TenDangNhap;
                    kh.TaiKhoan.MatKhau = MatKhau;
                    kh.TaiKhoan.VaiTro = VaiTro;
                    kh.TaiKhoan.TrangThai = (TrangThai == "true");
                }

                db.SaveChanges();
                TempData["Message"] = "✅ Đã cập nhật thông tin người dùng thành công!";
            }

            return RedirectToAction("ChiTietNguoiDung", new { id = model.MaKH });
        }


        // 🗑️ Xóa người dùng
        [HttpPost]
        public ActionResult XoaNguoiDung(string id)
        {
            var kh = db.KhachHangs.Include("TaiKhoan").FirstOrDefault(k => k.MaKH == id);
            if (kh == null) return HttpNotFound();

            // Xóa tài khoản nếu có
            if (kh.TaiKhoan != null)
                db.TaiKhoans.Remove(kh.TaiKhoan);

            db.KhachHangs.Remove(kh);
            db.SaveChanges();

            TempData["Message"] = "🗑️ Đã xóa người dùng thành công!";
            return RedirectToAction("DanhSachNguoiDung");
        }

        // 🔒 Mở/Khóa người dùng
        [HttpPost]
        public ActionResult DoiTrangThaiNguoiDung(string id)
        {
            var kh = db.KhachHangs.Include("TaiKhoan").FirstOrDefault(k => k.MaKH == id);
            if (kh == null || kh.TaiKhoan == null)
                return HttpNotFound();

            kh.TaiKhoan.TrangThai = !(kh.TaiKhoan.TrangThai ?? false);
            db.SaveChanges();

            TempData["Message"] = kh.TaiKhoan.TrangThai == true
                ? $"✅ Đã mở khóa tài khoản người dùng {kh.TenKH}"
                : $"🔒 Đã khóa tài khoản người dùng {kh.TenKH}";

            return RedirectToAction("DanhSachNguoiDung");
        }



        // 🟡 Danh sách Shipper chờ cấp phép
        public ActionResult CapPhepShipper(string keyword)
        {
            var dsShipper = db.Shippers.Include("TaiKhoan")
                .Where(s => s.TaiKhoan.TrangThai == false) // chỉ hiển thị tài khoản bị khóa (chờ duyệt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                dsShipper = dsShipper.Where(s =>
                    s.TenShipper.Contains(keyword) ||
                    s.SDT.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(dsShipper.ToList());
        }

        // 🟢 Chấp nhận Shipper
        [HttpPost]
        public ActionResult ChapNhanShipper(string id)
        {
            var shipper = db.Shippers.FirstOrDefault(s => s.MaShipper == id);
            if (shipper == null) return HttpNotFound();

            shipper.TaiKhoan.TrangThai = true; // kích hoạt tài khoản
            db.SaveChanges();

            TempData["Message"] = "Đã chấp nhận người giao hàng thành công.";
            return RedirectToAction("CapPhepShipper");
        }

        // 🔴 Từ chối Shipper
        [HttpPost]
        public ActionResult TuChoiShipper(string id)
        {
            var shipper = db.Shippers.FirstOrDefault(s => s.MaShipper == id);
            if (shipper == null) return HttpNotFound();

            // Xóa tài khoản + Shipper
            var tk = shipper.TaiKhoan;
            db.Shippers.Remove(shipper);
            db.TaiKhoans.Remove(tk);
            db.SaveChanges();

            TempData["Message"] = "Đã từ chối và xóa tài khoản Shipper.";
            return RedirectToAction("CapPhepShipper");
        }
        // ==================== THỐNG KÊ ====================
        public ActionResult ThongKe(string type = "Tháng")
        {
            ViewBag.Type = type;

            // 🟢 Thống kê doanh thu theo tháng
            var doanhThu = db.DonHangs
                .Where(d => d.ThoiGianDat != null && d.TongTien != null)
                .GroupBy(d => new
                {
                    Nam = d.ThoiGianDat.HasValue ? d.ThoiGianDat.Value.Year : 0,
                    Thang = d.ThoiGianDat.HasValue ? d.ThoiGianDat.Value.Month : 0
                })
                .Select(g => new
                {
                    Nam = g.Key.Nam,
                    Thang = g.Key.Thang,
                    DoanhThu = g.Sum(x => (decimal?)x.TongTien) ?? 0
                }).ToList();

            // 🟢 Nếu chọn thống kê theo quý
            if (type == "Quý")
            {
                doanhThu = doanhThu
                    .GroupBy(m => new { m.Nam, Quy = (m.Thang - 1) / 3 + 1 })
                    .Select(g => new
                    {
                        Nam = g.Key.Nam,
                        Thang = g.Key.Quy,
                        DoanhThu = g.Sum(x => x.DoanhThu)
                    }).ToList();
            }

            // 🧮 Thống kê người dùng theo thời gian
            var khachHangCount = db.KhachHangs.Count();
            var shipperCount = db.Shippers.Count();
            var nhaHangCount = db.NhaHangs.Count();

            // 🧮 Có thể thêm: tổng số đơn hàng
            var tongDonHang = db.DonHangs.Count();

            // Gửi dữ liệu sang View
            ViewBag.KhachHang = khachHangCount;
            ViewBag.Shipper = shipperCount;
            ViewBag.NhaHang = nhaHangCount;
            ViewBag.DoanhThu = doanhThu;
            ViewBag.TongDonHang = tongDonHang;

            return View();
        }
        [HttpPost]
        public FileResult ExportThongKe(string format)
        {
            var data = db.DonHangs
                .Where(d => d.ThoiGianDat != null && d.TongTien != null)
                .GroupBy(d => new
                {
                    Nam = d.ThoiGianDat.HasValue ? d.ThoiGianDat.Value.Year : 0,
                    Thang = d.ThoiGianDat.HasValue ? d.ThoiGianDat.Value.Month : 0
                })
                .Select(g => new
                {
                    Nam = g.Key.Nam,
                    Thang = g.Key.Thang,
                    TongDoanhThu = g.Sum(x => (decimal?)x.TongTien) ?? 0
                })
                .OrderBy(x => x.Nam)
                .ThenBy(x => x.Thang)
                .ToList();

            if (format == "excel")
            {
                ExcelPackage.License.SetNonCommercialPersonal("Team");

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("ThongKe");

                    // Tiêu đề
                    ws.Cells["A1"].Value = "BÁO CÁO THỐNG KÊ DOANH THU";
                    ws.Cells["A1:C1"].Merge = true;
                    ws.Cells["A1"].Style.Font.Bold = true;
                    ws.Cells["A1"].Style.Font.Size = 16;
                    ws.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    // Header cột
                    ws.Cells["A3"].Value = "Năm";
                    ws.Cells["B3"].Value = "Tháng";
                    ws.Cells["C3"].Value = "Doanh thu (VNĐ)";
                    ws.Cells["A3:C3"].Style.Font.Bold = true;

                    // Dữ liệu
                    int row = 4;
                    foreach (var d in data)
                    {
                        ws.Cells[row, 1].Value = d.Nam;
                        ws.Cells[row, 2].Value = d.Thang;
                        ws.Cells[row, 3].Value = d.TongDoanhThu;
                        row++;
                    }

                    // Tự động chỉnh độ rộng cột
                    ws.Cells.AutoFitColumns();

                    // Xuất file Excel ra stream
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ThongKe_FoodDelivery.xlsx");
                }
            }
            return null;
        }

        // ==================== TRUY CẬP NHANH CÁC VIEW ====================
        public ActionResult AdminViews()
        {
            return View(); // Đây có thể là trang dashboard với link tới các view khác
        }

        // Danh sách
        public ActionResult QuickDanhSachCuaHang() => RedirectToAction("DanhSachCuaHang");
        public ActionResult QuickDanhSachNguoiDung() => RedirectToAction("DanhSachNguoiDung");
        public ActionResult QuickDanhSachShipper() => RedirectToAction("DanhSachShipper");

        // Chi tiết
        public ActionResult QuickChiTietNhaHang(string id) => RedirectToAction("ChiTietNhaHang", new { id });
        public ActionResult QuickChiTietNguoiDung(string id) => RedirectToAction("ChiTietNguoiDung", new { id });
        public ActionResult QuickChiTietShipper(string id) => RedirectToAction("ChiTietShipper", new { id });

        // Thêm mới
        public ActionResult QuickThemNhaHang() => RedirectToAction("ThemNhaHang");
        public ActionResult QuickThemNguoiDung() => RedirectToAction("ThemNguoiDung");
        public ActionResult QuickThemShipper() => RedirectToAction("ThemShipper");

        // Cấp phép
        public ActionResult QuickCapPhepCuaHang() => RedirectToAction("CapPhepCuaHang");
        public ActionResult QuickCapPhepShipper() => RedirectToAction("CapPhepShipper");

        // Thống kê
        public ActionResult QuickThongKe() => RedirectToAction("ThongKe");
    }
}
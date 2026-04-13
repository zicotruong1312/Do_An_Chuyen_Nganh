using ĐACN.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace ĐACN.Controllers
{
    public class AccountController : Controller
    {
        private readonly FoodDeliveryDBEntities db = new FoodDeliveryDBEntities();

        // ====================== TRANG ĐĂNG NHẬP ======================
        [HttpGet]
        public ActionResult Login()
        {
            AutoLoginTheoIP();
            var tk = Session["TaiKhoan"] as TaiKhoan;
            if (tk != null)
            {
                if (tk.VaiTro == "Shipper")
                    return RedirectToAction("Index", "Shipper");
                else if (tk.VaiTro == "Admin")
                    return RedirectToAction("DanhSachCuaHang", "Admin");
                else if (tk.VaiTro == "NhaHang")
                    return RedirectToAction("ThongKe", "NhaHang");
                else
                    return RedirectToAction("TrangChu", "Home");
            }
            return View();
        }

        // ====================== LOGIN (AJAX) ======================
        [HttpPost]
        public JsonResult Login(string username, string password)
        {
            var tk = db.TaiKhoans.FirstOrDefault(x => x.TenDangNhap == username && x.MatKhau == password);

            if (tk == null)
                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng." });

            if (tk.TrangThai == false)
                return Json(new { success = false, message = "Tài khoản của bạn đã bị khóa hoặc chưa kích hoạt." });

            Session["TaiKhoan"] = tk;

            // Lưu session theo từng vai trò
            if (tk.VaiTro == "KhachHang")
            {
                var maKH = db.KhachHangs
                             .Where(k => k.MaTK == tk.MaTK)
                             .Select(k => k.MaKH)
                             .FirstOrDefault();
                if (!string.IsNullOrEmpty(maKH))
                    Session["MaKH"] = maKH;
            }
            else if (tk.VaiTro == "Shipper")
            {
                var shipper = db.Shippers.FirstOrDefault(s => s.MaTK == tk.MaTK);
                if (shipper != null)
                {
                    Session["MaShipper"] = shipper.MaShipper;
                    Session["Shipper"] = shipper;
                }
            }

            // Cookies Auto Login
            string ip = LayDiaChiIP();
            Response.Cookies.Add(new HttpCookie("ZFoodLoginIP", ip)
            {
                Expires = DateTime.Now.AddDays(30)
            });

            Response.Cookies.Add(new HttpCookie("ZFoodUser", tk.TenDangNhap)
            {
                Expires = DateTime.Now.AddDays(30)
            });

            return Json(new { success = true, role = tk.VaiTro });
        }

        // ====================== LOGOUT ======================
        public ActionResult Logout()
        {
            var tk = Session["TaiKhoan"] as TaiKhoan;
            string vaiTro = tk?.VaiTro;

            Session.Clear();

            // Xóa cookie
            if (Request.Cookies["ZFoodLoginIP"] != null)
            {
                var c = new HttpCookie("ZFoodLoginIP");
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }

            if (Request.Cookies["ZFoodUser"] != null)
            {
                var c = new HttpCookie("ZFoodUser");
                c.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(c);
            }

            if (vaiTro == "KhachHang")
                return RedirectToAction("TrangChu", "Home");

            return RedirectToAction("Login", "Account");
        }

        public ActionResult DangXuat()
        {
            return Logout();
        }

        // ====================== REGISTER (GET) ======================
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // ====================== REGISTER (POST - AJAX) ======================
        [HttpPost]
        public JsonResult Register(string username, string password, string email,
                                   string fullname, string phone, string address,
                                   string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "Tên đăng nhập và mật khẩu không được để trống." });

            if (db.TaiKhoans.Any(t => t.TenDangNhap == username))
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });

            try
            {
                string maTK = "TK" + DateTime.Now.Ticks;

                var taiKhoan = new TaiKhoan
                {
                    MaTK = maTK,
                    TenDangNhap = username,
                    MatKhau = password,
                    VaiTro = role,       // ✨ CHỌN THEO NGƯỜI DÙNG
                    TrangThai = true
                };
                db.TaiKhoans.Add(taiKhoan);

                // Tạo bản ghi theo role
                if (role == "KhachHang")
                {
                    string maKH = "KH" + DateTime.Now.Ticks;
                    db.KhachHangs.Add(new KhachHang
                    {
                        MaKH = maKH,
                        TenKH = fullname,
                        SDT = phone,
                        DiaChi = address,
                        MaTK = maTK
                    });
                }
                else if (role == "Shipper")
                {
                    string maSP = "SP" + DateTime.Now.Ticks;
                    db.Shippers.Add(new Shipper
                    {
                        MaShipper = maSP,
                        TenShipper = fullname,
                        SDT = phone,
                        MaTK = maTK
                    });
                }
                else if (role == "NhaHang")
                {
                    string maNH = "NH" + DateTime.Now.Ticks;
                    db.NhaHangs.Add(new NhaHang
                    {
                        MaNH = maNH,
                        TenNH = fullname,
                        SDT = phone,
                        DiaChi = address,
                        MaTK = maTK,
                        HinhAnh = null
                    });
                }
                // Admin: không tạo bảng phụ

                db.SaveChanges();

                // Gửi email đăng ký
                try
                {
                    if (!string.IsNullOrWhiteSpace(email))
                        SendVerificationEmail(email, username);
                }
                catch { }

                return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký: " + ex.Message });
            }
        }

        // ====================== GỬI EMAIL ======================
        private void SendVerificationEmail(string toEmail, string username)
        {
            string from = "your_email@gmail.com";
            string password = "your_app_password";
            string subject = "Xác thực tài khoản ZFood Delivery";
            string body = $"Xin chào {username},\n\nTài khoản của bạn đã được tạo thành công!";

            var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(from, password),
                EnableSsl = true
            };
            smtp.Send(from, toEmail, subject, body);
        }

        // ====================== LẤY IP ======================
        private string LayDiaChiIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
                ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip;
        }

        // ====================== AUTO LOGIN THEO IP ======================
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
                        else if (tk.VaiTro == "Shipper")
                        {
                            var shipper = db.Shippers.FirstOrDefault(s => s.MaTK == tk.MaTK);
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
    }
}

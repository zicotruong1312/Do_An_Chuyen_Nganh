using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐACN.Models
{
    public class AcceptedOrderView
    {
        // Thông tin đơn hàng
        public string MaDon { get; set; }
        public string TrangThai { get; set; }
        public decimal? TongTien { get; set; }
        public DateTime? ThoiGianDat { get; set; }

        // Thông tin giao hàng từ DonHang (đã được copy từ KhachHang khi nhận đơn)
        public string DiaChi { get; set; }
        public string Sdt { get; set; }

        // Thông tin khách hàng
        public string TenKhachHang { get; set; }
        public string DiaChiKhachHang { get; set; }
        public string SDTKhachHang { get; set; }

        // Thông tin nhà hàng
        public string TenNhaHang { get; set; }
        public string DiaChiNhaHang { get; set; }
        public string SDTNhaHang { get; set; }

        // Chi tiết đơn hàng (danh sách món ăn)
        public List<ChiTietMonAnViewModel> ChiTietMonAn { get; set; }

        // Thông tin tổng hợp
        public int SoLuongMon { get; set; }
        public string DanhSachMonTomTat { get; set; }
    }
}
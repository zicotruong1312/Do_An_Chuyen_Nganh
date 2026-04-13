using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐACN.Models
{
    public class DonHangDetailsViewModel
    {
        public string MaDon { get; set; }
        public string MaKH { get; set; }
        public string MaNH { get; set; }
        public string MaShipper { get; set; }
        public string TrangThai { get; set; }
        public decimal? TongTien { get; set; }
        public DateTime? ThoiGianDat { get; set; }
        public KhachHangInfoViewModel KhachHang { get; set; }
        public NhaHangInfoViewModel NhaHang { get; set; }
        public List<ChiTietMonAnViewModel> ChiTietMonAn { get; set; }
    }

    // Class con chứa thông tin khách hàng
    public class KhachHangInfoViewModel
    {
        public string TenKH { get; set; }
        public string DiaChi { get; set; }
        public string SDT { get; set; }
    }

    // Class con chứa thông tin nhà hàng
    public class NhaHangInfoViewModel
    {
        public string TenNH { get; set; }
        public string DiaChi { get; set; }
        public string SDT { get; set; }
    }

    // Class chứa thông tin chi tiết món ăn trong đơn hàng
    public class ChiTietMonAnViewModel
    {
        public string MaMon { get; set; }
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
    }
}
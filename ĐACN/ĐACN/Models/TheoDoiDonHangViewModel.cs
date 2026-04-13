using System;

namespace ĐACN.Models
{
    public class TheoDoiDonHangViewModel
    {
        public string MaDon { get; set; }           // Mã đơn hàng
        public string TenKH { get; set; }           // Tên khách hàng
        public string TenNH { get; set; }           // Tên nhà hàng
        public string DiaChi { get; set; }          // Địa chỉ giao hàng
        public string Sdt { get; set; }             // Số điện thoại khách hàng
        public string MaShipper { get; set; }       // Mã Shipper (nếu đã có)
        public string TrangThai { get; set; }       // Trạng thái đơn hàng
        public decimal TongTien { get; set; }       // Tổng tiền đơn hàng
        public DateTime ThoiGianDat { get; set; }   // Thời gian đặt đơn
        public double? NhaHangLatitude { get; set; }
        public double? NhaHangLongitude { get; set; }
        public double? KhachHangLatitude { get; set; }
        public double? KhachHangLongitude { get; set; }
    }
}
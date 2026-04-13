using System;

namespace ĐACN.Models
{
    public class OrderCardViewModel
    {
        public string MaDon { get; set; }
        public string TenNhaHang { get; set; }
        public string DiaChiNhaHang { get; set; }
        public string TenKhachHang { get; set; }
        public string DiaChiKhachHang { get; set; }
        public decimal? TongTien { get; set; }
        
        // Thêm các trường mới
        public string TrangThai { get; set; }
        public DateTime? ThoiGianDat { get; set; }
        public string SDTNhaHang { get; set; }
        public string SDTKhachHang { get; set; }
        public int SoLuongMon { get; set; }
        public string DanhSachMonTomTat { get; set; }
        public string DistanceText { get; set; }
    }
}


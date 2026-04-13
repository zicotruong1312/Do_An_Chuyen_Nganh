using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐACN.Models
{
    public class CartItem
    {
        public string MaMon { get; set; }
        public string TenMon { get; set; }
        public decimal? Gia { get; set; }
        public int SoLuong { get; set; }

        // Thông tin nhà hàng
        public string MaNH { get; set; }
        public string TenNH { get; set; }
        // Thêm hình ảnh món
        public string HinhAnh { get; set; }
        // Cho phép gán trực tiếp từ LichSuGioHang.TongTien
        public decimal ThanhTien { get; set; }
    }
}
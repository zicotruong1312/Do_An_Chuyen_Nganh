using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ĐACN.Models
{
    public class LichSuGioHang
    {
        [Key]
        public string MaGH { get; set; } // Ví dụ: "GH00001"
        public string MaKH { get; set; }
        public string MaNH { get; set; }
        public string MaMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal TongTien { get; set; }
        public DateTime ThoiGianChon { get; set; }
    }
}
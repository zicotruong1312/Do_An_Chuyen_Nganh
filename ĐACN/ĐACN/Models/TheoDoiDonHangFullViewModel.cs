using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐACN.Models
{
    public class ChiTietDonHangModel
    {
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal TongTien { get; set; }
    }

    public class TheoDoiDonHangFullViewModel
    {
        public TheoDoiDonHangViewModel DonHang { get; set; }
        public List<ChiTietDonHangModel> ChiTietDonHang { get; set; }
    }
}

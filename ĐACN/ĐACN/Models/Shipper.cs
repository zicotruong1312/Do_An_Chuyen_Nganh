
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ĐACN.Models
{

    public class ShipperMetadata
    {
        // [Display(Name = "Mã Shipper")] // Bạn có thể thêm [Display] nếu muốn
        [StringLength(20)] // Tối đa 20
        public string MaShipper { get; set; }

        // [Display(Name = "Tên Shipper")]
        [StringLength(100)] // Tối đa 100
        public string TenShipper { get; set; }

        // [Display(Name = "Số điện thoại")]
        [StringLength(20)]
        public string SDT { get; set; }

        // [Display(Name = "Biển số xe")]
        [StringLength(50)] // Tối đa 50
        public string BienSoXe { get; set; }

        // [Display(Name = "Mã tài khoản")]
        [StringLength(20)]
        public string MaTK { get; set; }

        [StringLength(255)]
        public string HinhAnh { get; set; }

        // [Display(Name = "Điểm đánh giá")]
        public decimal? DiemDanhGia { get; set; } // Dùng ? vì CSDL cho phép NULL

        // [Display(Name = "Thu nhập")]
        public decimal? ThuNhap { get; set; } // Dùng ? vì CSDL cho phép NULL
    }


    [MetadataType(typeof(ShipperMetadata))]
    public partial class Shipper
    {

    }
}
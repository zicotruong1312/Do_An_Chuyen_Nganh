namespace ĐACN.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("NhaHang")]
    public partial class NhaHang
    {
        [Key]
        [StringLength(10)]
        public string MaNH { get; set; }

        [StringLength(200)]
        public string TenNH { get; set; }

        [StringLength(300)]
        public string DiaChi { get; set; }

        [StringLength(20)]
        public string SDT { get; set; }

        [StringLength(20)]
        public string MaTK { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; }

        [StringLength(255)]
        public string HinhAnh { get; set; }

        [StringLength(300)]
        public string MoTa { get; set; }
    }
}

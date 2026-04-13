using System;
using System.Collections.Generic;

namespace ĐACN.Models
{
    public class MonAnTheoLoaiViewModel
    {
        public LoaiMonAnViewModel LoaiMonAn { get; set; }
        public IEnumerable<MonAnViewModel> MonAn { get; set; }
        public IEnumerable<NhaHangViewModel> NhaHang { get; set; }
    }
}
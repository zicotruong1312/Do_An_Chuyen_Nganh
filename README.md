# Food Delivery Application (Đồ Án Chuyên Ngành)
Project
01/2024 - 05/2024 | Fullstack Developer
• Developed a comprehensive food delivery web application using ASP.NET MVC, C# and Entity Framework.
• Designed and structured the SQL Server database to manage users, restaurants, menus, and order workflows.
• Implemented core functionalities including shopping cart, dynamic product listing, and order processing.
• Recorded technical documentation, analyzed performance, and proposed solutions for system improvement.

---

## 🚀 Hướng dẫn cài đặt và cách chạy (Setup Instructions)

1. **Clone dự án về máy**
   ```bash
   git clone https://github.com/zicotruong1312/Do_An_Chuyen_Nganh.git
   ```

2. **Cấu hình Cơ Sở Dữ Liệu (Database)**
   - Mở SQL Server Management Studio (SSMS).
   - Đảm bảo bạn đã có CSDL **`FoodDeliveryDB`** (import data hoặc khôi phục từ file backup nếu có).
   - Mở file `Web.config` trong Visual Studio.
   - Tìm `<connectionStrings>` và đổi `data source=LAPTOP-CUA-KIM\MAY1` thành tên Server SQL trên máy của bạn.

3. **Khởi chạy ứng dụng**
   - Mở file `ĐACN.sln` bằng Visual Studio (2019/2022).
   - Nhấn chuột phải vào Solution chọn **Restore NuGet Packages** để tải các thư viện cần thiết.
   - Cài đặt `ĐACN` làm Startup Project và nhấn **F5** (hoặc Run) để chạy.

# Đồ Án Chuyên Ngành - Hệ Thống Ứng Dụng Web (ASP.NET MVC)

Dự án này là một ứng dụng website được phát triển bằng framework ASP.NET MVC, phục vụ cho môn học Đồ Án Chuyên Ngành. 

## 💻 Công nghệ sử dụng
- **Backend framework**: ASP.NET MVC (.NET Framework 4.7.2)
- **Database**: Microsoft SQL Server
- **ORM**: Entity Framework 6 (Database First hoặc Model First sử dụng .edmx)
- **Frontend**: HTML, CSS, JavaScript, BootStrap (nếu có)

## 🛠️ Hướng dẫn cài đặt và chạy dự án

### 1. Yêu cầu môi trường
- Phần mềm **Visual Studio (2019 hoặc 2022)** với đã cài đặt workload: *ASP.NET and web development*.
- Cài đặt **Microsoft SQL Server** và công cụ quản lý cơ sở dữ liệu **SQL Server Management Studio (SSMS)**.

### 2. Thiết lập Cơ sở dữ liệu (Database)
Dự án sử dụng Database tên là `FoodDeliveryDB`. Bạn cần tạo mới hoặc khôi phục lại (Restore) Database theo các bước sau:
1. Mở phần mềm SQL Server Management Studio (SSMS).
2. Tạo CSDL mới tên là `FoodDeliveryDB` và tiến hành chạy lệnh SQL/Script của project để tạo các bảng dữ liệu (KhachHang, MonAn, DonHang, Shipper, TaiKhoan...).
3. Mở mã nguồn bằng Visual Studio, tìm file **`Web.config`** trong thư mục project `ĐACN`.
4. Kéo tới tag `<connectionStrings>`, lúc này bạn cần cấu hình lại tên Data Source thành tên Server SQL đang chạy trên máy của bạn (Ví dụ: `localhost`, `.\SQLEXPRESS` hoặc `Tên-Máy-Tính-Của-Bạn\MAY1`). Đổi dữ liệu này ở cả 2 dòng là `FoodDeliveryDBEntities` và `NhaHang`.

**Ví dụ:** Thay thế chuỗi gốc:
```xml
data source=LAPTOP-CUA-KIM\MAY1;initial catalog=FoodDeliveryDB;integrated security=True;...
```
Bằng chuỗi kết nối của bạn:
```xml
data source=TEN_SERVER_SQL_CUA_DANG_SU_DUNG;initial catalog=FoodDeliveryDB;integrated security=True;...
```

### 3. Chạy dự án bằng Visual Studio
1. Double-click mở tệp Solution: **`ĐACN.sln`**.
2. Toàn bộ thư viện bắt buộc (như EntityFramework, thư viện .NET) ở mục `packages` đã có sẵn. Nếu có lỗi tham chiếu (dấu chấm than vàng), bạn có thể chuột phải vào mục **References**, chọn *Manage NuGet Packages* -> click **Restore**.
3. Chọn Set as Startup Project bằng cách nhấp chuột phải vào dự án `ĐACN` trong cửa sổ *Solution Explorer*.
4. Nhấn phím nóng **F5** hoặc chọn nút khởi chạy **IIS Express** (nút màu xanh lá cây ở thanh công cụ phía trên) để bắt đầu biên dịch và chạy trang web. Dịch vụ sẽ tự động bật trình duyệt mặc định.

## 🤝 Các thông tin thêm
Ghi chú thêm về đăng nhập Admin hoặc thông tin tài khoản demo tại đây:
- **Tài khoản Admin**: 
- **Mật khẩu**: 

---
*Dự án cung cấp cho môn học Đồ Án Chuyên Ngành.*

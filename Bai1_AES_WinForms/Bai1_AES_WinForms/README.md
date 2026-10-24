# Bài 1 — Mã hóa AES-256-GCM (WinForms, .NET 8)

Ứng dụng desktop dùng C# WinForms cho bài tập môn **An toàn và Bảo mật thông tin**. Đây không phải ứng dụng web.

## Chức năng đã triển khai

1. Sinh khóa AES-256 ngẫu nhiên (32 byte).
2. Lưu khóa ra tệp JSON và import lại khóa đã lưu.
3. Mã hóa văn bản UTF-8, hiển thị bản mã dạng Base64.
4. Giải mã bản mã Base64 và xác thực dữ liệu.
5. Mã hóa tệp bất kỳ ra tệp `.aesgcm`.
6. Giải mã tệp `.aesgcm` ra tệp mới.
7. Xử lý tệp theo các khối 1 MiB; không đọc toàn bộ tệp hàng GB vào RAM.
8. Ghi thời gian, dung lượng và tốc độ mã hóa/giải mã vào nhật ký.
9. So sánh hai tệp theo từng byte, trả về offset khác biệt đầu tiên.
10. Dùng AES-GCM để phát hiện bản mã bị sửa, bị cắt cụt hoặc dùng sai khóa. Nếu giải mã thất bại, tệp đầu ra tạm chưa hoàn chỉnh sẽ bị xóa.

## Công nghệ

- C# / .NET 8
- Windows Forms
- `System.Security.Cryptography.AesGcm`
- Không cần cài thêm gói NuGet.

## Cách mở và chạy trên Windows 10/11

### Cách 1 — Visual Studio 2022

1. Cài **Visual Studio 2022** với workload **.NET desktop development** và .NET 8 SDK.
2. Giải nén thư mục dự án.
3. Mở `Bai1_AES_WinForms.csproj` bằng Visual Studio.
4. Chọn cấu hình `Debug` hoặc `Release`, sau đó bấm `F5`.

### Cách 2 — Terminal

Cài .NET 8 SDK trước, mở Terminal/PowerShell tại thư mục chứa `.csproj`, rồi chạy:

```powershell
dotnet restore
dotnet run
```

Đây là dự án Windows Forms nên cần chạy trên Windows để sử dụng giao diện.

## Hướng dẫn sử dụng

### A. Sinh và lưu khóa

1. Bấm **Sinh khóa mới**.
2. Bấm **Lưu khóa...** và lưu ví dụ thành `aes256.key.json`.
3. Khóa AES-256 có 256 bit = 32 byte. Trong tệp JSON, khóa được biểu diễn bằng Base64.
4. Đóng rồi mở lại ứng dụng, bấm **Import khóa...** để nạp đúng khóa đã lưu.

**Cảnh báo:** tệp JSON chỉ giúp lưu khóa theo định dạng dễ đọc; khóa bên trong không được mã hóa bằng mật khẩu. Không gửi tệp này cho người khác và không dùng cách lưu khóa thuần như vậy cho dữ liệu nhạy cảm thực tế.

### B. Mã hóa văn bản

1. Nhập văn bản vào ô **Văn bản gốc**.
2. Bấm **Mã hóa văn bản → Base64**.
3. Kết quả Base64 xuất hiện ở ô **Bản mã Base64**.
4. Bấm **Giải mã Base64 bên trên**. Ô kết quả cần trùng với văn bản gốc.
5. Có thể sửa một ký tự trong Base64 để kiểm tra rằng xác thực sẽ thất bại.

Mỗi lần mã hóa dùng nonce ngẫu nhiên nên bản mã Base64 có thể khác nhau ở các lần mã hóa, kể cả khi văn bản và khóa không đổi.

### C. Mã hóa và giải mã tệp

1. Chuyển sang tab **Mã hóa tệp & đo hiệu năng**.
2. Bấm **Chọn tệp và mã hóa...**, chọn tệp nguồn, rồi chọn nơi lưu `.aesgcm`.
3. Đợi thao tác hoàn thành; nhật ký sẽ ghi dung lượng gốc, dung lượng bản mã, thời gian và tốc độ MiB/s.
4. Bấm **Chọn tệp mã hóa và giải mã...**, chọn tệp `.aesgcm`, rồi chọn tên tệp đầu ra.
5. Bấm **So sánh 2 tệp theo byte...**; chọn tệp gốc trước và tệp đã giải mã sau. Kết quả mong muốn là **GIỐNG HỆT**.

Ứng dụng xử lý theo khối 1 MiB và ghi dữ liệu ra đĩa theo luồng. Mỗi khối có tag xác thực riêng và được gắn với chỉ số khối cùng phần header để phát hiện sửa đổi, đổi thứ tự hoặc cắt cụt tệp.

### Tạo tệp thử nghiệm dung lượng lớn trên Windows

Trong thư mục dự án có `TaoFileTest.ps1`, dùng để tạo tệp ngẫu nhiên theo từng khối (không đưa toàn bộ tệp vào RAM). Ví dụ tạo tệp 1 GiB:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\TaoFileTest.ps1 -OutputPath "C:\Temp\test-1GiB.bin" -SizeGiB 1
```

Tệp 2 GiB:

```powershell
.\TaoFileTest.ps1 -OutputPath "C:\Temp\test-2GiB.bin" -SizeGiB 2
```

Trước khi thử, hãy bảo đảm ổ đĩa còn đủ dung lượng cho tệp nguồn, tệp mã hóa và tệp giải mã. Ví dụ với nguồn 2 GiB, nên dự trù tối thiểu khoảng 6 GiB trống để giữ lại cả ba tệp, chưa tính các dữ liệu khác. Thời gian tạo tệp thử không tính vào thời gian mã hóa/giải mã.

Định dạng `.aesgcm` là định dạng thực hành riêng của ứng dụng này, không phải định dạng chuẩn dùng chung với các phần mềm mã hóa khác.

## Kế hoạch kiểm thử đề xuất

| Ca kiểm thử | Cách thực hiện | Kết quả mong đợi |
|---|---|---|
| T1 — Sinh khóa | Sinh khóa mới | Hiển thị AES-256, 32 byte |
| T2 — Lưu/import | Lưu khóa, khởi động lại app, import lại | Import thành công |
| T3 — Văn bản ASCII | Mã hóa rồi giải mã `Hello AES` | Văn bản khôi phục trùng nguồn |
| T4 — Unicode | Dùng `Xin chào Việt Nam 🔐` | Khôi phục đúng dấu tiếng Việt và emoji |
| T5 — Văn bản rỗng | Để trống ô nguồn, mã hóa rồi giải mã | Chuỗi rỗng được khôi phục đúng |
| T6 — Sai bản mã | Sửa Base64 rồi giải mã | Báo xác thực thất bại |
| T7 — Tệp rỗng | Tạo một tệp 0 byte, mã hóa/giải mã | Tệp khôi phục 0 byte và so sánh bằng nhau |
| T8 — Tệp nhỏ | Thử với TXT, PDF, JPG hoặc ZIP | Tệp giải mã so sánh byte giống hệt |
| T9 — Tệp lớn | Thử 100 MiB, 1 GiB, rồi vài GiB nếu còn đủ dung lượng | Hoàn tất mà không cần nạp toàn bộ tệp vào RAM |
| T10 — Sai khóa | Import một khóa khác rồi giải mã | Xác thực thất bại; không giữ tệp giải mã dở dang |
| T11 — Sửa bản mã | Sao chép tệp `.aesgcm`, sửa vài byte hoặc cắt bớt cuối tệp | Giải mã báo lỗi xác thực/định dạng |
| T12 — So sánh khác nhau | Thay đổi một byte trong bản khôi phục rồi so sánh | Báo KHÁC NHAU và offset đầu tiên |

## Bảng ghi số liệu đưa vào báo cáo

Chạy trên cùng một máy tính, dùng cùng phiên bản ứng dụng và giữ các điều kiện thử nghiệm nhất quán. Nên chạy mỗi kích thước ít nhất 3 lần và lấy thời gian trung bình.

| Kích thước tệp nguồn | Lần chạy | Thời gian mã hóa (s) | Thời gian giải mã (s) | Tốc độ mã hóa (MiB/s) | Tốc độ giải mã (MiB/s) | So sánh byte |
|---:|---:|---:|---:|---:|---:|---|
| 100 MiB | 1 |  |  |  |  |  |
| 100 MiB | 2 |  |  |  |  |  |
| 100 MiB | 3 |  |  |  |  |  |
| 1 GiB | 1 |  |  |  |  |  |
| 1 GiB | 2 |  |  |  |  |  |
| 1 GiB | 3 |  |  |  |  |  |
| 2 GiB hoặc lớn hơn | 1 |  |  |  |  |  |

Công thức tốc độ trong ứng dụng:

`Tốc độ (MiB/s) = dung lượng dữ liệu xử lý (byte) / 1,048,576 / thời gian (giây)`

Không điền số liệu giả định vào báo cáo. Hãy lấy kết quả từ nhật ký thực nghiệm của máy đang chạy ứng dụng.

## Định dạng tệp `.aesgcm` (phần có thể trình bày khi bảo vệ)

- Header chứa magic/version `AESGCM02`, kích thước khối và nonce gốc 96 bit ngẫu nhiên.
- Mỗi bản ghi chứa độ dài khối, cờ khối cuối, ciphertext và tag xác thực 16 byte.
- Nonce từng khối được tạo từ nonce gốc cộng chỉ số khối; phần header, chỉ số, độ dài và cờ được đưa vào AAD để ràng buộc dữ liệu với đúng vị trí.
- Khối cuối cùng có cờ được xác thực để phát hiện trường hợp tệp bị cắt cụt.
- Dữ liệu được ghi vào tệp tạm; chỉ chuyển sang tên đầu ra sau khi toàn bộ quá trình giải mã và xác thực hoàn thành.

Đây là định dạng minh họa phục vụ bài tập. Khi xây dựng hệ thống thực tế, nên ưu tiên định dạng/ thư viện đã được đánh giá bảo mật và quản lý nonce, khóa, lưu trữ, quyền truy cập theo mô hình đe dọa cụ thể.

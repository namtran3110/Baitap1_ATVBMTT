# Phiếu kiểm thử bài 1 — AES-256-GCM

Ghi số liệu thực tế sau khi chạy ứng dụng trên Windows.

## Kiểm thử chức năng

| ID | Dữ liệu / thao tác | Kết quả thực tế | Đạt? |
|---|---|---|---|
| AES-01 | Sinh khóa AES-256 |  |  |
| AES-02 | Lưu khóa, đóng ứng dụng, mở lại và import |  |  |
| AES-03 | Mã hóa/giải mã `Hello AES` |  |  |
| AES-04 | Mã hóa/giải mã `Xin chào Việt Nam 🔐` |  |  |
| AES-05 | Mã hóa/giải mã chuỗi rỗng |  |  |
| AES-06 | Chỉnh sửa Base64 trước khi giải mã |  |  |
| AES-07 | Tệp nguồn 0 byte |  |  |
| AES-08 | Tệp văn bản nhỏ |  |  |
| AES-09 | Tệp hình ảnh hoặc PDF |  |  |
| AES-10 | Tệp 100 MiB |  |  |
| AES-11 | Tệp 1 GiB |  |  |
| AES-12 | Tệp nhiều GiB (nếu đủ dung lượng đĩa) |  |  |
| AES-13 | Giải mã bằng một khóa khác |  |  |
| AES-14 | Cắt bớt hoặc sửa một phần tệp `.aesgcm` |  |  |
| AES-15 | So sánh nguồn và tệp giải mã theo từng byte |  |  |
| AES-16 | Thay đổi một byte rồi so sánh lại |  |  |

## Bảng đo hiệu năng

| Kích thước nguồn (byte) | Thời gian mã hóa (s) | Tốc độ mã hóa (MiB/s) | Thời gian giải mã (s) | Tốc độ giải mã (MiB/s) | Kết quả so sánh byte |
|---:|---:|---:|---:|---:|---|
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |

**Gợi ý ghi điều kiện thử nghiệm:** CPU, RAM, ổ đĩa (HDD/SSD), phiên bản Windows, phiên bản .NET, kích thước tệp, loại dữ liệu, số lần lặp, thời gian trung bình.

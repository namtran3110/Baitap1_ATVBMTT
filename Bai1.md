# MÔN HỌC: AN TOÀN VÀ BẢO MẬT THÔNG TIN
## HỌ VÀ TÊN: TRẦN NHẤT NAM
## MSSV: K235480106001
## LỚP: K59KMT

---

# BÀI 1: MÃ HÓA ĐỐI XỨNG AES-256-GCM

## 1. Mục tiêu bài tập

Xây dựng ứng dụng desktop bằng C# WinForms để tìm hiểu và thực hành thuật toán mã hóa đối xứng AES. Ứng dụng có các chức năng chính:

- Sinh khóa AES-256, lưu khóa ra tệp và nhập lại khóa từ tệp đã lưu.
- Mã hóa một đoạn văn bản và hiển thị kết quả mã hóa.
- Mã hóa tệp, giải mã tệp và lưu kết quả ra tệp riêng.
- Thử nghiệm với tệp dung lượng lớn đến hàng GiB.
- Đo thời gian mã hóa, thời gian giải mã và tốc độ xử lý.
- So sánh tệp gốc với tệp sau giải mã theo từng byte để kiểm tra dữ liệu được khôi phục có chính xác hay không.

--> Qua bài tập, phải nắm được cách sử dụng khóa AES, cách lưu và nạp lại khóa, cách mã hóa/giải mã dữ liệu, đồng thời đánh giá chương trình bằng số liệu thực nghiệm.

## 2. Công nghệ sử dụng

- **Ngôn ngữ lập trình:** C#.
- **Loại ứng dụng:** Windows Forms (WinForms), chạy trên Windows; không phải ứng dụng web.
- **Nền tảng:** .NET 8
- **Thư viện mật mã:** `System.Security.Cryptography`, sử dụng AES-GCM.
- **Xử lý tệp:** đọc và ghi dữ liệu theo từng khối 1 MiB.
- **Định dạng tệp khóa:** JSON; giá trị khóa được biểu diễn bằng Base64.

## 3. Cấu trúc thư mục dự án

```text
Bai1_AES_WinForms/
├── Bai1_AES_WinForms.sln       # Solution mở dự án trong Visual Studio
├── Bai1_AES_WinForms.csproj    # Cấu hình dự án và nền tảng .NET
├── Program.cs                  # Điểm bắt đầu của ứng dụng
├── MainForm.cs                 # Giao diện và xử lý thao tác người dùng
├── AesGcmService.cs             # Sinh khóa, mã hóa/giải mã, so sánh tệp
├── KeyFileService.cs            # Lưu và đọc khóa AES từ JSON
├── README.md                   # Hướng dẫn chạy và sử dụng chương trình
├── TEST_CASES.md               # Danh sách trường hợp kiểm thử
├── TaoFileTest.ps1              # Tạo tệp thử nghiệm dung lượng lớn
└── .gitignore                   # Danh sách tệp/thư mục Git bỏ qua
```

- `Bai1_AES_WinForms.sln`: dùng để mở solution trong Visual Studio.
- `Bai1_AES_WinForms.csproj`: khai báo cấu hình WinForms và phiên bản .NET.
- `Program.cs`: khởi động cửa sổ `MainForm` khi chạy chương trình.
- `MainForm.cs`: tạo giao diện, nhận thao tác bấm nút, gọi chức năng xử lý và hiển thị kết quả.
- `AesGcmService.cs`: thực hiện các chức năng mật mã, xử lý tệp theo khối và so sánh dữ liệu.
- `KeyFileService.cs`: lưu khóa ra tệp JSON và import khóa đã lưu.
- `README.md`: hướng dẫn sử dụng ứng dụng.
- `TEST_CASES.md`: danh sách ca kiểm thử để đối chiếu kết quả.
- `TaoFileTest.ps1`: sinh tệp ngẫu nhiên dung lượng lớn để phục vụ thí nghiệm.
- `.gitignore`: bỏ qua các tệp/thư mục như `bin`, `obj`, `.vs` khi quản lý mã nguồn bằng Git.

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/193b083c-abf5-4f82-bb66-9c8def8ea58b" />
 — Cấu trúc dự án trong Visual Studio**  
`images/01-cau-truc-du-an.png`

---

## 4. Quá trình thực hiện

### 4.1. Mở và chạy ứng dụng

- Mở `Bai1_AES_WinForms.sln` bằng Visual Studio.
- Chọn cấu hình `Debug` và nhấn `F5` để chạy ứng dụng.
- Khi cửa sổ chính xuất hiện, có thể thực hiện các chức năng sinh khóa, mã hóa văn bản và mã hóa tệp.
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/e849c722-6b64-4c91-a464-28a3fcae648a" />
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/914658b7-560a-44a7-92b5-95deab97a5e5" />
> ** — Cửa sổ ứng dụng chạy thành công**  

### 4.2. Sinh khóa AES-256

- Tại khu vực quản lý khóa, nhấn **Sinh khóa mới**.
- Chương trình sinh một khóa ngẫu nhiên bằng bộ sinh số ngẫu nhiên an toàn cho mật mã và giữ khóa trong bộ nhớ để thực hiện các thao tác tiếp theo.
- AES-256 sử dụng khóa có độ dài 256 bit. Vì 1 byte bằng 8 bit nên:

```text
256 / 8 = 32 byte
```

--> Khóa AES-256 cần có đúng **32 byte**. Vì AES là mã hóa đối xứng, cùng khóa bí mật được dùng để mã hóa và giải mã dữ liệu.

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/ce5a0cc4-7a4c-49db-884e-5e3b93b0c04c" />
 — Sinh khóa AES-256 thành công**  
`images/03-sinh-khoa.png`.

### 4.3. Lưu khóa và import khóa

- Sau khi sinh khóa, nhấn **Lưu khóa...** và lưu khóa vào tệp, ví dụ `aes256.key.json`.
- Chương trình ghi thông tin thuật toán và giá trị khóa được biểu diễn bằng Base64 vào tệp JSON.
- Đóng ứng dụng, mở lại rồi nhấn **Import khóa...** để nạp khóa đã lưu.
- Nếu import thành công, khóa đã nạp có thể được sử dụng cho chức năng mã hóa và giải mã.

--> Base64 chỉ là cách biểu diễn dữ liệu nhị phân thành chuỗi ký tự, không phải thuật toán mã hóa. Tệp khóa của phiên bản này chưa được bảo vệ bằng mật khẩu, vì vậy cần giữ tệp riêng tư và không chia sẻ công khai.

**Kết quả thực tế:** Khóa được lưu có tên là `aes256.key.json`, nằm tại mục Downloads, chương trình import khóa thành công.

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/0f2189f3-2cf1-4858-a5ee-876b5e9cfe64" />
 — Lưu khóa ra tệp JSON**  
`images/04-luu-khoa.png`
>
> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/c3363d32-8a9b-47ca-bbf2-e1a7e5034ae1" />
Y — Import khóa đã lưu thành công**  
`images/05-import-khoa.png`

### 4.4. Mã hóa và giải mã văn bản

- Chọn tab **Mã hóa văn bản**.
- Nhập văn bản cần mã hóa, ví dụ: `10 Điểm Môn thầy Cốp`.
- Nhấn **Mã hóa văn bản → Base64** để tạo bản mã.
- Nhấn **Giải mã Base64 bên trên** để khôi phục văn bản gốc.
- Kiểm tra văn bản sau giải mã, bao gồm dấu tiếng Việt, khoảng trắng và ký tự đặc biệt.

--> Chương trình sử dụng AES-GCM. Ngoài việc mã hóa, chế độ này còn kiểm tra tính xác thực của dữ liệu. Mỗi lần mã hóa sử dụng nonce mới nên kết quả Base64 có thể khác nhau ngay cả khi văn bản đầu vào và khóa không đổi.

**Kết quả thực tế:** Văn bản thử nghiệm: `10 Điểm Môn thầy Cốp`, nội dung sau khi mã hóa rồi giải mã hoàn toàn trùng khớp nhau

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/8b684576-4c61-4f22-a148-73609679f2b7" />
 — Văn bản gốc và bản mã Base64**  
`images/06-ma-hoa-van-ban.png`

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/f14e9172-ce24-4f94-8ac2-784a3fb4d862" />
 — Văn bản sau giải mã**  
`images/07-giai-ma-van-ban.png`.

### 4.5. Mã hóa và giải mã tệp nhỏ

- Tạo một tệp thử nghiệm nhỏ, ví dụ: `test-aes.txt`.
- Chuyển sang tab **Mã hóa tệp & đo hiệu năng**.
- Nhấn **Chọn tệp và mã hóa...**, chọn tệp nguồn rồi chọn nơi lưu tệp mã hóa có phần mở rộng `.aesgcm`.
- Sau khi mã hóa thành công, nhấn **Chọn tệp mã hóa và giải mã...**, chọn tệp `.aesgcm` và chọn đường dẫn lưu tệp sau giải mã.
- Không lưu đè lên tệp gốc để có thể so sánh kết quả.

--> Tệp `.aesgcm` là định dạng do ứng dụng này quy định để lưu phần thông tin cần thiết cho việc giải mã và xác thực. Đây không phải định dạng chung có thể giải mã bằng các phần mềm AES khác.

**Kết quả thực tế:** Tệp nguồn: `tes_aes.txt`; tệp mã hóa: tes_aes.txt.aesgcm; tệp giải mã: tes_aes.txt.decrypted

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/e344b634-fa3c-426b-a5df-80d4857dbbff" />
 — Mã hóa tệp nhỏ thành công**  
`images/08-ma-hoa-tep-nho.png`

> **<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/e343b47b-e62a-4e5d-9401-dc9a1d446f29" />
 — Giải mã tệp nhỏ thành công**  
`images/09-giai-ma-tep-nho.png`

### 4.6. So sánh tệp nguồn và tệp sau giải mã

- Chọn chức năng **So sánh 2 tệp theo byte...**.
- Chọn tệp nguồn ở hộp thoại thứ nhất.
- Chọn tệp sau giải mã ở hộp thoại thứ hai.
- Đọc kết quả do chương trình trả về.

--> Nếu chương trình báo hai tệp **GIỐNG HỆT**, nội dung byte của tệp sau giải mã trùng với tệp nguồn trong lần kiểm thử này. Chỉ so sánh tên tệp hoặc dung lượng là chưa đủ để chứng minh nội dung giống nhau.

**Kết quả thực tế:** Kết quả so sánh giữa 2 tệp (tệp nguồn và tệp sau giải mã) hoàn toàn giống nhau đến từng byte, dung lượng cả 2 tệp đều bằng 178 byte
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/1839d5bd-8a04-44d0-a1e2-d2d21c398c57" />
<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/6cfb342b-6c75-480f-8c88-9aee0f5676c6" />

> ** — Kết quả so sánh tệp theo byte**  
`images/10-so-sanh-byte-tep-nho.png`

### 4.7. Tạo tệp thử nghiệm dung lượng lớn

Trong thư mục dự án có tệp `TaoFileTest.ps1`. Script này tạo dữ liệu ngẫu nhiên và ghi ra tệp theo từng khối, không nạp toàn bộ tệp vào RAM. Tệp được tạo tại đường dẫn truyền vào tham số `-OutputPath`.

Ví dụ tạo tệp 1 GiB:

```powershell
Set-ExecutionPolicy -Scope Process Bypass -Force
.\TaoFileTest.ps1 -OutputPath "C:\Temp\test-1GiB.bin" -SizeGiB 1
```

Ví dụ tạo tệp 2 GiB:

```powershell
.\TaoFileTest.ps1 -OutputPath "C:\Temp\test-2GiB.bin" -SizeGiB 2
```

- Tệp thử nghiệm 1 GiB sẽ nằm tại `C:\Temp\test-1GiB.bin` nếu chạy đúng lệnh thứ nhất.
- Tệp thử nghiệm 2 GiB sẽ nằm tại `C:\Temp\test-2GiB.bin` nếu chạy đúng lệnh thứ hai.
- Nếu thay đổi `-OutputPath`, tệp sẽ được tạo tại vị trí đã chỉ định.
- `1 GiB = 1.073.741.824 byte`; `2 GiB = 2.147.483.648 byte`.
- Cần kiểm tra ổ đĩa còn đủ chỗ cho tệp gốc, tệp mã hóa và tệp khôi phục.

**Kết quả thực tế:** [Ghi đường dẫn đã dùng, dung lượng tệp và trạng thái chạy script.]

> **📸 CHÈN ẢNH 11 TẠI ĐÂY — Tạo tệp lớn bằng PowerShell**  
> Gợi ý tên ảnh: `images/11-tao-tep-lon.png`. Chụp lệnh đã chạy và thông báo hoàn tất.
>
> **📸 CHÈN ẢNH 12 TẠI ĐÂY — Tệp lớn trong File Explorer**  
> Gợi ý tên ảnh: `images/12-dung-luong-tep-lon.png`. Chụp tên tệp, đường dẫn và dung lượng.

### 4.8. Mã hóa, giải mã và đo hiệu năng tệp lớn

- Chọn tệp 1 GiB hoặc 2 GiB vừa tạo, sau đó mã hóa và lưu bản mã ra một tệp riêng.
- Ghi nhận thời gian mã hóa, dung lượng đầu vào/đầu ra và tốc độ xử lý trong nhật ký của ứng dụng.
- Tiếp tục giải mã bản mã ra tệp khôi phục, ghi lại thời gian giải mã và tốc độ xử lý.
- So sánh tệp gốc với tệp khôi phục theo byte.
- Nên lặp lại phép thử vài lần trong điều kiện tương tự để kết quả ít bị ảnh hưởng bởi biến động nhất thời.

--> Chương trình xử lý tệp theo các khối 1 MiB. Cách này hạn chế lượng RAM cần dùng vì không phải đọc toàn bộ tệp hàng GiB vào bộ nhớ cùng một lúc.

**Điều kiện thử nghiệm:**

- CPU: [Điền thông tin nếu biết].
- RAM: [Điền dung lượng].
- Ổ đĩa chứa tệp: [SSD/HDD và thông tin nếu biết].
- Hệ điều hành: [Điền phiên bản Windows].
- Tệp thử nghiệm: [Tên và dung lượng thực tế].
- Số lần chạy: [Điền số lần].

**Kết quả thực tế:** [Điền thời gian và tốc độ lấy từ nhật ký ứng dụng. Không tự điền số liệu khi chưa chạy thử.]

> **📸 CHÈN ẢNH 13 TẠI ĐÂY — Nhật ký mã hóa tệp lớn**  
> Gợi ý tên ảnh: `images/13-ma-hoa-tep-lon.png`. Đảm bảo ảnh thấy được dung lượng, thời gian và tốc độ.
>
> **📸 CHÈN ẢNH 14 TẠI ĐÂY — Nhật ký giải mã tệp lớn**  
> Gợi ý tên ảnh: `images/14-giai-ma-tep-lon.png`.
>
> **📸 CHÈN ẢNH 15 TẠI ĐÂY — So sánh byte của tệp lớn**  
> Gợi ý tên ảnh: `images/15-so-sanh-byte-tep-lon.png`.

---

## 5. Bảng tổng hợp kiểm thử chức năng

Điền kết quả sau khi chạy từng trường hợp. Không đánh dấu “Đạt” nếu chưa thực hiện kiểm thử.

| STT | Nội dung kiểm thử | Kết quả mong đợi | Kết quả thực tế | Đánh giá |
|---:|---|---|---|---|
| 1 | Sinh khóa AES-256 | Sinh được khóa 32 byte | [Điền] | [Đạt/Không đạt/Chưa thử] |
| 2 | Lưu khóa và import lại | Nạp được khóa đã lưu | [Điền] | [Điền] |
| 3 | Mã hóa/giải mã văn bản tiếng Việt | Văn bản khôi phục giống nguồn | [Điền] | [Điền] |
| 4 | Mã hóa/giải mã chuỗi rỗng | Khôi phục đúng chuỗi rỗng | [Điền] | [Điền] |
| 5 | Sửa bản mã hoặc dùng sai khóa | Xác thực thất bại hoặc báo lỗi phù hợp | [Điền] | [Điền] |
| 6 | Mã hóa/giải mã tệp nhỏ | Tệp khôi phục giống nguồn theo byte | [Điền] | [Điền] |
| 7 | Mã hóa/giải mã tệp 1 GiB | Hoàn tất nếu đủ tài nguyên máy | [Điền] | [Điền] |
| 8 | Mã hóa/giải mã tệp 2 GiB | Hoàn tất nếu đủ tài nguyên máy | [Điền] | [Điền] |
| 9 | So sánh tệp sau giải mã | Hai tệp giống hệt theo byte | [Điền] | [Điền] |

## 6. Bảng số liệu thực nghiệm

Ghi lại số liệu thực tế từ ứng dụng. Nếu có thể, thực hiện 3 lần với mỗi kích thước tệp và giữ điều kiện thử nghiệm tương tự nhau.

| Dung lượng nguồn | Lần thử | Thời gian mã hóa (s) | Tốc độ mã hóa (MiB/s) | Thời gian giải mã (s) | Tốc độ giải mã (MiB/s) | Kết quả so sánh byte |
|---|---:|---:|---:|---:|---:|---|
| 1 GiB | 1 | [Điền] | [Điền] | [Điền] | [Điền] | [Điền] |
| 1 GiB | 2 | [Điền] | [Điền] | [Điền] | [Điền] | [Điền] |
| 1 GiB | 3 | [Điền] | [Điền] | [Điền] | [Điền] | [Điền] |
| 2 GiB | 1 | [Điền] | [Điền] | [Điền] | [Điền] | [Điền] |
| 2 GiB | 2 | [Điền] | [Điền] | [Điền] | [Điền] | [Điền] |
| 2 GiB | 3 | [Điền] | [Điền] | [Điền] | [Điền] | [Điền] |

Công thức tham khảo:

```text
Tốc độ (MiB/s) = Dung lượng xử lý (byte) / 1.048.576 / Thời gian (giây)
Thời gian trung bình = (Lần 1 + Lần 2 + Lần 3) / 3
```

## 7. Nhận xét và kết luận

### 7.1. Nhận xét về chức năng

[Điền các chức năng đã chạy thành công, chức năng gặp lỗi và cách khắc phục. Nêu rõ kết quả so sánh tệp nguồn với tệp sau giải mã.]

### 7.2. Nhận xét về hiệu năng

[Dựa vào bảng số liệu để so sánh thời gian mã hóa và giải mã giữa các kích thước tệp. Nêu tốc độ xử lý quan sát được trên máy tính đã dùng. Không khái quát kết quả thành tốc độ của AES trên tất cả máy tính.]

### 7.3. Kết luận

[Đưa ra kết luận dựa trên kết quả kiểm thử thực tế: chương trình đáp ứng được những yêu cầu nào, dữ liệu sau giải mã có khớp theo byte hay không, quá trình xử lý tệp lớn có hoàn tất hay không và hạn chế còn lại.]

## 8. Hạn chế và lưu ý bảo mật

- Tệp khóa JSON trong phiên bản bài tập chưa được mã hóa bằng mật khẩu. Cần giữ tệp khóa riêng tư.
- Tệp `.aesgcm` sử dụng định dạng riêng của ứng dụng, không mặc nhiên tương thích với phần mềm AES khác.
- Kết quả đo tốc độ phụ thuộc vào cấu hình máy tính, ổ đĩa, kích thước tệp và điều kiện thử nghiệm.
- Nếu mã hóa/giải mã tệp lớn gặp lỗi, cần ghi lại thông báo lỗi, dung lượng tệp và dung lượng trống của ổ đĩa.
- Sau khi lưu đủ ảnh minh chứng và số liệu, có thể xóa các tệp thử nghiệm dung lượng lớn nếu không còn sử dụng. Cần kiểm tra đúng đường dẫn trước khi xóa.

---

## 9. Danh sách ảnh minh chứng

Tạo thư mục `images` cùng cấp với file báo cáo này. Các tên ảnh gợi ý tương ứng với những vị trí được đánh dấu trong báo cáo:

1. `01-cau-truc-du-an.png` — Cấu trúc dự án trong Visual Studio.
2. `02-cua-so-ung-dung.png` — Cửa sổ ứng dụng sau khi chạy.
3. `03-sinh-khoa.png` — Sinh khóa AES-256.
4. `04-luu-khoa.png` — Lưu khóa ra JSON.
5. `05-import-khoa.png` — Import khóa đã lưu.
6. `06-ma-hoa-van-ban.png` — Mã hóa văn bản.
7. `07-giai-ma-van-ban.png` — Giải mã văn bản.
8. `08-ma-hoa-tep-nho.png` — Mã hóa tệp nhỏ.
9. `09-giai-ma-tep-nho.png` — Giải mã tệp nhỏ.
10. `10-so-sanh-byte-tep-nho.png` — So sánh tệp nhỏ theo byte.
11. `11-tao-tep-lon.png` — Tạo tệp lớn bằng PowerShell.
12. `12-dung-luong-tep-lon.png` — Dung lượng tệp lớn trong File Explorer.
13. `13-ma-hoa-tep-lon.png` — Nhật ký mã hóa tệp lớn.
14. `14-giai-ma-tep-lon.png` — Nhật ký giải mã tệp lớn.
15. `15-so-sanh-byte-tep-lon.png` — So sánh tệp lớn theo byte.

**Cách chèn ảnh:** Sau khi chụp và lưu ảnh vào thư mục `images`, thay dòng đánh dấu **📸 CHÈN ẢNH...** tương ứng bằng cú pháp, ví dụ:

```markdown
![Ảnh 1: Cấu trúc dự án trong Visual Studio](images/01-cau-truc-du-an.png)
```

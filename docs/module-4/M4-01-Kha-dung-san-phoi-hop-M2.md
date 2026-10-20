# M4-01 — Hợp đồng kiểm tra sân khả dụng cho M2

Phụ trách M4: Phạm Thành Nghĩa — MSSV 23103100308.
Nội dung: phần M4 của phối hợp M2/M4; hợp đồng tra cứu, quy tắc dùng chung, cách tích hợp và kiểm thử.
Cập nhật: 20/10/2026.

- M4: [issue #21](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/21).
- M2: [issue #15](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/15).

## 1. Phần M4 đã triển khai

- Bổ sung `IDatSanService.KiemTraKhaDungAsync` cho M2 gọi từ service tìm sân.
- Kiểm tra khoảng giờ, trạng thái sân/loại sân, giờ mở cửa, ngày bảo trì và giao lịch.
- Dùng chung truy vấn và quy tắc đánh giá với `TaoDonAsync` và `XacNhanDonAsync`.
- Mỗi lô tối đa 200 mã sân dùng một lệnh SQL, gồm kiểm tra tồn tại lịch giao nhau bằng EXISTS; không tải danh sách đơn hay thông tin khách.
- Tạo/xác nhận vẫn kiểm tra lại trong transaction sau khi khóa sân; xác nhận bỏ qua chính đơn đang xử lý.
- Đọc dữ liệu sân mới từ database, kể cả khi DbContext đang theo dõi một bản sân cũ.
- Giữ các mã lỗi tạo/xác nhận hiện có để controller và ViewModel tiếp tục hiển thị đúng.
- Bổ sung kiểm thử SQL Server cho hợp đồng và chuỗi migration hiện hành. Tính năng này không cần migration mới.

## 2. Hợp đồng gọi

Namespace: `QuanLyDatSan_UNETI5_DHTI17A4HN.Services`.
`IDatSanService` đã được đăng ký scoped trong `Extensions/DependencyInjection.cs`.

```csharp
Task<KetQuaKhaDungSan> KiemTraKhaDungAsync(
    IReadOnlyCollection<int> maSans,
    KhoangGioTraCuuSan khoangGio,
    CancellationToken cancellationToken);
```

### Đầu vào

- `maSans`: tối đa 200 phần tử, tất cả mã dương. Mã lặp chỉ trả một lần, theo thứ tự xuất hiện đầu tiên.
- Danh sách rỗng: trả danh sách rỗng, không đọc SQL.
- `NgaySuDung`, `GioBatDau`, `GioKetThuc`: ngày/giờ địa phương của ứng dụng, cùng quy ước `IDongHo`; không đổi sang UTC riêng ở M2.
- Chuẩn hóa giờ tới phút bằng cách bỏ giây, giống luồng tạo đơn.
- Bắt đầu phải lớn hơn giờ hiện tại; kết thúc lớn hơn bắt đầu trong cùng ngày. Chưa hỗ trợ qua đêm.
- Null, mã không dương hoặc quá 200 phần tử là lỗi cách gọi (`ArgumentException`/`ArgumentNullException`). M2 cần validate và chia lô trước.
- Truyền cancellation token từ request. Không gọi song song nhiều lô trên cùng scoped DbContext.

### Đầu ra

- `BatDau`, `KetThuc`: thời điểm đã chuẩn hóa.
- `Loi` ở cấp kết quả: lỗi thời gian của toàn yêu cầu; khi có lỗi, `DanhSach` rỗng. M2 phải xử lý lỗi này trước khi lọc sân.
- `DanhSach`: mỗi mã có `MaSan`, `KhaDung`, `Loi`, `GioMoCua`, `GioDongCua`.
- Mã sân không tồn tại cũng có kết quả với lỗi `SanKhongTonTai`; giờ mở/đóng là null.
- Không có lỗi cấp yêu cầu không đồng nghĩa tất cả sân đều khả dụng; cần đọc `KhaDung` từng dòng.

| Mã lỗi | Điều kiện / cách M2 xử lý |
| --- | --- |
| BatDauKhongTuongLai | Báo người dùng chọn giờ bắt đầu trong tương lai. |
| KhoangGioKhongHopLe | Báo giờ kết thúc phải sau bắt đầu trong cùng ngày. |
| SanKhongTonTai | Loại sân không còn tồn tại khỏi kết quả. |
| SanKhongNhanDat | Sân hoặc loại sân không hoạt động; loại khỏi kết quả. |
| NgoaiGioHoatDong | Khoảng đặt ngoài giờ mở/đóng; có thể hiển thị giờ hoạt động. |
| SanBaoTri | Ngày sử dụng trùng ngày bảo trì; loại khỏi kết quả. |
| TrungLich | Có đơn chiếm chỗ giao khoảng giờ; loại khỏi kết quả. |

Mỗi sân trả lỗi đầu tiên theo thứ tự: không tồn tại → ngừng nhận đặt → ngoài giờ → ngày bảo trì → trùng lịch.

## 3. Quy tắc chiếm chỗ

```text
batDauMoi < ketThucCu && ketThucMoi > batDauCu
```

- Hai lịch liền kề được phép: 10:00–11:00 và 11:00–12:00.
- `ChoXuLy`, `DangXuLy`, `HoanThanh` được tính khi kiểm tra giao lịch.
- `DaHuy`, `TuChoi` không chiếm chỗ.
- Bắt đầu đúng giờ mở và kết thúc đúng giờ đóng được phép.
- Trạng thái sân `BaoTri` chặn nhận đặt; `NgayBaoTri` chặn riêng ngày tương ứng ngay cả khi sân đang `HoatDong`.
- Kết quả tra cứu không giữ chỗ, không đảm bảo sân vẫn trống khi người dùng gửi form.
- M3 phải tiếp tục gọi `TaoDonAsync`; không chèn đơn trực tiếp dựa trên kết quả M2.

## 4. Công việc M2 cần làm tiếp

1. Inject `IDatSanService` vào service tìm sân và nhận ngày/giờ từ ViewModel đã validate.
2. Lấy các mã ứng viên theo tên, loại sân và các tiêu chí tìm kiếm của M2.
3. Gọi kiểm tra khả dụng theo lô tối đa 200 mã, tuần tự; gom các mã `KhaDung == true`.
4. Lọc tập ứng viên bằng các mã khả dụng, rồi mới tính tổng, sắp xếp ổn định và phân trang.
5. Hiển thị lỗi thời gian ở form, phân biệt với trường hợp không có sân phù hợp.
6. Giữ ngày/giờ khi đổi bộ lọc, chuyển trang và mở form tạo đơn M3.
7. Test tích hợp UI tìm sân → tạo đơn, gồm trường hợp người khác vừa đặt trước khi gửi.

Không phân trang ứng viên trước khi kiểm tra khả dụng: việc đó làm thiếu kết quả và sai tổng số trang.
Ví dụ phần gọi trong service M2 (các biến đầu vào do M2 chuẩn bị):

```csharp
var maKhaDung = new HashSet<int>();
foreach (var lo in maSanUngVien.Distinct().Chunk(200))
{
    var ketQua = await datSanService.KiemTraKhaDungAsync(
        lo, new KhoangGioTraCuuSan(ngaySuDung, gioBatDau, gioKetThuc), cancellationToken);
    if (ketQua.Loi is { } loi)
    {
        // Ánh xạ loi vào kết quả lỗi tìm kiếm của M2 và dừng xử lý.
        // Không trả danh sách thành công rỗng như thể không có sân.
        return KetQuaTimKiem.ThatBai(loi); // Tên minh họa: M2 dùng DTO kết quả của mình.
    }
    maKhaDung.UnionWith(ketQua.DanhSach.Where(x => x.KhaDung).Select(x => x.MaSan));
}
// M2 tiếp tục lọc, tính tổng, sắp xếp và phân trang bằng maKhaDung.
```

Khi không có ứng viên, M2 vẫn validate ngày/giờ hoặc gọi service với danh sách rỗng để nhận lỗi thời gian.
Với số lượng sân rất lớn, M2 cần tránh nạp toàn bộ thông tin sân trước khi lọc; hợp đồng hiện tại giới hạn mỗi lô, không giới hạn tổng số sân tìm được.

## 5. File liên quan

- `Services/DatSan/HopDongKhaDungSan.cs`: request, response và mã lỗi tra cứu.
- `Services/DatSan/DatSanService.KhaDung.cs`: xử lý theo lô, quy tắc và ánh xạ lỗi.
- `Services/DatSan/IDatSanService.cs`: API dùng chung.
- `Services/DatSanService.cs`: tạo đơn dùng quy tắc chung sau khóa.
- `Services/DatSan/DatSanService.XuLy.cs`: xác nhận dùng quy tắc chung, bỏ qua chính đơn.
- `tests/QuanLyDatSan.M4.Tests/KhaDungSanTests.cs`: ca kiểm thử khả dụng và tính nhất quán.
- `tests/QuanLyDatSan.M4.Tests/MigrationDatSanTests.cs`: áp dụng migrations thực trên DB mới.

## 6. Kiểm thử và giới hạn

Kết quả chạy ngày 20/10/2026 trên SQL LocalDB riêng `M4Issue21_20261020_a7c2`:

- .NET M4: **141 passed, 0 failed, 0 skipped** (114 ca hiện có + 27 ca bổ sung).
- JavaScript xử lý đơn: **4 passed, 0 failed**. Lần chạy đầu bị sandbox chặn tạo tiến trình (`spawn EPERM`); chạy lại với quyền phù hợp đạt đủ 4 ca.
- Migration: áp dụng đủ chuỗi migration vào database rỗng; đọc được model và gọi được hợp đồng khả dụng.
- Kiểm tra theo lô 200 đầu vào: đúng **1 lệnh SQL**, loại mã lặp và không track entity.
- Hồi quy đạt các ca đặt/xác nhận đồng thời, RowVersion, rollback, quyền HTTP và dữ liệu thay đổi sau khóa.
- Build project kiểm thử M4 và ứng dụng thành công: **0 lỗi**. Có 4 cảnh báo NU1902/NU1903 từ dependency `System.Security.Cryptography.Pkcs 9.0.4` hiện có; lượt này không đổi package.
- `git diff --check` đạt.

Các ca mới đối chiếu khả dụng với tạo/xác nhận cho sân ngừng hoạt động, loại sân ngừng hoạt động,
trạng thái/ngày bảo trì, giờ mở/đóng; kiểm tra toàn bộ trạng thái đơn, lịch liền kề/giao nhau,
chuẩn hóa phút, khoảng giờ sai, mã không tồn tại, null/giới hạn lô, cancellation và dữ liệu đã track cũ.
Có ca chứng minh tra cứu thấy sân trống vẫn bị từ chối tạo đơn nếu khách khác đặt trước đó.

```powershell
$env:M4_TEST_SQL_SERVER = '(localdb)\<instance-test>'
dotnet test tests/QuanLyDatSan.M4.Tests/QuanLyDatSan.M4.Tests.csproj --no-restore --verbosity minimal
node --test tests/QuanLyDatSan.M4.Tests/quan-ly-don-dat.test.cjs
git diff --check
```

Test dùng database ngẫu nhiên `QuanLyDatSan_M4Test_<guid>` và `QuanLyDatSan_M4MigrationTest_<guid>` trên instance chỉ định, dọn đúng database đã tạo.
Test migration kiểm tra chuỗi hiện có từ database rỗng, không kiểm chứng tự động mọi database cũ đã bị sửa lịch sử migrations.
Việc tích hợp giao diện tìm sân của M2 và kiểm thử luồng người dùng xuyên M2/M3 cần thực hiện sau khi M2 sử dụng hợp đồng này.
Chưa đóng issue #21 hoặc cập nhật GitHub trong lượt triển khai này.

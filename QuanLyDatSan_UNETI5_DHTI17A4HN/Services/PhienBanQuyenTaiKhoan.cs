// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ thu hồi phiên khi đổi quyền/trạng thái.
using System.Collections.Concurrent;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

// Cùng phạm vi một tiến trình với Session hiện có. Không thay thế kiểm tra database.
// Mỗi lần đổi quyền/trạng thái tạo phiên bản mới: khóa rồi mở vẫn thu hồi cookie cũ.
public sealed class PhienBanQuyenTaiKhoan
{
    private readonly ConcurrentDictionary<int, string> phienBan = new();

    public string Lay(int maTaiKhoan) => phienBan.GetOrAdd(maTaiKhoan, _ => Guid.NewGuid().ToString("N"));

    public void VoHieuHoa(int maTaiKhoan) => phienBan[maTaiKhoan] = Guid.NewGuid().ToString("N");
}

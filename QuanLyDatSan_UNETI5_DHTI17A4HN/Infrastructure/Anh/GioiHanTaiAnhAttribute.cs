// M1: Trần Trọng Tùng - 23103100202. Đánh dấu endpoint upload để M1/M2 dùng chung giới hạn request.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;

[AttributeUsage(AttributeTargets.Method)]
public sealed class GioiHanTaiAnhAttribute : Attribute;

// M4: hồi quy Controller dùng chung với M3; HTTP/cookie/antiforgery và SQL Server thật.
using System.Data.Common;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class DonDatHttpTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    [Theory]
    [InlineData(null)]
    [InlineData("admin")]
    [InlineData("staff")]
    public async Task QuyenControllerHienTaiDuocGiuChoGetVaPost(string? user)
    {
        using var host = new App(f); using var client = host.Client();
        if (user is not null) await Login(client, user);
        foreach (var path in new[] { "/DonDat/Index", "/DonDat/ChiTiet/1", "/DonDat/Tao" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(user is null ? "DangNhap" : "TuChoiTruyCap", response.Headers.Location!.OriginalString);
        }
        var post = await client.PostAsync("/DonDat/Tao", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
    }

    [Fact]
    public async Task ChiTietVaDanhSachChiTraDonCuaKhachHienTai()
    {
        var order = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "other");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/DonDat/ChiTiet/{order.MaDatSan}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/DonDat/ChiTiet/2147483647")).StatusCode);
        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDatSanService>();
        var list = await service.LayDanhSachCaNhanAsync(f.OtherCustomerAccountId, null, 1, default);
        Assert.Empty(list.Dong);
        Assert.Null(await service.LayChiTietCaNhanAsync(f.OtherCustomerAccountId, order.MaDatSan, default));
    }

    [Fact]
    public async Task FormLoiNapLaiLuaChonVaTaoDonGiuRedirectGiaServer()
    {
        var seed = await f.Booking(TrangThaiDatSan.DaHuy);
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync("/DonDat/Tao");
        Assert.Contains($"value=\"{seed.MaSan}\"", html);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync("/DonDat/Tao", new FormUrlEncodedContent([]))).StatusCode);
        var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(html),
            ["MaSan"] = seed.MaSan.ToString(), ["NgaySuDung"] = "2030-01-02", ["GioBatDau"] = "10:00" };
        var invalid = await client.PostAsync("/DonDat/Tao", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var invalidHtml = WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync());
        Assert.Contains("Vui lòng chọn giờ kết thúc.", invalidHtml);
        Assert.Contains($"value=\"{seed.MaSan}\"", invalidHtml);
        Assert.Contains("2030-01-02", invalidHtml);

        form["GioKetThuc"] = "11:00";
        form["DonGia"] = "1"; form["TienCoc"] = "999999"; form["TrangThai"] = "2";
        var created = await client.PostAsync("/DonDat/Tao", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.StartsWith("/DonDat/ChiTiet/", created.Headers.Location!.OriginalString);
        var id = int.Parse(created.Headers.Location.OriginalString.Split('/').Last());
        var saved = await f.Read(id);
        Assert.Equal(seed.DonGia, saved.DonGia);
        Assert.Equal(0m, saved.TienCoc);
        Assert.Equal(TrangThaiDatSan.ChoXuLy, saved.TrangThai);
        Assert.Contains("Đặt sân thành công", WebUtility.HtmlDecode(await client.GetStringAsync(created.Headers.Location)));
        var duplicate = await client.PostAsync("/DonDat/Tao", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("Khung giờ này đã có người đặt", WebUtility.HtmlDecode(await duplicate.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task PhanTrangLocVaDtoDaMaterialize()
    {
        for (var i=0;i<12;i++) await f.Booking(TrangThaiDatSan.TuChoi);
        using var host = new App(f);
        using var scope = host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDatSanService>();
        var first = await service.LayDanhSachCaNhanAsync(f.CustomerAccountId, TrangThaiDatSan.TuChoi, -1, default);
        var last = await service.LayDanhSachCaNhanAsync(f.CustomerAccountId, TrangThaiDatSan.TuChoi, int.MaxValue, default);
        Assert.Equal(1, first.Trang); Assert.Equal(10, first.Dong.Count);
        Assert.Equal(2, first.TongTrang); Assert.Equal(2, last.Trang); Assert.Equal(2, last.Dong.Count);
        Assert.All(first.Dong, x => Assert.Equal(TrangThaiDatSan.TuChoi,x.TrangThai));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ChangeTracker.Entries());
        var invalid = await service.LayDanhSachCaNhanAsync(f.CustomerAccountId, (TrangThaiDatSan)99, 1, default);
        Assert.Null(invalid.TrangThai);
    }

    [Fact]
    public async Task LoiNgoaiDuKienTra500VaLogTrongProduction()
    {
        using var host = new App(f, production:true); using var client = host.Client();
        await Login(client,"customer");
        host.Fault.Enabled = true;
        var response = await client.GetAsync("/DonDat/Index");
        Assert.Equal(HttpStatusCode.InternalServerError,response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("M4_TEST_INTERNAL",html);
        Assert.DoesNotContain("InvalidOperationException",html);
        Assert.Contains("Request",html,StringComparison.OrdinalIgnoreCase);
        Assert.True(host.Log.SawException);
    }

    private async Task Login(HttpClient client,string user)
    {
        var token = Token(await client.GetStringAsync("/TaiKhoan/DangNhap"));
        var response = await client.PostAsync("/TaiKhoan/DangNhap",new FormUrlEncodedContent(new Dictionary<string,string>
        { ["TenDangNhap"]=user,["MatKhau"]=f.Password,["__RequestVerificationToken"]=token }));
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
    }
    private static string Token(string html)
    {
        var match=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success); return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private sealed class App(SqlFixture fixture,bool production=false) : WebApplicationFactory<DonDatController>
    {
        public FaultInterceptor Fault { get; } = new();
        public TestLog Log { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(production ? "Production" : "Development");
            builder.UseContentRoot(Path.GetFullPath("../../../../../QuanLyDatSan_UNETI5_DHTI17A4HN",AppContext.BaseDirectory));
            builder.ConfigureLogging(logging=>logging.ClearProviders().AddProvider(Log));
            builder.ConfigureServices(services=>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options=>options.UseSqlServer(fixture.ConnectionString).AddInterceptors(Fault));
                services.RemoveAll<IDongHo>(); services.AddSingleton<IDongHo>(fixture.Clock);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }
        public HttpClient Client()=>CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false,BaseAddress=new Uri("https://localhost") });
    }

    private sealed class FaultInterceptor : DbCommandInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            if(Enabled && command.CommandText.Contains("FROM [DatSan]"))
                throw new InvalidOperationException("M4_TEST_INTERNAL: unexpected database failure");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class TestLog : ILoggerProvider,ILogger
    {
        public bool SawException { get; private set; }
        public ILogger CreateLogger(string categoryName)=>this;
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(LogLevel level)=>true;
        public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        { if(exception is not null) SawException=true; }
        public void Dispose() { }
    }
}

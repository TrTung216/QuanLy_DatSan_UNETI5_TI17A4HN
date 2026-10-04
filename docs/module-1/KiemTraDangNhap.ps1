# M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử HTTP + SQL Server.
# Tạo database LocalDB riêng, mật khẩu ngẫu nhiên; xóa database và dừng tiến trình thử khi kết thúc.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
Add-Type -AssemblyName System.Net.Http
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project = Join-Path $repo 'QuanLyDatSan_UNETI5_DHTI17A4HN'
$dll = Join-Path $project 'bin/Debug/net10.0/QuanLyDatSan_UNETI5_DHTI17A4HN.dll'
if (!(Test-Path $dll)) { throw 'Run dotnet build first.' }
$dbName = 'QuanLyDatSan_M1_LoginTest_' + [Guid]::NewGuid().ToString('N')
$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=$dbName;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5"
$testPassword = [Guid]::NewGuid().ToString('N') + '!aA1'
$names = @('ConnectionStrings__DefaultConnection','AdminKhoiTao__TenDangNhap','AdminKhoiTao__HoTen','AdminKhoiTao__Email','AdminKhoiTao__MatKhau','ASPNETCORE_ENVIRONMENT','Logging__LogLevel__Default')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
$server = $null
$sql = $null
$client = $null
$handler = $null
$log = Join-Path $project 'obj/login-test.log'
$errLog = Join-Path $project 'obj/login-test-error.log'

function Assert($condition, [string]$name) {
    if (!$condition) { throw "FAIL: $name" }
    Write-Output "PASS: $name"
}
function Execute-Sql([string]$query) {
    $command = $sql.CreateCommand()
    $command.CommandText = $query
    try { return $command.ExecuteScalar() }
    finally { $command.Dispose() }
}
function Request([string]$method, [string]$path, [hashtable]$data = @{}) {
    if ($method -eq 'POST') {
        $values = [Collections.Generic.Dictionary[string,string]]::new()
        foreach ($key in $data.Keys) { $values.Add($key, [string]$data[$key]) }
        $form = [Net.Http.FormUrlEncodedContent]::new($values)
        try { $response = $client.PostAsync("$baseUrl$path", $form).GetAwaiter().GetResult() }
        finally { $form.Dispose() }
    } else { $response = $client.GetAsync("$baseUrl$path").GetAwaiter().GetResult() }
    try {
        return @{
            Status = [int]$response.StatusCode
            Body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Location = [string]$response.Headers.Location
        }
    } finally { $response.Dispose() }
}
function Token([string]$path = '/TaiKhoan/DangNhap') {
    $page = Request 'GET' $path
    if ($page.Status -ne 200) { throw "Cannot load token: $($page.Status)" }
    $match = [regex]::Match($page.Body, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (!$match.Success) { throw 'Missing antiforgery token.' }
    return [Net.WebUtility]::HtmlDecode($match.Groups[1].Value)
}
function Login([string]$username = 'admin_test', [string]$password = $testPassword, [string]$returnUrl = '') {
    return Request 'POST' '/TaiKhoan/DangNhap' @{
        TenDangNhap=$username; MatKhau=$password; ReturnUrl=$returnUrl; __RequestVerificationToken=(Token)
    }
}
try {
    $env:ConnectionStrings__DefaultConnection = $connectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Logging__LogLevel__Default = 'Warning'
    $env:AdminKhoiTao__TenDangNhap = 'admin_test'
    $env:AdminKhoiTao__HoTen = 'Admin Test'
    $env:AdminKhoiTao__Email = 'admin@example.test'
    $env:AdminKhoiTao__MatKhau = $testPassword
    dotnet ef database update --project $project --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }
    dotnet $dll --tao-admin
    Assert ($LASTEXITCODE -eq 0) 'Bootstrap first Admin'
    # A second invocation must neither overwrite credentials nor create another Admin.
    $process = Start-Process dotnet -ArgumentList @($dll, '--tao-admin') -WorkingDirectory $project -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput $log -RedirectStandardError $errLog
    Assert ($process.ExitCode -eq 1) 'Bootstrap refuses existing Admin'
    $sql = [Data.SqlClient.SqlConnection]::new($connectionString)
    $sql.Open()
    $hash = Execute-Sql "SELECT MatKhau FROM TaiKhoan WHERE TenDangNhap='admin_test'"
    Assert ($hash -ne $testPassword -and $hash.Length -gt 60) 'Password stored as hash'
    $env:AdminKhoiTao__MatKhau = $null

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port"
    $server = Start-Process dotnet -ArgumentList @($dll, '--urls', $baseUrl) -WorkingDirectory $project -WindowStyle Hidden -PassThru -RedirectStandardOutput $log -RedirectStandardError $errLog
    $handler = [Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(10)
    $ready = $false
    for ($i=0; $i -lt 40; $i++) {
        try { $ready = (Request 'GET' '/TaiKhoan/DangNhap').Status -eq 200 } catch {}
        if ($ready) { break }
        Start-Sleep -Milliseconds 250
    }
    Assert $ready 'Login page available'
    $result = Request 'GET' '/TaiKhoan/ThongTin'
    Assert ($result.Status -eq 302 -and $result.Location -like '*/TaiKhoan/DangNhap*') 'Anonymous cannot access current account'
    $result = Request 'POST' '/TaiKhoan/DangNhap' @{ TenDangNhap='admin_test'; MatKhau=$testPassword }
    Assert ($result.Status -eq 400) 'Login rejects missing antiforgery token'
    $result = Login '' ''
    Assert ($result.Status -eq 200 -and $result.Body -like '*field-validation-error*') 'Server validates empty fields'
    $wrong = Login 'admin_test' 'wrong-password'
    $unknown = Login 'unknown_test' 'wrong-password'
    Assert ($wrong.Status -eq 200 -and $unknown.Status -eq 200 -and $wrong.Body -like '*validation-summary-errors*' -and $unknown.Body -like '*validation-summary-errors*') 'Wrong password and unknown account rejected'
    Assert (!$wrong.Body.Contains('value="wrong-password"')) 'Password not echoed in HTML'
    $result = Login '  ADMIN_TEST  ' $testPassword '/TaiKhoan/ThongTin'
    Assert ($result.Status -eq 302 -and $result.Location -eq '/TaiKhoan/ThongTin') 'Trim and case-insensitive login with local return URL'
    $result = Request 'GET' '/TaiKhoan/ThongTin'
    Assert ($result.Status -eq 200 -and $result.Body.Contains('Admin Test')) 'Authenticated account page'
    $cookies = $handler.CookieContainer.GetCookies([Uri]$baseUrl)
    Assert ($cookies['QuanLyDatSan.Auth'].HttpOnly -and $cookies['QuanLyDatSan.Session'].HttpOnly) 'Auth and Session cookies are HttpOnly'
    $getLogout = Request 'GET' '/TaiKhoan/DangXuat'
    Assert ($getLogout.Status -in @(404,405) -and (Request 'GET' '/TaiKhoan/ThongTin').Status -eq 200) 'GET cannot log out'
    Assert ((Request 'POST' '/TaiKhoan/DangXuat').Status -eq 400) 'Logout rejects missing antiforgery token'
    $null = Execute-Sql "UPDATE TaiKhoan SET HoTen='Updated Name' WHERE TenDangNhap='admin_test'"
    Assert ((Request 'GET' '/TaiKhoan/ThongTin').Body.Contains('Updated Name')) 'Name refreshed from database'
    $null = Execute-Sql "UPDATE TaiKhoan SET VaiTro=2 WHERE TenDangNhap='admin_test'"
    Assert ((Request 'GET' '/TaiKhoan/ThongTin').Status -eq 302) 'Role change invalidates existing session'
    Assert ((Login).Status -eq 302) 'Can log in again with new role'
    $null = Execute-Sql "UPDATE TaiKhoan SET TrangThai=0 WHERE TenDangNhap='admin_test'"
    Assert ((Request 'GET' '/TaiKhoan/ThongTin').Status -eq 302) 'Lock invalidates existing session'
    Assert ((Login).Status -eq 200) 'Locked account cannot log in'
    $null = Execute-Sql "UPDATE TaiKhoan SET TrangThai=1, MatKhau='invalid_hash' WHERE TenDangNhap='admin_test'"
    Assert ((Login).Status -eq 200) 'Invalid password hash fails without server error'
    $command = $sql.CreateCommand()
    $command.CommandText = "UPDATE TaiKhoan SET MatKhau=@hash WHERE TenDangNhap='admin_test'"
    $null = $command.Parameters.AddWithValue('@hash', $hash)
    $null = $command.ExecuteNonQuery()
    $command.Dispose()
    $result = Login 'admin_test' $testPassword 'https://example.test/outside'
    Assert ($result.Status -eq 302 -and $result.Location -eq '/TaiKhoan/ThongTin') 'External return URL blocked'
    # Removing Session while retaining the auth cookie must invalidate authentication.
    $handler.CookieContainer.GetCookies([Uri]$baseUrl)['QuanLyDatSan.Session'].Expired = $true
    Assert ((Request 'GET' '/TaiKhoan/ThongTin').Status -eq 302) 'Missing Session invalidates auth cookie'
    Assert ((Login).Status -eq 302) 'Fresh login succeeds'
    $oldCookies = $handler.CookieContainer.GetCookieHeader([Uri]$baseUrl)
    $result = Request 'POST' '/TaiKhoan/DangXuat' @{ __RequestVerificationToken=(Token '/TaiKhoan/ThongTin') }
    Assert ($result.Status -eq 302 -and (Request 'GET' '/TaiKhoan/ThongTin').Status -eq 302) 'Logout clears access'
    $replayHandler = [Net.Http.HttpClientHandler]::new()
    $replayHandler.AllowAutoRedirect = $false
    $replayHandler.UseCookies = $false
    $replay = [Net.Http.HttpClient]::new($replayHandler)
    try {
        $replay.DefaultRequestHeaders.Add('Cookie', $oldCookies)
        $response = $replay.GetAsync("$baseUrl/TaiKhoan/ThongTin").GetAwaiter().GetResult()
        Assert ([int]$response.StatusCode -eq 302) 'Logged-out cookies cannot restore session'
        $response.Dispose()
    } finally { $replay.Dispose(); $replayHandler.Dispose() }
    Assert ((Login).Status -eq 429) 'Login limit: 10 POST requests per minute per IP'
    Assert ((Request 'GET' '/TaiKhoan/TuChoiTruyCap').Status -eq 403) 'Access-denied page returns 403'
}
finally {
    if ($client) { $client.Dispose() }
    if ($handler) { $handler.Dispose() }
    if ($server -and !$server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if ($sql) { $sql.Dispose() }
    [Data.SqlClient.SqlConnection]::ClearAllPools()
    # Only drop the unique database created by this invocation, never a configured application database.
    if ($dbName -match '^QuanLyDatSan_M1_LoginTest_[0-9a-f]{32}$') {
        $master = [Data.SqlClient.SqlConnection]::new('Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;Connect Timeout=5')
        try {
            $master.Open()
            $command = $master.CreateCommand()
            $command.CommandText = "IF DB_ID('$dbName') IS NOT NULL BEGIN ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$dbName]; END"
            $null = $command.ExecuteNonQuery()
        } finally { $master.Dispose() }
    }
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
}

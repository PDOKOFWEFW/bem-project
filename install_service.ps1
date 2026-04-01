param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ServiceName = "EndpointAgent",
    [string]$DisplayName = "Endpoint Agent Service",
    [string]$Description = "BemApi icin tarayici eklenti envanterini raporlayan ve politikayi uygulayan endpoint ajan.",
    [string]$InstallPath = "C:\Services\EndpointAgent",
    [string]$EnrollmentToken = ""
)

Write-Host "=== EndpointAgent publish islemi basliyor ==="

$projPath = Join-Path $PSScriptRoot "EndpointAgent.csproj"

dotnet publish $projPath `
  -c $Configuration `
  -r $Runtime `
  --self-contained false `
  -o $InstallPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish basarisiz oldu. Script sonlandiriliyor."
    exit 1
}

Write-Host "Publish tamamlandi. Çikis klasoru: $InstallPath"

# Kurulum sırasında token dışarıdan verilmişse, publish çıktısındaki appsettings'i güncelle.
if (-not [string]::IsNullOrWhiteSpace($EnrollmentToken)) {
    $appSettingsPath = Join-Path $InstallPath "appsettings.json"
    if (Test-Path $appSettingsPath) {
        Write-Host "appsettings.json icindeki EnrollmentToken guncelleniyor..."
        $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        if ($null -eq $json.SecuritySettings) {
            $json | Add-Member -NotePropertyName "SecuritySettings" -NotePropertyValue (@{}) -Force
        }
        $json.SecuritySettings.EnrollmentToken = $EnrollmentToken
        $json | ConvertTo-Json -Depth 10 | Set-Content -Path $appSettingsPath -Encoding UTF8
    } else {
        Write-Warning "appsettings.json bulunamadi, EnrollmentToken uygulanamadi: $appSettingsPath"
    }
}

# LocalSystem icin klasor yetkileri (Full Control)
Write-Host "Klasor ACL ayarlaniyor (LocalSystem Full Control): $InstallPath"
& icacls $InstallPath /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" /T | Out-Null

# HKLM\SOFTWARE\EndpointAgent registry key ACL (LocalSystem Full Control)
$regPath = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\EndpointAgent"
if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}

$acl = Get-Acl -Path $regPath
$rule = New-Object System.Security.AccessControl.RegistryAccessRule(
    "NT AUTHORITY\SYSTEM",
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl -Path $regPath -AclObject $acl
Write-Host "Registry ACL ayarlandi: HKLM\SOFTWARE\EndpointAgent"

# Hizmet zaten var mi kontrol et
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($null -ne $existingService) {
    Write-Host "Service zaten mevcut: $ServiceName. Once durdurulacak ve binPath guncellenecek."
    try {
        if ($existingService.Status -ne 'Stopped') {
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        }
    } catch { }

    # Mevcut servisin binPath'ini guncellemek icin sc config kullanabiliriz.
    $exePath = Join-Path $InstallPath "EndpointAgent.exe"
    & sc.exe config $ServiceName binPath= "`"$exePath`""
    & sc.exe config $ServiceName start= delayed-auto
} else {
    Write-Host "Yeni Windows Service olusturuluyor: $ServiceName"

    $exePath = Join-Path $InstallPath "EndpointAgent.exe"

    & sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto `
        DisplayName= "`"$DisplayName`""

    & sc.exe description $ServiceName "$Description"
    & sc.exe config $ServiceName start= delayed-auto
}

Write-Host "Service baslatiliyor: $ServiceName"
Start-Service -Name $ServiceName

Write-Host "Tamamlandi. Service durumu:"
Get-Service -Name $ServiceName


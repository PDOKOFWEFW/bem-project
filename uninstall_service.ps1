param(
    [string]$ServiceName = "EndpointAgent",
    [string]$InstallPath = "C:\Services\EndpointAgent",
    [switch]$RemoveRegistry = $true,
    [switch]$RemoveFiles = $true
)

Write-Host "=== EndpointAgent uninstall basliyor ==="

# 1) Servisi durdur
$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $svc) {
    if ($svc.Status -ne "Stopped") {
        Write-Host "Service durduruluyor: $ServiceName"
        try {
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        } catch {
            Write-Warning "Service durdurulamadi: $($_.Exception.Message)"
        }
    }

    # 2) Servisi sil
    Write-Host "Service siliniyor: $ServiceName"
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
} else {
    Write-Host "Service bulunamadi: $ServiceName"
}

# 3) Registry temizligi
if ($RemoveRegistry) {
    $agentReg = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\EndpointAgent"
    if (Test-Path $agentReg) {
        Write-Host "Registry anahtari siliniyor: HKLM\SOFTWARE\EndpointAgent"
        try {
            Remove-Item -Path $agentReg -Recurse -Force -ErrorAction Stop
        } catch {
            Write-Warning "Registry anahtari silinemedi: $($_.Exception.Message)"
        }
    }
}

# 4) Dosya temizligi
if ($RemoveFiles) {
    if (Test-Path $InstallPath) {
        Write-Host "Kurulum klasoru siliniyor: $InstallPath"
        try {
            Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
        } catch {
            Write-Warning "Kurulum klasoru silinemedi: $($_.Exception.Message)"
        }
    }
}

Write-Host "Uninstall tamamlandi."


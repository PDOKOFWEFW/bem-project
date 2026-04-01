# EndpointAgent Log Izleme Rehberi

Bu rehber, `EndpointAgent` Windows Service calisirken sorun gidermeyi hizlandirmak icin Event Viewer ve dosya tabanli log izleme adimlarini ozetler.

## 1) Windows Event Viewer uzerinden izleme

1. `Win + R` -> `eventvwr.msc` yazip acin.
2. `Windows Logs` -> `Application` altina gidin.
3. Sag panelden `Filter Current Log...` secin.
4. `Event sources` alaninda asagidaki kaynaklari filtreleyin:
   - `.NET Runtime`
   - `Application Error`
   - `EndpointAgent` (kaynak olusturulduysa)
5. `Error` ve `Warning` seviyelerini ozellikle inceleyin.

### PowerShell ile hizli son hata listeleme

```powershell
Get-WinEvent -LogName Application -MaxEvents 200 |
  Where-Object { $_.LevelDisplayName -in @("Error","Warning") } |
  Select-Object TimeCreated, ProviderName, Id, Message
```

## 2) Uygulama seviyesi dosya loglama

Mevcut kod tabaninda konsol/event odakli loglama var. Dosya loglama icin en pratik yol `Serilog` kullanmaktir.

### Onerilen NuGet paketleri

```powershell
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Settings.Configuration
```

### appsettings.json ornek (dosya loglama)

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.File" ],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "C:\\Services\\EndpointAgent\\logs\\agent-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14,
          "shared": true,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Program.cs entegrasyon notu

`Host` olusumunda `UseSerilog(...)` ile appsettings konfigurasyonu okunur hale getirilmelidir.

## 3) Operasyonel troubleshooting kontrol listesi

- Servis durumu: `Get-Service EndpointAgent`
- Son restart zamani / crash: Event Viewer Application loglari
- Token/config hatasi: `C:\Services\EndpointAgent\appsettings.json`
- Registry erisim hatasi:
  - `HKLM\SOFTWARE\EndpointAgent`
  - `HKLM\SOFTWARE\Policies\Google\Chrome\...`
  - `HKLM\SOFTWARE\Policies\Microsoft\Edge\...`


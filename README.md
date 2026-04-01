# BEM Project — Browser Extension Management

Çok katmanlı bir **tarayıcı eklentisi yönetim** çözümü: uç nokta **ajan (Windows Worker)**, **.NET 8 Web API** ve statik **Admin Dashboard** bileşenlerinden oluşur. Amaç; cihazlardaki Chrome/Edge eklenti envanterini toplamak, merkezi politikaları (zorunlu yükleme, blok / izin listeleri ve isteğe bağlı tarayıcı ayarları) uygulamak ve yöneticilere özet görünürlük sağlamaktır.

## Mimari

| Katman | Teknoloji | Rol |
|--------|-----------|-----|
| **Agent** | .NET 8 Worker (`Microsoft.NET.Sdk.Worker`), Windows Service | Periyodik eklenti keşfi, backend’e rapor, gelen `DevicePolicyResponse` ile registry politikaları |
| **Backend API** | ASP.NET Core 8, EF Core, SQLite | `POST /api/device/report`, `GET /api/dashboard`, OU bazlı politika motoru, denetim kayıtları |
| **Dashboard** | HTML / CSS / JS, Chart.js | `assets/js/dashboard.js` üzerinden canlı KPI, grafikler ve cihaz tablosu |

Akış özetle: **Agent** → **API** (cihaz + eklenti listesi) → **PolicyEngine** (OU kuralları) → yanıt ile **PolicyEnforcer** (HKLM politikaları). Dashboard yalnızca **GET `/api/dashboard`** kullanır.

## Önkoşullar

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows (ajan ve servis için; API ve SQLite geliştirme için macOS/Linux da mümkün)
- Git

## Kurulum

### 1. Depoyu klonlayın

```bash
git clone <repository-url>
cd "BEM PROJECT"
```

### 2. Yapılandırma (gizli bilgiler)

**Asla gerçek token veya üretim bağlantı dizelerini repoya commit etmeyin.**

- **Agent:** `appsettings.json` içinde `SecuritySettings:EnrollmentToken` değerini yerel ortamda doldurun veya [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) kullanın.
- **API:** `EndpointApi/appsettings.json` aynı token ile uyumlu olmalıdır (ajan ve API eşleşmeli).
- İsteğe bağlı: `EndpointApi` için `ApiSettings:CorsOrigins` — dashboard’u farklı bir origin’den (ör. Live Server) açacaksanız API adresinizi ekleyin.

Örnek yerel `EnrollmentToken`:

```json
"SecuritySettings": {
  "EnrollmentToken": "<güçlü-rastgele-değer>"
}
```

### 3. Backend API’yi çalıştırma

```bash
cd EndpointApi
dotnet run
```

Varsayılan örnek: `http://localhost:5000` (Swagger: Development ortamında `/swagger`).

İlk çalıştırmada SQLite dosyası (`endpoint.db`) oluşturulur; OU tohumları `DbInitializer` ile yüklenir.

### 4. Agent’ı çalıştırma

Proje kökünden:

```bash
dotnet run
```

Windows hizmeti olarak çalıştırma ve ayrıntılar için projedeki yardımcı betiklere bakın (`install_service.ps1` vb.).

### 5. Dashboard

`index.html` ve `devices.html` dosyalarını bir statik sunucu ile açın veya doğrudan tarayıcıda açın. API farklı bir origin’den geliyorsa:

- HTML içindeki `<meta name="endpoint-api-base" content="http://localhost:5000">` adresini güncelleyin, **veya**
- `window.ENDPOINT_API_BASE` atayın.

CORS hatası alırsanız API `appsettings.json` içinde `ApiSettings:CorsOrigins` listesine dashboard origin’inizi ekleyin.

## Önemli API uçları

| Metot | Yol | Açıklama |
|-------|-----|----------|
| POST | `/api/device/report` | Ajan; `DeviceReportPayload` (cihaz, eklentiler, kullanıcı/IP alanları) |
| GET | `/api/dashboard` | Dashboard; KPI, trend, risk dağılımı, son olaylar, cihaz listesi |

## Güvenlik notları (Google Workspace / kurumsal)

- **Enrollment token** paylaşılmamalı; depoda yalnızca yer tutucu bulunmalı.
- Registry (HKLM) yazımları tipik olarak **yönetici** ayrıcalığı gerektirir.
- Üretimde SQLite yerine yönetilen bir veritabanı ve sırların Azure Key Vault / Secret Manager ile yönetimi değerlendirilmelidir.

## Proje yapısı (özet)

```
BEM PROJECT/
├── EndpointAgent (Worker, servisler, PolicyEnforcer, ApiReporter, …)
├── EndpointApi/          # Web API, EF Core, kontrolörler
├── assets/js/dashboard.js
├── index.html, devices.html, …
└── appsettings.json      # Ajan — yerelde doldurulacak token
```

## Katkı ve lisans

İhtiyaçlarınıza göre README’yi genişletebilirsiniz; kurumsal kullanımda uyumluluk ve veri işleme politikalarını eklemeniz önerilir.

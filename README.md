# EnergyTrading Background Worker

.NET 10, Hangfire, PostgreSQL ve Clean Architecture ile EPİAŞ PTF ve Sistem Marjinal Fiyatı verilerini alan worker uygulaması.

## Mimari

- `EnergyTrading.Domain`: Entity ve ortak domain tipleri; başka projeye bağımlı değildir.
- `EnergyTrading.Application`: Use-case, generic repository sözleşmeleri, EPİAŞ contract modelleri ve `IntegrationJobBase<T>`.
- `EnergyTrading.Infrastructure`: EF Core/Npgsql, generic repository, log servisi, HTTP client'lar ve migration.
- `EnergyTrading.Worker`: DI composition root, Hangfire storage/server, recurring job ve Hangfire policy adaptörü.
- `EnergyTrading.Tests`: HTTP, cache, deserialize, idempotent insert/update, log ve repository testleri.

PostgreSQL fiziksel tablo, kolon, index ve constraint adları küçük harfli `snake_case` olarak oluşturulur. C# sınıf ve property adları PascalCase kalır; SQL sorgularında çift tırnak gerekmez.

## Doğrulanan EPİAŞ sözleşmesi

Resmî teknik dokümanın 5.128 ve model bölümleri esas alınmıştır:

- Authentication: `POST https://giris.epias.com.tr/cas/v1/tickets`, `application/x-www-form-urlencoded`, `201 Created`, düz metin `TGT-*` yanıtı.
- TGT geçerliliği dokümana göre iki saattir; uygulama emniyet payıyla 115 dakika cache'ler ve eş zamanlı yenilemeyi `SemaphoreSlim` ile tekilleştirir.
- PTF: `POST /v1/markets/dam/data/mcp`, header `TGT`, body `startDate`, `endDate`, opsiyonel `page`.
- Response: `items[].date`, `hour`, `price`, `priceUsd`, `priceEur`.
- PTF business key: `Date + TimeOfPeriodId`; `hour` başlangıç saati 1 tabanlı dönem kimliğine çevrilir.
- Sistem Marjinal Fiyatı: `POST /v1/markets/bpm/data/system-marginal-price`; request alanları `startDate`, `endDate`, opsiyonel `region` ve `page`; response alanları `items[].date`, `hour`, `systemMarginalPrice`.
- SMF çağrısında dokümandaki geçerli bölge kısa adı `TR1`, `Transparency:SystemMarginalPriceRegion` ayarı üzerinden gönderilir.

Job her gün `Europe/Istanbul` saat diliminde `15:00` (`0 15 * * *`) çalışır ve Türkiye yerel tarihine göre bir sonraki günün PTF verisini ister. Hangfire `DisableConcurrentExecution` filtresi aynı job metodunun ikinci instance'ını engeller; PTF ve SMF farklı metotlar olduğu için birbirini bloke etmez. Lock alma işlemi en fazla 60 saniye bekler. 60/300/900 saniye aralıklı üç retry uygulanır. Her deneme ayrı log kaydıdır ve `RetryCount` Hangfire parametresinden alınır. Hata loglandıktan sonra exception yeniden fırlatılır.

Sistem Marjinal Fiyatı job'ı her saat başında (`0 * * * *`) çalışır. Geçmiş gün aralıklarında bitiş, seçilen son günün ertesi `00:00:00+03:00` değeridir. Bugün seçildiğinde servis yalnız geçmiş `endDate` kabul ettiği için Türkiye saatinin bir dakika öncesi kullanılır; gelecek tarihler queue öncesinde reddedilir. `date + time_of_period_id` unique business key'i üzerinden eksik dönemleri ekler, fiyatı değişen dönemleri günceller ve değişmeyen kayıtları yazmaz.

Worker aynı zamanda operasyon web sunucusudur. `/` manuel job çalıştırma ekranını, `/hangfire` ise Hangfire Dashboard'u yayınlar. Development ortamında doğrudan erişilebilir; diğer ortamlarda Basic Authentication zorunludur ve credential yapılandırılmamışsa operasyon yüzeyi güvenli biçimde kapalı kalır.

## Güvenli yapılandırma

Repository'deki `appsettings.json` yalnız boş credential şablonu içerir. Development için:

```powershell
dotnet user-secrets set --project EnergyTrading.Worker "ConnectionStrings:EnergyTrading" "Host=localhost;Port=5432;Database=EnergyTrading;Username=userEnergyTrade;Password=<PASSWORD>"
dotnet user-secrets set --project EnergyTrading.Worker "Transparency:Username" "<USERNAME>"
dotnet user-secrets set --project EnergyTrading.Worker "Transparency:Password" "<PASSWORD>"
dotnet user-secrets set --project EnergyTrading.Worker "WorkerOperation:Username" "<WORKER_OPERATION_USERNAME>"
dotnet user-secrets set --project EnergyTrading.Worker "WorkerOperation:Password" "<STRONG_PASSWORD>"
```

Environment variable alternatifi:

```powershell
$env:ConnectionStrings__EnergyTrading='Host=localhost;Port=5432;Database=EnergyTrading;Username=userEnergyTrade;Password=<PASSWORD>'
$env:Transparency__Username='<USERNAME>'
$env:Transparency__Password='<PASSWORD>'
$env:WorkerOperation__Username='<WORKER_OPERATION_USERNAME>'
$env:WorkerOperation__Password='<STRONG_PASSWORD>'
```

## Veritabanı ve çalıştırma

`.NET 10 SDK` ve PostgreSQL gereklidir. Hangfire tablolarını `Hangfire.PostgreSql` ilk bağlantıda hazırlar.
Uygulama varsayılan olarak başlangıçta bekleyen EF Core migration'larını uygular (`Database:ApplyMigrationsOnStartup=true`). Birden fazla container'ın aynı anda deploy edildiği kontrollü production ortamlarında bu ayar kapatılıp migration ayrı deployment adımında çalıştırılabilir.

```powershell
psql -U postgres -f scripts/create-database.sql
dotnet tool install --global dotnet-ef
dotnet ef database update --project EnergyTrading.Infrastructure --startup-project EnergyTrading.Worker
dotnet build EnergyTrading.slnx
dotnet test EnergyTrading.slnx
dotnet run --project EnergyTrading.Worker
```

`scripts/create-database.sql` psql `\gexec` kullandığı için psql ile çalıştırılmalıdır. Alternatif olarak yalnız veritabanını oluşturup EF migration komutunu çalıştırabilirsiniz.

## Tasarım notları

- Entity başına repository yoktur; `EfGenericRepository<TEntity>` kullanılır. Mapping/index/precision sınıfları entity-specific olabilir.
- `GetListAsync`, boş dönem listesinde yanlışlıkla tam tablo çekmez ve normal sorguda filtreleri SQL'e taşır.
- Insert/update setleri transaction içinde hazırlanır, tek `SaveChangesAsync` ile yazılır.
- Log servisi `IDbContextFactory` ile ayrı DbContext kullanır; ana veri transaction'ı rollback olsa da failure logu kalır.
- Credential ve TGT hiçbir log/exception içine yazılmaz.
- Ortak `ITransparencyHttpClient.PostAsync<TRequest,TResponse>` request/response tiplerini generic yönetir; endpoint client'ları yalnız servis yolunu ve contract tiplerini belirtir.
- Manuel job ekranı job adı, başlangıç tarihi ve bitiş tarihi alır; doğrulanan çalışmayı Hangfire queue'suna ekler. Bitiş tarihi başlangıçtan önce olamaz ve aralık bir takvim ayını aşamaz. Aynı kural job base class içinde de uygulanır.
- PostgreSQL unique index'i ve uygulama diff algoritması birlikte idempotency sağlar.
- Bütün dönemsel job entity'leri `IPeriodEntity.Date` alanını `DateOnly` olarak kullanır. PostgreSQL karşılığı `date` tipidir ve saat bilgisi saklanmaz. API'den gelen ISO date-time değerleri entity mapping sırasında yerel takvim gününe dönüştürülür.

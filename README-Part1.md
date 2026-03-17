# Kuvakirjasto-API — Osa 1: Lokaali kehitys

Tässä tehtävässä rakennat kuvakirjasto-API:n täydentämällä Clean Architecture -koodipohjaa. Opit tunnistamaan ja korjaamaan tietoturvariskin (kovakoodattu API-avain), käyttämään **User Secrets** -mekanismia, soveltamaan **Options Pattern** -mallia ja toteuttamaan kuvien tallentamisen paikalliselle levylle.

Tehtävässä on valmiina koodipohja (`StarterCode/`), jossa on rakennettu projektipohja, tietokanta ja albumioperaatiot. Sinun tehtäväsi on lisätä puuttuvat osat: salaisuuksien hallinta ja kuvien tallennus.

## Mitä osaat tämän osan jälkeen?

Kun olet tehnyt tämän osan loppuun, osaat:
- tunnistaa miksi kovakoodatut salaisuudet ovat riski ja miten riski poistetaan käytännössä
- käyttää `dotnet user-secrets` -mekanismia turvalliseen kehitysaikaiseen konfiguraatioon
- sitoa konfiguraation `Options Pattern` -mallilla tyypitettyihin C#-luokkiin
- toteuttaa vaihdettavan tallennuskerroksen (`IStorageService`) ilman että käyttötapauslogiikka muuttuu
- validoida tiedoston tyypin ja koon sekä testata käyttötapausta mockeilla

---

## Mitä tarvitset?

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) tai [VS Code](https://code.visualstudio.com/) + C# Dev Kit
- Git

---

## Lisämateriaali

- [Secrets Management — teoria](../../C%23/fin/04-Advanced/Secrets-Management/README.md)
- [Configuration ja Options Pattern](../../C%23/fin/04-Advanced/Secrets-Management/Configuration.md)

---

## Projektin rakenne

Koodipohja käyttää **Clean Architecture** -mallia. Tässä tehtävässä täydennettävät tiedostot on merkitty alla:

```
GalleryApi/
├── GalleryApi.Domain/          ← Ydin: entiteetit ja rajapinnat
│   ├── Entities/
│   │   ├── Album.cs
│   │   └── Photo.cs
│   └── Interfaces/
│       ├── IAlbumRepository.cs
│       ├── IPhotoRepository.cs
│       └── IStorageService.cs  ← Tallennuksen abstraktio
│
├── GalleryApi.Application/     ← Sovelluslogiikka: käyttötapaukset
│   ├── DTOs/
│   └── UseCases/
│       ├── Albums/             ← Valmiina
│       └── Photos/
│           ├── GetPhotosByAlbumUseCase.cs  ← Valmis
│           ├── UploadPhotoUseCase.cs       ← Täydennettävä (Vaihe 7)
│           └── DeletePhotoUseCase.cs       ← Täydennettävä (Vaihe 8)
│
├── GalleryApi.Infrastructure/  ← Tekninen toteutus
│   ├── Persistence/            ← EF Core + SQLite (Valmiina)
│   ├── Storage/
│   │   ├── LocalStorageService.cs     ← Täydennettävä (Vaihe 5)
│   │   └── AzureBlobStorageService.cs ← Osa 2:ssa
│   ├── Moderation/
│   │   └── ModerationServiceClient.cs ← Täydennettävä (Vaihe 4)
│   └── DependencyInjection.cs  ← Täydennettävä (Vaihe 6)
│
├── GalleryApi.Tests/
│   └── UseCases/
│       └── GetAlbumByIdUseCaseTests.cs ← Esimerkkitesti
│
└── GalleryApi.WebApi/
    ├── Controllers/
    │   ├── AlbumsController.cs ← Valmis
    │   └── PhotosController.cs ← Valmis (kutsuu käyttötapauksia)
    └── Program.cs              ← ONGELMA: kovakoodattu avain (Vaihe 2)
```

**Tärkeää tässä tehtävässä:** `IStorageService`-rajapinta on määritelty Domain-kerroksessa. Kun vaihdat Osa 2:ssa tallennuksen paikallisesta levystä Azure Blob Storageen, muutat *vain* `DependencyInjection.cs`:n yhtä riviä — `UploadPhotoUseCase` ja `PhotosController` pysyvät täsmälleen samana.

---

## Vaihe 1: Avaa koodipohja ja tutki rakennetta

**1.1** Avaa projekti.

Valitse alla oleva tapa sen mukaan, käytätkö VS Codea vai Visual Studiota:

<details>
<summary><strong>Vaihtoehto A: Komentorivi (VS Code)</strong></summary>

```bash
cd StarterCode/GalleryApi
code .
```

</details>

<details>
<summary><strong>Vaihtoehto B: Visual Studio</strong></summary>

Avaa Visual Studio → **File → Open → Project/Solution** → etsi ja valitse `StarterCode/GalleryApi/GalleryApi.sln` → **Open**

</details>

**1.2** Aja testit:

```bash
dotnet test
```

Kolme testiä pitäisi mennä läpi `GetAlbumByIdUseCaseTests`-luokasta. Katso myös testikoodi — huomaat kuinka käyttötapaukset ovat testattavissa täysin ilman tietokantaa `Mock<IAlbumRepository>`-tekniikalla.

**1.3** Käynnistä sovellus:

```bash
cd GalleryApi.WebApi
dotnet run
```

Avaa `https://localhost:PORT/swagger`. Kokeile `GET /api/albums` — se palauttaa tyhjän listan. Albumioperaatiot toimivat.

Kokeile myös kuvien latausta (`POST /api/albums/{id}/photos`) — saat `500 Internal Server Error`. Tämä on odotettua: `LocalStorageService` ja käyttötapaukset sisältävät vielä `throw new NotImplementedException()`. Toteutat ne Vaiheissa 5, 7 ja 8.

**1.4** Tutki erityisesti näitä tiedostoja ennen kuin jatkat:
- `Program.cs` — etsi ONGELMA-kommentti
- `GalleryApi.Domain/Interfaces/IStorageService.cs` — tallennuksen rajapinta
- `GalleryApi.Infrastructure/DependencyInjection.cs` — TODO-kommentti

---

## Vaihe 2: Tunnista tietoturvariski

Avaa `GalleryApi.WebApi/Program.cs`. Löydät alussa:

```csharp
// ONGELMA: API-avain on kovakoodattu suoraan lähdekoodiin!
var moderationClient = new ModerationServiceClient("sk-moderation-hardcoded-dev-12345");
builder.Services.AddSingleton(moderationClient);
```

Tämä on **vakava tietoturvaongelma**. Miksi?

### Git-historia ei unohda

Kun kovakoodattu avain kerran lisätään `git commit`-komennolla, se **jää git-historiaan ikuisesti** — vaikka poistaisit sen myöhemmin.

```bash
# Näin voi löytää "poistetun" avaimen historiasta (bash + rg)
git log --all -p | rg "sk-moderation"
```

> `.gitignore` ei auta — se estää vain tulevat commitit, ei jo tallentuneita.

### Automatisoidut botit skannaavat GitHubin

Julkisissa repositorioissa: botit löytävät API-avaimet tyypillisesti **minuuteissa** commitista. Avain voi päätyä väärinkäyttöön heti.

### Muita ongelmia

| Ongelma | Seuraus |
|---|---|
| Kaikki kehittäjät jakavat saman avaimen | Ei tiedetä kuka käytti avainta |
| Avain lähdekoodissa = avain kaikissa haaroissa | Ympäristökohtaiset avaimet mahdottomia |
| Avain versionhallinnassa | Lähtee mukaan kaikille, jotka kloonaavat projektin |

**Ratkaisu:** User Secrets kehityksessä, pilvipalvelun salaisuudenhallinta tuotannossa (Osat 2 ja 3).

---

## Vaihe 3: Korjaa salaisuus — User Secrets

### Mikä on User Secrets?

**User Secrets** on ASP.NET Coren sisäänrakennettu mekanismi kehitysaikaisten salaisuuksien hallintaan. Ajatus on yksinkertainen: salaisuudet tallennetaan **projektin ulkopuolelle** käyttäjän kotihakemistoon erilliseen `secrets.json`-tiedostoon.

```
ILMAN User Secretsiä:
  appsettings.json  →  versiohallinnan kautta kaikille  → RISKI

USER SECRETSIN KANSSA:
  secrets.json  →  vain sinun koneellasi  → turvallinen
  appsettings.json  →  sisältää vain tyhjän placeholderin  → voidaan committaa
```

Kun sovellus käynnistyy kehitysmoodissa, ASP.NET Core lukee automaattisesti sekä `appsettings.json`:n *että* `secrets.json`:n, ja `secrets.json`:n arvot *ylikirjoittavat* `appsettings.json`:n arvot. Et tarvitse mitään erikoiskoodi tätä varten — se tapahtuu automaattisesti.

**Miten sovellus "löytää" oikean `secrets.json`-tiedoston?**

`GalleryApi.WebApi.csproj`-tiedostossa on tunniste:
```xml
<UserSecretsId>gallery-api-dev-secrets</UserSecretsId>
```
Tämä tunniste yhdistää projektin oikeaan salaisuustiedostoon käyttäjän kotihakemistossa. Sama tunniste voi olla vain yhdessä projektissa — näin eri projektien salaisuudet pysyvät erillään.

**Missä `secrets.json` sijaitsee fyysisesti?**

| Käyttöjärjestelmä | Sijainti |
|---|---|
| Windows | `%APPDATA%\Microsoft\UserSecrets\gallery-api-dev-secrets\secrets.json` |
| macOS / Linux | `~/.microsoft/usersecrets/gallery-api-dev-secrets/secrets.json` |

Projektin `.csproj`-tiedostossa on jo valmiiksi `<UserSecretsId>gallery-api-dev-secrets</UserSecretsId>` — sinun ei tarvitse ajaa `dotnet user-secrets init`.

Aseta nyt salaisuus käyttöön. Valitse alla oleva tapa sen mukaan, käytätkö komentoriviä vai Visual Studiota:

<details>
<summary><strong>Vaihtoehto A: Komentorivi</strong></summary>

Varmista, että olet `GalleryApi.WebApi`-hakemistossa:

```bash
cd GalleryApi.WebApi
```

Aseta salaisuus:

```bash
dotnet user-secrets set "ModerationService:ApiKey" "sk-moderation-local-dev-key"
```

Tarkista:

```bash
dotnet user-secrets list
```

Pitäisi näyttää:
```
ModerationService:ApiKey = sk-moderation-local-dev-key
```

</details>

<details>
<summary><strong>Vaihtoehto B: Visual Studio</strong></summary>

1. Solution Explorer -näkymässä: klikkaa hiiren **oikealla** `GalleryApi.WebApi`-projektin päällä
2. Valitse **Manage User Secrets**
3. Visual Studio avaa `secrets.json`-tiedoston. Kirjoita:

```json
{
  "ModerationService": {
    "ApiKey": "sk-moderation-local-dev-key"
  }
}
```

4. Tallenna (**Ctrl+S**)

</details>

### Miksi nimi on `ModerationService`?

Nimi tulee suoraan luokasta, jota konfiguroidaan: `ModerationServiceClient`. Käytäntönä on nimetä konfiguraatio-osio **palvelun mukaan**, johon se kuuluu. Näin tiedät heti mikä palvelu mitäkin asetusta käyttää:

```
ModerationService:ApiKey   →  ModerationServiceClient tarvitsee tämän
Storage:Provider           →  LocalStorageService / AzureBlobStorageService
ConnectionStrings:Default  →  tietokantayhteys
```

Jos sovelluksessa olisi toinen ulkoinen palvelu, sille tulisi oma osionsa:
```json
{
  "ModerationService": { "ApiKey": "..." },
  "EmailService":      { "ApiKey": "..." },
  "PaymentService":    { "ApiKey": "..." }
}
```

Nimeämiskäytäntö on vapaa — tärkeintä on että `appsettings.json`:n osionimi, `Options`-luokan `SectionName`-vakio ja User Secretsin avain täsmäävät keskenään.

### Miksi hierarkkinen rakenne `ModerationService:ApiKey`?

Kaksoispiste `:` on ASP.NET Coren konfiguraatioerottaja, joka vastaa JSON-hierarkiaa:

```
"ModerationService:ApiKey"  ←→  { "ModerationService": { "ApiKey": "..." } }
```

Tämä rakenne vastaa `appsettings.json`:n `ModerationService`-osion rakennetta — konfiguraatiolähteet sulautuvat yhteen saumattomasti.

**Tarkistuspiste:** Varmista, että `appsettings.json` sisältää vain tyhjän placeholderin:

```json
"ModerationService": {
  "ApiKey": "",
  "BaseUrl": "https://api.moderation-example.com"
}
```

Oikea arvo on nyt User Secretsissä, ei koodissa eikä versionhallinnassa.

---

## Vaihe 4: Options Pattern — lue salaisuus turvallisesti

### Mikä on Options Pattern?

**Options Pattern** on tapa *sitoa* (bind) konfiguraatiotiedoston osio suoraan C#-luokkaan. Sen sijaan että lukisit arvoja merkkijonoavaimilla, luot luokan jonka kentät vastaavat konfiguraation rakennetta.

Kuvittele tilanne ilman Options Pattern -mallia. Koodissa täytyy muistaa tarkat konfiguraatioavaimet merkkijonoina:

```csharp
// Ilman Options Pattern — merkkijonoavaimet eri puolilla koodia
var apiKey = configuration["ModerationService:ApiKey"];      // Kirjoitusvirhe → null
var baseUrl = configuration["ModerationService:BaseUrl"];    // Ei IntelliSensea
var timeout = int.Parse(configuration["ModerationService:TimeoutSeconds"]); // Ei tyyppiturvallisuutta
```

Options Pattern -mallilla sama asia hoituu näin:

```csharp
// Options Pattern — kaikki konfiguraatio yhdessä tyypitetyssä luokassa
public class ModerationServiceOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
}

// Käyttö palvelussa — IntelliSense toimii, ei kirjoitusvirheille mahdollisuutta
public ModerationServiceClient(IOptions<ModerationServiceOptions> options)
{
    var config = options.Value;   // config on ModerationServiceOptions-olio
    var apiKey = config.ApiKey;   // IntelliSense ehdottaa, kääntäjä tarkistaa
}
```

**Kolme osaa, jotka täytyy aina olla:**

```
1. Options-luokka         2. Rekisteröinti Program.cs:ssä    3. Injektio konstruktorissa
──────────────────         ────────────────────────────────    ─────────────────────────
ModerationServiceOptions   services.Configure<               IOptions<ModerationService-
{                            ModerationServiceOptions>(         Options> options
  ApiKey = ""                  config.GetSection("Moderation   → options.Value = olio
  BaseUrl = ""               Service"))                          jonka kentät täytetty
}                                                               konfiguraatiosta
```

`IOptions<T>` on .NET:n tarjoama "kuori" (*wrapper*), joka pitää sisällään konfiguraatio-olion. `.Value`-ominaisuus palauttaa itse olion. Syy kuoren käyttöön on se, että .NET voi hallinnoida olion elinkaarta ja tukea konfiguraation päivitystä ajon aikana.

**Miksi Options Pattern on parempi kuin suora `IConfiguration`?**

| | `IConfiguration["avain"]` | `IOptions<T>` |
|---|---|---|
| Kirjoitusvirheet | Ei huomata ennen ajoa | Kääntäjä huomaa |
| IntelliSense | Ei tue | Tukee |
| Testaaminen | Täytyy mockata IConfiguration | Voidaan antaa olio suoraan |
| Dokumentaatio | Piilotettu merkkijonoihin | Options-luokka on dokumentaatio |

**4.1** `ModerationServiceOptions`-luokka on jo valmiina `GalleryApi.Infrastructure/Options/ModerationServiceOptions.cs`:ssä. Avaa se ja tutki:

```csharp
public class ModerationServiceOptions
{
    public const string SectionName = "ModerationService";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.moderation-example.com";
}
```

**4.2** Muokkaa `GalleryApi.Infrastructure/Moderation/ModerationServiceClient.cs` ottamaan `IOptions<ModerationServiceOptions>` konstruktorissa:

```csharp
using GalleryApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GalleryApi.Infrastructure.Moderation;

public class ModerationServiceClient
{
    private readonly ModerationServiceOptions _options;

    // Muutettu: ei enää kovakoodattua merkkijonoa
    public ModerationServiceClient(IOptions<ModerationServiceOptions> options)
    {
        _options = options.Value;
    }

    public Task<bool> IsContentSafeAsync(Stream imageStream, string contentType)
    {
        // Simuloitu tarkistus — käyttäisi _options.ApiKey:ta oikeassa toteutuksessa
        return Task.FromResult(true);
    }
}
```

**4.3** Muokkaa `GalleryApi.WebApi/Program.cs`. Poista kovakoodattu rivi ja lisää Options-rekisteröinti:

```csharp
// POISTA NÄMÄ RIVIT:
// var moderationClient = new ModerationServiceClient("sk-moderation-hardcoded-dev-12345");
// builder.Services.AddSingleton(moderationClient);

// LISÄÄ TILALLE (ennen builder.Services.AddApplication()):
builder.Services.Configure<ModerationServiceOptions>(
    builder.Configuration.GetSection(ModerationServiceOptions.SectionName));

// ModerationServiceClient saa asetukset DI:n kautta
builder.Services.AddSingleton<ModerationServiceClient>();
```

**4.4** Käynnistä sovellus ja varmista se käynnistyy virheittä:

```bash
dotnet run
```

### Konfiguraation prioriteettijärjestys

ASP.NET Core lukee konfiguraation seuraavassa järjestyksessä (korkein prioriteetti ensin):

```
User Secrets            ← ModerationService:ApiKey = "sk-moderation-local-dev-key"
appsettings.Development.json
appsettings.json        ← ModerationService:ApiKey = ""   (placeholder)
```

Kehityksessä User Secrets ylikirjoittaa `appsettings.json`:n tyhjän arvon. Tuotannossa (Osa 2 ja 3) avain tulee Azure App Servicen Application Settingsistä tai Key Vaultista.

---

## Vaihe 5: Toteuta LocalStorageService

Nyt toteutat kuvatiedostojen tallentamisen paikalliselle levylle.

### Miksi kuvia ei tallenneta tietokantaan?

Ennen toteutusta on hyvä ymmärtää miksi kuvatiedostot tallennetaan tiedostojärjestelmään (tai Blob Storageen) eikä suoraan tietokantaan. Tietokantaan voisi periaatteessa tallentaa kuvan `byte[]`-muodossa, mutta se johtaa useisiin ongelmiin:

| Ongelma | Selitys |
|---|---|
| **Suorituskyky** | Tietokanta on optimoitu rakenteiselle datalle, ei isoille binaariblobeille. Yksittäinen 5 MB kuva hidastaa jokaista kyselyä, joka hakee albumin kuvat. |
| **Muistin kulutus** | Tietokantayhteys pitää koko binäärin muistissa kunnes se on lähetetty asiakkaalle. Satoja samanaikaisia pyyntöjä → muisti loppuu nopeasti. |
| **Välimuistitus** — CDN | Tiedostojärjestelmässä tai Blob Storagessa olevalle kuvalle voidaan antaa suora HTTP URL. CDN (Content Delivery Network) osaa välimuistittaa sen automaattisesti maailmanlaajuisesti. Tietokantakyselyä ei voi välimuistittaa samoin. |
| **Varmuuskopiointi** | Tietokantavarmuuskopiot kasvavat valtaviksi jos kuvat ovat sisällä. Tiedostot ja tietokanta kannattaa varmuuskopioida erikseen eri rytmillä. |
| **Skaalautuvuus** | Blob Storage (Osa 2) skaalautuu automaattisesti. Jos tallennettaisiin tietokantaan, tietokantapalvelin muodostuisi pullonkaulaksi. |

**Sen sijaan tietokantaan tallennetaan vain URL** — kevyt merkkijono, josta sovellus tietää mistä tiedosto löytyy:

```
Tietokanta (Photo-taulu):
  Id        = guid
  Title     = "Auringonlasku"
  ImageUrl  = "/uploads/abc123/photo.jpg"   ← vain URL, ei binääridataa
  FileName  = "photo.jpg"

Tiedostojärjestelmä / Blob Storage:
  wwwroot/uploads/abc123/photo.jpg          ← varsinainen tiedosto
```

Näin tietokanta pysyy nopeana ja pienenä, ja tiedostot voidaan siirtää palvelimelta toiselle (tai kehityskoneelta Azure Blob Storageen) muuttamatta tietokantaskeemaa lainkaan.

**Mikä on `IStorageService`?**

Domain-kerroksen rajapinta, joka piilottaa toteutuksen yksityiskohdat:

```
Application-kerros: kutsuu IStorageService.UploadAsync(...)
                                    │
              ┌─────────────────────┴─────────────────────┐
              ▼                                           ▼
   LocalStorageService                      AzureBlobStorageService
   (Osa 1 — kehityksessä)                  (Osa 2 — tuotannossa)
```

Avaa `GalleryApi.Infrastructure/Storage/LocalStorageService.cs`. Tiedostossa on TODO-kommentit. Toteuta luokka:

**5.1** Lisää konstruktori (`IWebHostEnvironment` antaa sovelluksen juuripolun):

```csharp
using GalleryApi.Domain.Interfaces;
using GalleryApi.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace GalleryApi.Infrastructure.Storage;

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IWebHostEnvironment env, IOptions<StorageOptions> opts)
    {
        // Yhdistää juuripolun ja konfiguroitun suhteellisen polun
        // Esim: "C:/projects/GalleryApi/GalleryApi.WebApi" + "wwwroot/uploads"
        _basePath = Path.Combine(env.ContentRootPath, opts.Value.BasePath);
    }
```

**5.2** Toteuta `UploadAsync`:

```csharp
    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId)
    {
        // Luo albumikohtainen kansio
        var albumDir = Path.Combine(_basePath, albumId.ToString());
        Directory.CreateDirectory(albumDir);

        // Kirjoita tiedosto
        var filePath = Path.Combine(albumDir, fileName);
        using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output);

        // Palauta URL — UseStaticFiles() tarjoilee wwwroot/-kansion
        return $"/uploads/{albumId}/{fileName}";
    }
```

**5.3** Toteuta `DeleteAsync`:

```csharp
    public Task DeleteAsync(string fileName, Guid albumId)
    {
        var filePath = Path.Combine(_basePath, albumId.ToString(), fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}
```

**Miksi URL on `/uploads/albumId/fileName`?**

`Program.cs`:ssa on `app.UseStaticFiles()`. Se kertoo ASP.NET Corelle, että `wwwroot/`-kansion sisältö tarjoillaan HTTP:nä:
- Tiedosto tallennetaan: `wwwroot/uploads/abc123/photo.jpg`
- Selaimessa löytyy: `https://localhost:PORT/uploads/abc123/photo.jpg`

---

## Vaihe 6: Rekisteröi IStorageService riippuvuusinjektioon

### Mitä "rekisteröiminen" tarkoittaa?

ASP.NET Coressa on **DI-säiliö** (Dependency Injection container) — ohjelma joka hallinnoi olioita puolestasi. Kun luokka (esim. `UploadPhotoUseCase`) tarvitsee `IStorageService`-toteutuksen, se ei luo sitä itse. Se vain ilmoittaa konstruktorissaan: "minä tarvitsen jotain, joka toteuttaa `IStorageService`." DI-säiliö etsii rekisteristä sopivan toteutuksen ja antaa sen automaattisesti.

```
UploadPhotoUseCase konstruktori:
  public UploadPhotoUseCase(IStorageService storageService, ...)
                                  ↑
                  DI-säiliö etsii rekisteristä:
                  "Kuka toteuttaa IStorageService?"
                                  ↑
                  DependencyInjection.cs sanoo:
                  services.AddScoped<IStorageService, LocalStorageService>()
                  → "LocalStorageService toteuttaa sen"
```

Ilman rekisteröintiä DI-säiliö ei tiedä mitä antaa, ja sovellus kaatuu käynnistyksessä virheeseen kuten:
> `Unable to resolve service for type 'IStorageService'`

**Mikä on `AddScoped`?**

`AddScoped` tarkoittaa: "luo uusi olio jokaista HTTP-pyyntöä varten". Samaan pyyntöön liittyvät kaikki luokat saavat saman olion, mutta eri pyynnöt saavat omat olioidensa.

| Rekisteröintitapa | Milloin käytetään |
|---|---|
| `AddScoped` | Useimmiten — yksi olio per HTTP-pyyntö |
| `AddSingleton` | Yksi olio koko sovelluksen elinajan (esim. `ModerationServiceClient`) |
| `AddTransient` | Uusi olio joka kerta kun pyydetään |

Tällä hetkellä `IStorageService`-rajapintaa ei ole rekisteröity — sovellus kaatuisi jos yrittäisi käyttää sitä. Avaa `GalleryApi.Infrastructure/DependencyInjection.cs` ja etsi TODO-kommentti.

**6.1** Lisää rekisteröinti:

```csharp
using GalleryApi.Domain.Interfaces;
using GalleryApi.Infrastructure.Options;   // ← Lisää tämä
using GalleryApi.Infrastructure.Persistence;
using GalleryApi.Infrastructure.Storage;   // ← Lisää tämä
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GalleryApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Rekisteröi tietokantayhteys — käyttää SQLiteä oletuksena
        // "Default" viittaa appsettings.json:n ConnectionStrings:Default-arvoon
        services.AddDbContext<GalleryDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("Default") ?? "Data Source=gallery.db"));

        // Rekisteröi repositoriot — DI antaa näiden toteutukset automaattisesti
        // kun jokin luokka pyytää IAlbumRepository- tai IPhotoRepository-tyyppiä
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();

        // Valitaan tallennustoteutus konfiguraatioarvon perusteella.
        // Lokaalissa kehityksessä appsettings.json:ssa on Storage:Provider = "local",
        // Azuressa App Settingsissä asetetaan Storage:Provider = "azure" (Osa 2).
        // Näin sama koodi toimii molemmissa ympäristöissä — vain yksi rivi konfiguraatiossa muuttuu.
        // StorageOptions-vakiot estävät magic stringit — kirjoitusvirhe näkyy käännösaikana
        var provider = configuration[$"{StorageOptions.SectionName}:Provider"]
            ?? StorageOptions.LocalProvider;
        if (provider == StorageOptions.AzureProvider)
            services.AddScoped<IStorageService, AzureBlobStorageService>(); // Osa 2
        else
            services.AddScoped<IStorageService, LocalStorageService>();     // Osa 1 — tämä nyt

        return services;
    }
}
```

**Miksi `configuration["Storage:Provider"]`, ei `env.IsDevelopment()`?**

Ympäristö (kehitys/tuotanto) ei ole sama asia kuin tallennuspaikka. Konfiguraatioarvo on eksplisiittinen — tiedät aina mitä käytetään:

```
appsettings.json           → Storage:Provider = "local"   (kehityksessä)
Azure App Settings (Osa 2) → Storage:Provider = "azure"   (tuotannossa)
```

**6.2** Käynnistä sovellus. Nyt `IStorageService` on rekisteröity, mutta `UploadPhotoUseCase` heittää vielä `NotImplementedException`.

---

## Vaihe 7: Toteuta UploadPhotoUseCase

Avaa `GalleryApi.Application/UseCases/Photos/UploadPhotoUseCase.cs`. Metodi heittää `NotImplementedException`.

**Miksi logiikka on käyttötapauksessa, ei kontrollerissa?**

Kontrollerin ainoa vastuu on HTTP-kerros: vastaanota pyyntö, muunna lomakedata pyyntö-olioksi, kutsu käyttötapausta, muuta tulos HTTP-vastaukseksi. Kontrolleriin ei pidä kirjoittaa:
- validointilogiikkaa (sallitut tiedostotyypit, koko)
- tietokantakutsuja
- tiedoston tallennuslogiikkaa

Nämä kaikki kuuluvat `UploadPhotoUseCase`:en (Application-kerros). Näin logiikka on testattavissa ilman HTTP-kerrosta — voidaan kutsua suoraan yksikkötesteistä.

Katso miten `PhotosController` kutsuu käyttötapausta (`GalleryApi.WebApi/Controllers/PhotosController.cs`):

```csharp
// Kontrolleri on ohut: se vain muuntaa HTTP-pyynnön kutsuksi käyttötapaukselle
var result = await _uploadPhoto.ExecuteAsync(request);
if (!result.IsSuccess)
    return BadRequest(new { Error = result.Error });
return CreatedAtAction(nameof(GetByAlbum), new { albumId }, result.Value);
```

**7.1** Toteuta `ExecuteAsync`:

```csharp
public async Task<Result<PhotoDto>> ExecuteAsync(UploadPhotoRequest request)
{
    // 1. Tarkista että albumi on olemassa
    var album = await _albumRepository.GetByIdAsync(request.AlbumId);
    if (album is null)
        return Result<PhotoDto>.Failure($"Albumia {request.AlbumId} ei löydy.");

    // 2. Validoi tiedostotyyppi
    if (!AllowedContentTypes.Contains(request.ContentType))
        return Result<PhotoDto>.Failure(
            $"Tiedostotyyppi '{request.ContentType}' ei ole sallittu. " +
            $"Sallitut tyypit: {string.Join(", ", AllowedContentTypes)}");

    // 3. Validoi tiedoston koko
    if (request.FileSize > MaxFileSizeBytes)
        return Result<PhotoDto>.Failure(
            $"Tiedosto on liian suuri. Maksimikoko on {MaxFileSizeBytes / (1024 * 1024)} MB.");

    // 4. Lataa tiedosto tallennuspalveluun — kääri try-catchiin
    //    Jos upload epäonnistuu, kantaan ei tallenneta mitään
    string imageUrl;
    try
    {
        imageUrl = await _storageService.UploadAsync(
            request.FileStream, request.FileName, request.ContentType, request.AlbumId);
    }
    catch (Exception ex)
    {
        return Result<PhotoDto>.Failure($"Tiedoston tallennus epäonnistui: {ex.Message}");
    }

    // 5. Tallenna tiedot tietokantaan — vain onnistuneen uploadin jälkeen
    var photo = new Photo
    {
        Id = Guid.NewGuid(),
        AlbumId = request.AlbumId,
        Title = request.Title,
        FileName = request.FileName,
        ImageUrl = imageUrl,
        ContentType = request.ContentType,
        FileSizeBytes = request.FileSize,
        UploadedAt = DateTime.UtcNow
    };
    var saved = await _photoRepository.CreateAsync(photo);

    return Result<PhotoDto>.Success(new PhotoDto(saved.Id, saved.AlbumId, saved.Title,
        saved.ImageUrl, saved.ContentType, saved.FileSizeBytes, saved.UploadedAt));
}
```

Lisää using, jos ei ole jo:
```csharp
using GalleryApi.Application.Common;
using GalleryApi.Domain.Entities;
```

**7.2** Testaa Swaggerissa:
1. `POST /api/albums` → `{ "name": "Testi", "description": "Ensimmäinen albumi" }` — kopioi `id`
2. `POST /api/albums/{id}/photos` — valitse multipart/form-data, anna `title` ja lataa kuvatiedosto
3. Vastaus: `201 Created` ja kuvan tiedot JSON:na
4. Avaa `imageUrl`-arvo suoraan selaimessa — kuvan pitäisi näkyä

---

## Vaihe 8: Toteuta DeletePhotoUseCase

Avaa `GalleryApi.Application/UseCases/Photos/DeletePhotoUseCase.cs`.

**8.1** Toteuta `ExecuteAsync`:

```csharp
public async Task<Result> ExecuteAsync(Guid photoId)
{
    // 1. Hae kuva tietokannasta
    var photo = await _photoRepository.GetByIdAsync(photoId);
    if (photo is null)
        return Result.Failure($"Kuvaa {photoId} ei löydy.");

    // 2. Poista tiedosto tallennuspalvelusta
    //    Käytetään FileName + AlbumId, ei ImageUrl
    //    (Azure Blob Storagessa URL ei vastaa suoraan blob-nimeä)
    await _storageService.DeleteAsync(photo.FileName, photo.AlbumId);

    // 3. Poista tietue tietokannasta
    await _photoRepository.DeleteAsync(photoId);

    return Result.Success();
}
```

Lisää using:
```csharp
using GalleryApi.Application.Common;
```

**8.2** Testaa Swaggerissa:
1. `GET /api/albums/{albumId}/photos` — kopioi jonkin kuvan `id`
2. `DELETE /api/albums/{albumId}/photos/{photoId}` → `204 No Content`
3. Varmista, että tiedosto on poistunut `wwwroot/uploads/`-hakemistosta

---

## Vaihe 9: Testit ja konfiguraation tarkistus

**9.1** Aja testit:

```bash
dotnet test
```

Kaikki testit pitäisi mennä läpi. Tutki testikoodi — huomaa kuinka `Mock<IAlbumRepository>` mahdollistaa yksikkötestauksen ilman oikeaa tietokantaa. Tämä on mahdollista juuri Clean Architecture -rakenteen ansiosta: käyttötapaus ei tiedä mitään tietokannasta, vain rajapinnasta.

**Bonustehtävä:** Kirjoita vastaavat testit `UploadPhotoUseCase`:lle. Testaa ainakin:
- Onnistunut lataus (`result.IsSuccess == true`, `result.Value` sisältää PhotoDto)
- Puuttuva albumi (`result.IsSuccess == false`, `result.Error` sisältää virheviestin)
- Väärä tiedostotyyppi (`result.IsSuccess == false`)
- Liian suuri tiedosto (`result.IsSuccess == false`)

**9.2** Konfiguraation tarkistus — varmista ennen Osaan 2 siirtymistä:

`appsettings.json` ei saa sisältää oikeita avaimia:
```json
"ModerationService": {
  "ApiKey": "",
  "BaseUrl": "https://api.moderation-example.com"
}
```

User Secrets on asetettu. Tarkista se alla olevalla tavalla:

<details>
<summary><strong>Vaihtoehto A: Komentorivi</strong></summary>

```bash
dotnet user-secrets list
# → ModerationService:ApiKey = sk-moderation-local-dev-key
```

</details>

<details>
<summary><strong>Vaihtoehto B: Visual Studio</strong></summary>

Solution Explorerissa: klikkaa hiiren **oikealla** `GalleryApi.WebApi`-projektin päällä → **Manage User Secrets** → `secrets.json` avautuu ja näet asetetut arvot.

</details>

Lisää `.gitignore`:

```gitignore
*.db
*.db-shm
*.db-wal
wwwroot/uploads/
```

---

## Yhteenveto

| Konsepti | Mitä opittiin |
|---|---|
| Kovakoodattu salaisuus | Pysyy git-historiassa ikuisesti, botit löytävät sen nopeasti |
| User Secrets | Kehitysaikainen turvallinen salaisuudenhallinta, ei mene versionhallintaan |
| Options Pattern | Konfiguraatioarvot vahvasti tyypitettynä, injektoitavina |
| IStorageService | Tallennustapa voidaan vaihtaa muuttamatta sovelluslogiikkaa |
| Tiedostovalidointi | MIME-tyyppi ja koko tarkistetaan ennen tallennusta |
| Yksikkötestaus | Käyttötapaukset testattavissa mock-objekteilla ilman tietokantaa |

## Soveltamishaaste (suositus)

Tee pieni muutos, jolla varmistat että osaat soveltaa opittua myös tehtävän ulkopuolella:
1. Lisää `UploadPhotoUseCase`:en uusi sallittu MIME-tyyppi (esim. `image/heic`).
2. Lisää `StorageOptions`:iin uusi asetus `MaxFileSizeMb` ja lue se `IOptions`-mallilla.
3. Muuta validointi käyttämään tätä asetusta kovakoodatun vakion sijaan.
4. Kirjoita vähintään yksi yksikkötesti, joka todistaa uuden asetuksen vaikuttavan logiikkaan.

**Seuraavaksi:** [Osa 2 — Azure-julkaisu](./README-Part2.md)

---

## Palautustarkistuslista

- [ ] Sovellus kääntyy ja käynnistyy virheittä (`dotnet build`, `dotnet run`)
- [ ] Testit menevät läpi (`dotnet test`)
- [ ] `ModerationServiceClient` käyttää `IOptions<ModerationServiceOptions>`
- [ ] Kovakoodattu avain poistettu `Program.cs`:stä
- [ ] User Secret asetettu ja `appsettings.json` sisältää vain tyhjän placeholderin
- [ ] `LocalStorageService` toteutettu
- [ ] `IStorageService` rekisteröity `DependencyInjection.cs`:ssä
- [ ] `UploadPhotoUseCase` toteutettu ja kuvien lataus toimii
- [ ] `DeletePhotoUseCase` toteutettu ja kuvien poisto toimii
- [ ] `gallery.db` ja `wwwroot/uploads/` on `.gitignore`:ssa
- [ ] Vastaukset `questions-part1.md`-tiedostossa

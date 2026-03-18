# Kuvakirjasto-API — Osa 2: Azure-julkaisu

Tässä osassa julkaiset Osa 1:ssä rakennetun sovelluksen Azureen. Opit julkaisemaan sovelluksen **Azure App Serviceen**, hallitsemaan ympäristökohtaista konfiguraatiota **Application Settingsillä**, tallentamaan kuvat **Azure Blob Storageen** ja antamaan sovellukselle oikeudet ilman salasanoja **Managed Identitylla** ja **RBAC**:lla.

## Mitä osaat tämän osan jälkeen?

Kun olet tehnyt tämän osan loppuun, osaat:
- julkaista .NET Web API:n Azure App Serviceen
- erottaa konfiguraatioarvot ja salaisuudet toisistaan Azure-ympäristössä
- käyttää `DefaultAzureCredential`-mallia niin, että sama koodi toimii lokaalisti ja Azuressa
- antaa App Servicelle Blob Storage -oikeudet Managed Identityn ja RBAC:n avulla
- todentaa käytännössä, että tallennus vaihtuu lokaalista Azure Blob Storageen ilman käyttötapausmuutoksia

### Komentojen shell-huomio

Tässä osassa komennot on kirjoitettu **bash-muodossa** (esim. `\` rivinvaihtomerkkinä).  
Jos käytät PowerShelliä:
- voit ajaa komennot yhdellä rivillä, tai
- käyttää PowerShellin rivinjatkomerkkiä `` ` ``.

Selkeyden vuoksi suosittelemme opiskelijoille Git Bashia tai WSL:ää tämän osan komentojen ajamiseen.

---

## Esitietovaatimukset

- [Osa 1 — Lokaali kehitys](./README-Part1.md) täytyy olla tehtynä
- [Azure App Service — teoria](../../Cloud%20technologies/Azure/App-Service.md)
- [Managed Identity — teoria](../../Cloud%20technologies/Azure/Managed-Identity.md)
- Azure-tilaus (opiskelijatili tai maksuton kokeilutili)
- Azure CLI asennettuna ja kirjautuneena:

```bash
az --version
az login
```

---

## Mitä rakennetaan?

```
                    ┌────────────────────────────────────┐
                    │              Azure                  │
                    │                                     │
  Käyttäjä ──HTTP──▶│  App Service                        │
                    │  ┌─────────────────────────┐        │
                    │  │  GalleryApi              │        │
                    │  │  Storage:Provider=azure  │        │
                    │  │  Storage:AccountName=... │        │
                    │  └──────────┬──────────────┘        │
                    │             │ Managed Identity       │
                    │             ▼                        │
                    │  ┌──────────────────┐                │
                    │  │  Azure Blob      │◀── Kuvat       │
                    │  │  Storage         │                │
                    │  └──────────────────┘                │
                    └────────────────────────────────────┘
```

---

## Resurssien nimet — valitse etukäteen

Päätä nimesi ennen kuin aloitat. Käytä samoja nimiä koko tehtävän ajan. Korvaa `<etunimi>` omalla etunimelläsi (esim. `matti`):

| Resurssi | Nimi | Huom |
|---|---|---|
| Resource Group | `rg-gallery-<etunimi>` | |
| App Service Plan | `plan-gallery-<etunimi>` | |
| App Service | `gallery-api-<etunimi>` | **Maailmanlaajuisesti uniikki** |
| Storage Account | `stgallery<etunimi>` | Vain pienet kirjaimet + numerot, max 24 merkkiä |

> Jos käytät CLI:tä, aseta myös nämä muuttujat kerralla:
> ```bash
> RESOURCE_GROUP="rg-gallery-<etunimi>"
> LOCATION="swedencentral"
> APP_NAME="gallery-api-<etunimi>"
> STORAGE_ACCOUNT="stgallery<etunimi>"
> APP_SERVICE_PLAN="plan-gallery-<etunimi>"
> ```

---

## Vaihe 1: Luo Azure-resurssit

Jokaiselle vaiheelle on ohjeet sekä **Azure Portalin** että **Azure CLI:n** kautta. Suosittelemme tekemään vähintään kerran Portalin kautta, jotta resurssit tulevat tutuksi visuaalisesti.

### 1.1 Resource Group

Resource Group on "kansio" johon kaikki tämän tehtävän resurssit kuuluvat. Tehtävän jälkeen voit siivota kaiken poistamalla koko Resource Groupin.

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa [portal.azure.com](https://portal.azure.com) ja kirjaudu sisään
2. Etsi yläpalkin hakukentästä **"Resource groups"** ja avaa se
3. Klikkaa **"+ Create"**
4. Täytä kentät:
   - **Subscription**: valitse oma tilauksesi
   - **Resource group**: `rg-gallery-<etunimi>`
   - **Region**: `Sweden Central`
5. Klikkaa **"Review + create"** → **"Create"**
6. Odota kunnes ilmoitus "Resource group created" ilmestyy oikeaan yläkulmaan

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

</details>

---

### 1.2 App Service Plan

App Service Plan määrittää virtuaalikoneen koon ja hintatason — kaikki samaan Planiin liitetyt App Servicet jakavat sen resurssit.

<details>
<summary><strong>Azure Portal</strong></summary>

Tee Azuressa seuraavassa kohdassa, jossa luot App Servicen

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku F1 \
  --is-linux
```

</details>

---

### 1.3 App Service (Web App)

App Service on varsinainen isäntäpalvelu jolle sovelluksesi julkaistaan.

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Hae **"App Services"** → klikkaa **"+ Create"** → valitse **"Web App"**
2. Täytä **Basics**-välilehti:
   - **Resource Group**: `rg-gallery-<etunimi>`
   - **Name**: `gallery-api-<etunimi>` *(tämä muodostaa URL:n: `gallery-api-<etunimi>.azurewebsites.net`)*
   - **Publish**: `Code`
   - **Runtime stack**: `.NET 8 (LTS)`
   - **Operating System**: `Linux`
   - **Region**: `Sweden Central`
   - **Pricing plan**: Luo uusi pricing plan, muista valita F1 free tier
3. Klikkaa **"Review + create"** → **"Create"**
4. Odota käyttöönottoa (~1 min). Avaa resurssi kun se on valmis.

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:8.0"
```

</details>

---

### 1.4 Storage Account

Storage Account on "tili" johon Blob Storage -kontit kuuluvat. Nimi on maailmanlaajuisesti uniikki ja näkyy URL:ssa.

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Hae **"Storage accounts"** → klikkaa **"+ Create"**
2. Täytä **Basics**-välilehti:
   - **Resource Group**: valitse `rg-gallery-<etunimi>`
   - **Storage account name**: kirjoita `stgallery<etunimi>` *(esim. `stgallerymatti` — vain pienet kirjaimet ja numerot, max 24 merkkiä)*
   - **Region**: valitse `(Europe) Sweden Central`
   - **Preferred storage type**: jätä tyhjäksi *(vapaaehtoinen, ei vaikuta toimintaan)*
   - **Performance**: jätä `Standard` valittuna
   - **Redundancy**: vaihda `Geo-redundant storage (GRS)` → **`Locally-redundant storage (LRS)`** *(halvin, riittää tehtävään)*
3. **Älä klikkaa vielä "Review + create"** — siirry **"Advanced"**-välilehdelle
4. **Advanced**-välilehdellä etsi **Security**-osio:
   - **Allow enabling anonymous access on individual containers**: rastita tämä **päälle** *(oletuksena pois päältä)*
   - Muut asetukset voit jättää oletusarvoihin

   > **Miksi tämä asetus tarvitaan?** Oletuksena Azure estää kaiken anonyymin pääsyn Blob Storageen. Tässä tehtävässä haluamme, että ladattujen kuvien URL:t toimivat suoraan selaimessa ilman kirjautumista (esim. `https://stgallery....blob.core.windows.net/photos/albumId/photo.jpg`). Tämä asetus ei vielä itsessään tee mitään julkiseksi — se vain **sallii** yksittäisten containerien pääsytason asettamisen erikseen kohdassa 1.5. Ilman tätä container-tason anonyymin pääsyn valitseminen ei ole mahdollista.
   >
   > **Miksi luontivaiheessa eikä jälkikäteen?** Monissa Azure-tilauksissa (erityisesti opiskelijatileissä ja organisaation hallitsemissa tilauksissa) tämän asetuksen muuttaminen jälkikäteen on estetty policyllä — Configuration-sivulla asetus näkyy harmaana ja lukittuna. Kun se valitaan jo luontivaiheessa, ongelma vältetään kokonaan.

5. Klikkaa **"Review + create"** → **"Create"**

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2 \
  --allow-blob-public-access true
```

</details>

---

### 1.5 Blob Container

Container on "kansio" Storage Accountin sisällä. Kuvat tallennetaan `photos`-containeriin.

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa juuri luomasi Storage Account
2. Vasemmasta valikosta etsi **"Containers"** (kohdassa *Data storage*) → klikkaa se
3. Klikkaa **"+ Container"**
4. Täytä:
   - **Name**: `photos`
   - **Anonymous access level**: valitse `Blob (anonymous read access for blobs only)`
5. Klikkaa **"Create"**

> **Miksi "Blob"-taso?** Se tarkoittaa, että kuvien URL:t ovat julkisesti luettavia selaimessa — voit avata kuvan URL:n suoraan. Mutta kirjoitusoikeus vaatii silti tunnistautumisen (Managed Identity, jonka lisäät Vaiheessa 5). Oikeassa tuotantoympäristössä kuvat pidetään usein yksityisinä ja jaetaan allekirjoitetuilla URL-osoitteilla.

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az storage container create \
  --name "photos" \
  --account-name $STORAGE_ACCOUNT \
  --public-access blob
```

</details>

---

## Vaihe 2: Toteuta AzureBlobStorageService

Osassa 1 toteutit `LocalStorageService`-luokan, joka tallentaa kuvat palvelimen levylle. Nyt toteutat toisen toteutuksen samasta `IStorageService`-rajapinnasta — tällä kertaa Azure Blob Storageen. Käyttötapausluokat (`UploadPhotoUseCase` jne.) eivät muutu lainkaan.

### 2.1 Lisää NuGet-paketit

Varmista, että olet solution-juuressa (`StarterCode/GalleryApi/`):

```bash
cd GalleryApi.Infrastructure
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Identity
cd ..
```

| Paketti | Tarkoitus |
|---|---|
| `Azure.Storage.Blobs` | Blob Storagen SDK — tiedostojen lataus, poisto ja URL:n muodostus |
| `Azure.Identity` | `DefaultAzureCredential` — tunnistautuminen Azureen ilman avaimia |

### 2.2 Ymmärrä Blob Storagen rakenne

Ennen koodaamista on hyvä ymmärtää, miten Blob Storage vastaa tuttua tiedostojärjestelmää:

```
Kehityskone (LocalStorageService):        Azure (AzureBlobStorageService):
──────────────────────────────────        ──────────────────────────────────
wwwroot/                                  Storage Account  (stgallerymatti)
  uploads/                                  └── Container  (photos)
    albumId/                                      ├── albumId/photo1.jpg
      photo1.jpg                                  └── albumId/photo2.jpg

URL: /uploads/albumId/photo.jpg           URL: https://stgallerymatti.blob.core.windows.net/photos/albumId/photo.jpg
```

SDK:n luokkahierarkia noudattaa samaa rakennetta:

```
BlobServiceClient          ← yhteys Storage Accountiin
  └── BlobContainerClient  ← yhteys yhteen containeriin ("photos")
        └── BlobClient     ← yhteys yhteen tiedostoon ("albumId/photo.jpg")
```

### 2.3 Toteuta luokka

Avaa `GalleryApi.Infrastructure/Storage/AzureBlobStorageService.cs`. Tiedostossa on TODO-kommentit. Käydään toteutus läpi osa kerrallaan.

**Kenttä ja konstruktori — yhteyden muodostaminen:**

```csharp
// Kenttä johon tallennetaan viite photos-containeriin — käytetään Upload- ja Delete-metodeissa
private readonly BlobContainerClient _containerClient;

public AzureBlobStorageService(IOptions<StorageOptions> options)
{
    // Luetaan Storage Accountin nimi ja containerin nimi konfiguraatiosta (StorageOptions)
    var accountName = options.Value.AccountName;   // esim. "stgallerymatti"
    var containerName = options.Value.ContainerName; // esim. "photos"

    // Muodostetaan yhteys Storage Accountiin
    // DefaultAzureCredential hoitaa tunnistautumisen automaattisesti (ks. selitys alla)
    var serviceClient = new BlobServiceClient(
        new Uri($"https://{accountName}.blob.core.windows.net"),
        new DefaultAzureCredential());

    // Haetaan viite haluttuun containeriin — ei vielä tee verkko­kutsua
    _containerClient = serviceClient.GetBlobContainerClient(containerName);
}
```

> **Mikä on `DefaultAzureCredential`?**
>
> Sen sijaan että koodaisit yhden kiinteän tunnistautumistavan, `DefaultAzureCredential` kokeilee automaattisesti useita vaihtoehtoja järjestyksessä:
>
> | # | Tunnistautumistapa | Milloin aktivoituu |
> |---|---|---|
> | 1 | Managed Identity | Kun sovellus ajaa Azuressa *(aktivoidaan Vaiheessa 5)* |
> | 2 | Azure CLI (`az login`) | Kun kehität lokaalisti |
> | 3 | Visual Studio -kirjautuminen | Jos olet kirjautuneena VS:ssä |
>
> Ensimmäinen toimiva vaihtoehto valitaan automaattisesti. **Tärkein hyöty:** sama koodi toimii sekä kehityskoneella että Azuressa — ilman yhtään if-lausetta tai ympäristötarkistusta.

**UploadAsync — tiedoston lataus:**

```csharp
public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId)
{
    // albumId toimii "kansiorakenteena" blob-nimessä → "3fa85f64.../photo.jpg"
    var blobName = $"{albumId}/{fileName}";

    // Haetaan viite yksittäiseen blobiin containerin sisällä
    var blobClient = _containerClient.GetBlobClient(blobName);

    // Ladataan tiedosto Blob Storageen ja asetetaan Content-Type (esim. "image/jpeg"),
    // jotta selain osaa näyttää kuvan oikein suoraan URL:sta
    await blobClient.UploadAsync(
        fileStream,
        new BlobHttpHeaders { ContentType = contentType });

    // Palautetaan blobin julkinen URL
    // esim. https://stgallerymatti.blob.core.windows.net/photos/3fa85f64.../photo.jpg
    return blobClient.Uri.ToString();
}
```

**DeleteAsync — tiedoston poisto:**

```csharp
public async Task DeleteAsync(string fileName, Guid albumId)
{
    // Sama nimeämislogiikka kuin UploadAsync:ssa → "albumId/fileName"
    var blobName = $"{albumId}/{fileName}";
    var blobClient = _containerClient.GetBlobClient(blobName);

    // DeleteIfExistsAsync ei heitä poikkeusta jos blobi ei ole olemassa
    // → turvallisempi kuin DeleteAsync, joka heittäisi 404-virheen
    await blobClient.DeleteIfExistsAsync();
}
```

### 2.4 Koko tiedosto

Kopioi valmis toteutus tiedostoon `GalleryApi.Infrastructure/Storage/AzureBlobStorageService.cs`:

<details>
<summary><strong>▶ AzureBlobStorageService.cs — valmis koodi</strong></summary>

```csharp
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GalleryApi.Domain.Interfaces;
using GalleryApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace GalleryApi.Infrastructure.Storage;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(IOptions<StorageOptions> options)
    {
        var accountName = options.Value.AccountName;
        var containerName = options.Value.ContainerName;

        var serviceClient = new BlobServiceClient(
            new Uri($"https://{accountName}.blob.core.windows.net"),
            new DefaultAzureCredential());

        _containerClient = serviceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, Guid albumId)
    {
        var blobName = $"{albumId}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            fileStream,
            new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileName, Guid albumId)
    {
        var blobName = $"{albumId}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }
}
```

</details>

### 2.5 Miksi käyttötapaukset eivät muutu?

Vertaa mitä tapahtuu `UploadPhotoUseCase`-luokan näkökulmasta:

```
Osa 1 (lokaali):    _storageService.UploadAsync(...)  →  LocalStorageService       →  wwwroot/uploads/...
Osa 2 (Azure):      _storageService.UploadAsync(...)  →  AzureBlobStorageService   →  Blob Storage URL
                    ↑                                    ↑
                    Sama kutsu                           Eri toteutus — valitaan konfiguraation perusteella
```

`UploadPhotoUseCase` kutsuu aina `IStorageService.UploadAsync(...)`. Se ei tiedä eikä välitä, tallennetaanko kuva levylle vai pilveen. Tämä on juuri Clean Architecture -rakenteen hyöty: toteutus vaihtuu ilman käyttötapausmuutoksia.

---

## Vaihe 3: Julkaisu App Serviceen

Julkaise sovellus Vaiheessa 1 luotuun App Serviceen. Valitse sinulle sopivin tapa:

Muista Program.cs käydä laittamassa swagger toimimaan myös azuressa. Eli katso, että swagger komennot eivät ole

if (app.Environment.IsDevelopment()) <-- Tuon sisällä

app.UseSwagger(); 
app.UseSwaggerUI();

<details>
<summary><strong>▶ Visual Studio (Publish-toiminto)</strong></summary>

**3.1** Klikkaa **Solution Explorerissa** `GalleryApi.WebApi`-projektia hiiren oikealla → **"Publish..."**

**3.2** Valitse julkaisukohde:
1. Valitse **"Azure"** → **"Next"**
2. Valitse **"Azure App Service (Linux)"** → **"Next"**
3. Kirjaudu tarvittaessa Azure-tilillesi
4. Valitse listasta Vaiheessa 1 luomasi App Service (`gallery-api-<etunimi>`) → **"Finish"**

**3.3** Julkaise:
1. Visual Studio luo Publish-profiilin. Klikkaa **"Publish"**-painiketta
2. Odota kunnes Output-ikkunaan tulee `Publish Succeeded`
3. Visual Studio avaa automaattisesti selaimen sovelluksen URL:iin

**3.4** Lisää `/swagger` URL:n loppuun selaimessa.

> **Vinkki uudelleenjulkaisuun:** Kun teet muutoksia koodiin, klikkaa Publish-välilehdellä uudelleen **"Publish"** — Visual Studio muistaa profiilin eikä kysy kohdetta uudestaan.

</details>

<details>
<summary><strong>▶ Azure CLI (komentorivi)</strong></summary>

**3.1** Siirry `GalleryApi.WebApi`-hakemistoon, julkaise ja pakkaa:

Jos julkaisu ei mene läpi, käy lisäämässä alla oleva 

```bash
cd GalleryApi.WebApi
dotnet publish -c Release -o ./publish

# Windows PowerShell
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force

# Linux / macOS
# zip -r deploy.zip ./publish/*
```

**3.2** Lähetä App Serviceen:

```bash
az webapp deployment source config-zip \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip
```

**3.3** Avaa sovellus selaimessa:

```bash
az webapp browse --name $APP_NAME --resource-group $RESOURCE_GROUP
```

**3.4** Lisää `/swagger` URL:n loppuun selaimessa.

</details>

Swagger-sivun pitäisi näkyä, mutta kuvien lataus epäonnistuu — Storage-asetukset puuttuvat vielä. Korjataan ne seuraavaksi.

---

## Vaihe 4: Application Settings — ympäristömuuttujat

### 4.1 Miksi Application Settings?

Kehityskoneella `appsettings.json` on fyysisesti läsnä tiedostojärjestelmässä. Azure App Servicessä sovellus ajaa virtuaalikoneella, johon sinulla ei ole suoraa pääsyä — et voi mennä muokkaamaan `appsettings.json`-tiedostoa palvelimella.

**Application Settings** ratkaisee tämän: se on App Servicen web-käyttöliittymä (tai CLI-komento), jonka kautta syötetään konfiguraatioarvoja sovellukselle. ASP.NET Core lukee ne automaattisesti osana konfiguraatioputkea — täsmälleen kuten ympäristömuuttujat.

```
Kehityskone:                        Azure App Service:
────────────────────────────        ────────────────────────────────────
appsettings.json          →         appsettings.json (ei voi muokata!)
User Secrets              →         Application Settings  ← tämä on vastine
  ModerationService:ApiKey            Storage__Provider = "azure"
  → Vain sinun koneellasi             → Tallennetaan App Serviceen

Molemmat ylikirjoittavat appsettings.json:n arvot automaattisesti.
```

> **Application Settings vs. Key Vault — milloin kumpi?**
> Application Settings sopivat arvoihin, jotka eivät ole salaisuuksia — arvot näkyvät Portaalissa selväkielisenä.
>
> | Arvo | Paikka |
> |---|---|
> | `Storage:Provider = "azure"` | Application Settings ✓ — ei salainen |
> | `Storage:AccountName = "stgallery..."` | Application Settings ✓ — julkinen tieto |
> | `ModerationService:ApiKey = "sk-..."` | **Key Vault** ✓ (Osa 3) — oikea salaisuus |

### 4.2 Aseta asetukset

Sovellus tarvitsee kolme arvoa tietääkseen, mihin Blob Storageen kuvat tallennetaan:

| Name | Value | Mitä ohjaa |
|---|---|---|
| `Storage__Provider` | `azure` | Valitsee `AzureBlobStorageService`-toteutuksen |
| `Storage__AccountName` | `stgallery<etunimi>` | Storage Accountin nimi (URL:n osa) |
| `Storage__ContainerName` | `photos` | Containerin nimi Accountin sisällä |

> **Miksi kaksi alaviivaa (`__`)?** Application Settingsin avainnimet eivät voi sisältää kaksoispistettä (`:`). ASP.NET Core tunnistaa kaksi alaviivaa hierarkkiseksi erottimeksi:
> ```
> Application Settings:   Storage__Provider = "azure"
>                                ↕ sama asia
> ASP.NET Core:           Storage:Provider  = "azure"
>                                ↕ sama asia
> StorageOptions.cs:      Provider { get; set; }
> ```

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa App Service (`gallery-api-<etunimi>`)
2. Vasemmasta valikosta klikkaa **"Environment variables"** (tai vanhemmassa portaalissa: *Configuration* → *Application settings* -välilehti)
3. Klikkaa **"+ Add"** ja lisää yllä olevan taulukon kolme arvoa (muista kaksi alaviivaa `__`)
4. Klikkaa **"Apply"** → vahvista **"Confirm"**
5. Sovellus käynnistyy automaattisesti uudelleen

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "Storage__Provider=azure" \
    "Storage__AccountName=$STORAGE_ACCOUNT" \
    "Storage__ContainerName=photos"
```

Tarkista:
```bash
az webapp config appsettings list \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --output table
```

Käynnistä uudelleen:
```bash
az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP
```

</details>

### 4.3 Testaa — ja odota virhettä

Kokeile Swaggerissa kuvan latausta (`POST /api/albums/{id}/photos`). Todennäköisesti näet virheen:

```
AuthenticationFailedException: DefaultAzureCredential failed to retrieve a token...
```

**Tämä on odotettua.** Sovellus löytää nyt Storage Accountin osoitteen konfiguraatiosta, mutta `DefaultAzureCredential` ei löydä toimivaa tunnistautumistapaa — App Servicella ei ole vielä identiteettiä. Korjataan se seuraavaksi. Jos saat uploadattua kuvan, niin silloin azuressa env variablet ei ole oikein asetettu!

---

## Vaihe 5: Aktivoi Managed Identity

Vaiheessa 4 näit `AuthenticationFailedException`-virheen. Sovellus tietää *minne* yhdistää (Storage Account), mutta ei pysty todistamaan *kuka se on*. Tässä vaiheessa annetaan sovellukselle identiteetti.

### Mikä on Managed Identity ja miksi se ratkaisee ongelman?

Kuvittele tilanne ilman Managed Identityä: sovelluksesi haluaa kirjoittaa tiedoston Blob Storageen. Azure Blob Storage vaatii tunnistautumisen — "kuka sinä olet?". Perinteinen ratkaisu olisi antaa sovellukselle **storage account key** — pitkä merkkijono, joka toimii kuin salasana. Mutta nyt olet takaisin lähtöpisteessä:

```
ONGELMA:
App Service haluaa kirjoittaa Blob Storageen
  → Tarvitsee storage account keyn
  → Avain täytyy tallentaa johonkin (Application Settings? → näkyy portaalissa selväkielisenä!)
  → Jos avain vuotaa, kaikki Blob Storage -data on vaarassa
```

**Managed Identity** ratkaisee tämän kokonaan toisella tavalla. Azure antaa App Service -instanssillesi automaattisesti **identiteetin** — kuin passin. Tämä identiteetti on täysin Azuren hallitsema: ei avaimia, ei salasanoja, ei vanhentumisia joita täytyy hallita manuaalisesti.

```
MANAGED IDENTITYN KANSSA:
App Service haluaa kirjoittaa Blob Storageen
  → Azure todistaa: "Tämä on gallery-api-matti -sovellus, luotan siihen"
  → Blob Storage tarkistaa: "Onko gallery-api-matilla lupa kirjoittaa?"
  → RBAC-roolimääritys vastaa: "Kyllä, sillä on Storage Blob Data Contributor -rooli"
  → Pääsy myönnetään — ilman yhtään avainta tai salasanaa
```

Managed Identity on **System-assigned** tai **User-assigned**. Käytämme System-assigned -versiota, joka sidotaan suoraan App Service -instanssiin ja poistetaan automaattisesti kun App Service poistetaan.

**5.1** Aktivoi System-assigned Managed Identity:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa App Service (`gallery-api-<etunimi>`)
2. Vasemmasta valikosta etsi **"Identity"** (kohdassa *Settings*)
3. Olet **"System assigned"** -välilehdellä — vaihda **Status**: `Off` → `On`
4. Klikkaa **"Save"** → vahvista **"Yes"**

Azure luo automaattisesti identiteetin App Servicelle. Sivulle ilmestyy **Object (principal) ID** — kopioi se talteen, tarvitset sitä Vaiheessa 6.

```
Object (principal) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx  ← Kopioi tämä
```

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP
```

Tallenna `principalId` muuttujaan jatkokäyttöä varten:

```bash
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

echo "Principal ID: $PRINCIPAL_ID"
```

</details>

---

## Vaihe 6: Anna RBAC-oikeudet Blob Storageen

Vaiheessa 5 sovellus sai identiteetin (Managed Identity), mutta identiteetti ei vielä tarkoita oikeuksia. Azure noudattaa periaatetta: **kaikki on oletuksena kielletty**. Tässä vaiheessa annetaan identiteetille nimenomainen lupa lukea ja kirjoittaa Blob Storageen.

### Mikä on RBAC?

**RBAC** (Role-Based Access Control) on Azuren tapa hallita oikeuksia. Perinteinen "pääsynhallinta" on binaarinen: joko sinulla on pääsy tai ei. RBAC on tarkempi: annat *tietylle identiteetille* *tietyn roolin* *tiettyyn resurssiin*.

Jokainen roolimääritys koostuu kolmesta osasta:

```
Kuka?                        Mikä rooli?                     Mihin?
─────────────────────        ──────────────────────────      ──────────────────────────
App Service                  Storage Blob Data Contributor   Storage Account "stgallery..."
(Managed Identity,           (luku + kirjoitus + poisto)     (EI koko Azurea, vain tämä
 principalId: uuid)          — ei hallintaoikeuksia!          yksi storage account)
```

**Miksi ei vain anneta "kaikkia oikeuksia"?** Tietoturvan periaate: **Least Privilege** (minimaaliset oikeudet). Jos sovellus tarvitsee vain kirjoittaa Blob Storageen, se ei tarvitse oikeutta luoda/poistaa Storage Accounteja, muuttaa verkkoasetuksia tai lukea muita Azure-resursseja. Mitä vähemmän oikeuksia, sitä pienempi vahinko mahdollisen tietomurron sattuessa.

Tässä tehtävässä käytetty `Storage Blob Data Contributor` -rooli antaa:
- ✓ Blob-tiedostojen luku
- ✓ Blob-tiedostojen kirjoitus
- ✓ Blob-tiedostojen poisto
- ✗ Storage Account -asetusten muuttaminen
- ✗ Containerien luominen tai poistaminen

**6.1** Anna `Storage Blob Data Contributor` -rooli:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

Roolimääritys tehdään Storage Accountin kautta — sieltä hallitaan kuka saa pääsyn sen sisältöön.

1. Avaa Storage Account (`stgallery<etunimi>`)
2. Vasemmasta valikosta klikkaa **"Access control (IAM)"**
3. Klikkaa **"+ Add"** → **"Add role assignment"**
4. **Role**-välilehdellä: hae hakukentästä `Storage Blob Data Contributor` → valitse se → **"Next"**
5. **Members**-välilehdellä:
   - **Assign access to**: valitse `Managed identity`
   - Klikkaa **"+ Select members"**
   - Avautuvassa sivupalkissa: **Managed identity** -alasvetovalikosta valitse `App Service`
   - Listasta löydät `gallery-api-<etunimi>` — valitse se → **"Select"**
6. Klikkaa **"Review + assign"** → **"Review + assign"** uudelleen

Roolimääritys ilmestyy **"Role assignments"** -välilehdelle. Voit tarkistaa sen klikkaamalla **"Role assignments"** -välilehteä ja etsimällä `gallery-api-<etunimi>`.

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
STORAGE_ID=$(az storage account show \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query id \
  --output tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Contributor" \
  --scope $STORAGE_ID
```

</details>

**6.2** Odota 1-2 minuuttia. RBAC-muutosten astuminen voimaan vie hetken.

Jos lataus ei toimi odotuksen jälkeen:
- tarkista Portalissa Storage Account → IAM → Role assignments: näkyykö `gallery-api-<etunimi>` listassa?
- tarkista että App Servicen Identity on On (Vaihe 5)
- käynnistä App Service uudelleen ja testaa uudelleen

---

## Vaihe 7: Testaa kuvan lataus Azureen

**7.1** Käynnistä App Service uudelleen varmistaaksesi tuoreet konfiguraatioarvot:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa App Service (`gallery-api-<etunimi>`)
2. Klikkaa yläpalkin **"Restart"**-painiketta → vahvista **"Yes"**

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP
```

</details>

**7.2** Avaa Swagger (`https://<APP_NAME>.azurewebsites.net/swagger`) ja testaa:
1. Luo albumi: `POST /api/albums`
2. Lataa kuva: `POST /api/albums/{id}/photos` — valitse kuvatiedosto
3. Vastauksen `imageUrl` on nyt Azure Blob Storage -URL:
   `https://stgallery....blob.core.windows.net/photos/albumId/photo.jpg`
4. Avaa URL selaimessa — kuvan pitäisi näkyä

**7.3** Tarkista, että kuva tallentui Azure Blob Storageen:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa Storage Account → **"Containers"** → klikkaa `photos`-containeria
2. Näet listan tallennetuista blobeista: `albumId/photo.jpg`-muodossa
3. Klikkaa yksittäistä blobia → voit avata sen suoraan URL:sta tai ladata sen

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az storage blob list \
  --account-name $STORAGE_ACCOUNT \
  --container-name photos \
  --auth-mode login \
  --output table
```

</details>

**7.4** Vertaa `LocalStorageService` vs. `AzureBlobStorageService`:

| | LocalStorageService | AzureBlobStorageService |
|---|---|---|
| Tiedoston sijainti | `wwwroot/uploads/albumId/` | Azure Blob Container |
| Palautettu URL | `/uploads/albumId/photo.jpg` | `https://...blob.core.windows.net/...` |
| Toimii ilman nettiä | Kyllä | Ei |
| Skaalautuu | Ei (levytila rajoittaa) | Kyllä |
| Backup | Ei automaattisesti | Kyllä (redundanssi) |

---

## Vaihe 8: Konfiguraation yhteenveto

Tarkista, miten konfiguraatio toimii nyt eri ympäristöissä:

```
Kehityskone:
  appsettings.json        → Storage:Provider = "local"
  User Secrets            → ModerationService:ApiKey = "sk-...dev..."
  Tulos: käyttää LocalStorageService

Azure App Service:
  appsettings.json        → Storage:Provider = "local"   (oletusarvo)
  Application Settings    → Storage:Provider = "azure"   (ylikirjoittaa!)
                          → Storage:AccountName = "stgallery..."
  Tulos: käyttää AzureBlobStorageService
```

Huomaa: `ModerationService:ApiKey` puuttuu vielä App Servicesta — se lisätään Key Vaultista Osassa 3.

---



## Yhteenveto

| Konsepti | Mitä opittiin |
|---|---|
| Azure App Service | Sovelluksen julkaisu ja hallinta pilvessä |
| Application Settings | Ympäristökohtainen konfiguraatio (ei salaisuuksille) |
| `Storage__Provider` (kaksi alaviivaa) | ASP.NET Core konfiguraatiohierarkia App Settingsissa |
| DefaultAzureCredential | Sama koodi toimii lokaalisti ja Azuressa |
| Managed Identity | Sovellus kirjautuu Azure-palveluihin ilman avaimia |
| RBAC | Tarkat, minimaalisen oikeat oikeudet oikeaan resurssiin |
| AzureBlobStorageService | IStorageService-toteutus pilvessä |

## Soveltamishaaste (suositus)

Kokeile yhtä muutosta, joka pakottaa sinut soveltamaan opittua:
1. Luo toinen Blob-container (esim. `avatars`).
2. Lisää konfiguraatioon `Storage:ContainerName` uusi arvo ympäristökohtaisesti.
3. Varmista, että sovellus tallentaa kuvat oikeaan containeriin ilman koodimuutoksia käyttötapauksiin.
4. Dokumentoi lyhyesti, mikä muuttui ja mikä pysyi samana.

**Seuraavaksi:** [Osa 3 — Key Vault ja IaC](./README-Part3.md)

---

## Palautustarkistuslista

- [ ] Azure-resurssit luotu (Resource Group, App Service Plan, App Service, Storage Account)
- [ ] `AzureBlobStorageService` toteutettu
- [ ] Sovellus julkaistu Azure App Serviceen ja Swagger näkyy
- [ ] Application Settings asetettu (`Storage__Provider`, `Storage__AccountName`, `Storage__ContainerName`)
- [ ] Managed Identity aktivoitu App Servicessa
- [ ] RBAC-roolimääritys (`Storage Blob Data Contributor`) tehty Storage Accountiin
- [ ] Kuvan lataus toimii — `imageUrl` on Azure Blob -URL
- [ ] Vastaukset `questions-part2.md`-tiedostossa

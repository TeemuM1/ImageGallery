# Kuvakirjasto-API — Osa 3: Key Vault ja Infrastructure as Code

Tässä osassa siirrät salaisuuden (`ModerationService:ApiKey`) Azure Key Vaultiin — turvallisempaan paikkaan kuin Application Settings. Opit myös automatisoimaan koko infrastruktuurin **Bicep IaC** -templateilla niin, että koko Osan 2 ja 3 työ on toistettavissa yhdellä komennolla.

## Mitä osaat tämän osan jälkeen?

Kun olet tehnyt tämän osan loppuun, osaat:
- erottaa mitä kannattaa pitää Application Settingsissä ja mitä Key Vaultissa
- antaa App Servicelle rajatun lukuoikeuden Key Vaultiin RBAC-roolilla
- lisätä Key Vaultin osaksi ASP.NET Coren konfiguraatioputkea
- rakentaa saman infran manuaalisesti ja Bicepillä sekä vertailla lähestymistapoja
- tehdä toistettavan IaC-deployn turvallisesti ilman että salaisuudet päätyvät Git-historiaan

### Komentojen shell-huomio

Tässä osassa komennot on kirjoitettu pääosin **bash-muodossa**.  
Jos käytät PowerShelliä, aja komennot yhdellä rivillä tai käytä rivinjatkomerkkiä `` ` ``.  
Kohdassa 6.4 on mukana erikseen myös PowerShell-versio.

---

## Esitietovaatimukset

- [Osa 2 — Azure-julkaisu](./README-Part2.md) täytyy olla tehtynä
- [Managed Identity — teoria](../../Cloud%20technologies/Azure/Managed-Identity.md)
- Samat muuttujat kuin Osassa 2:

```bash
RESOURCE_GROUP="rg-gallery-<etunimi>"
APP_NAME="gallery-api-<etunimi>"
STORAGE_ACCOUNT="stgallery<etunimi>"
KEY_VAULT_NAME="kv-gallery-<etunimi>"    # Uusi muuttuja — nimi oltava uniikki
LOCATION="swedencentral"
```

---

## Osa A: Key Vault

### Mikä on Azure Key Vault?

**Azure Key Vault** on Azuren hallittu palvelu salaisuuksien, sertifikaattien ja kryptografisten avainten säilytykseen. Se on kuin turvallinen holvi, jolla on oma pääsynhallinta, täydellinen käyttöhistoria ja automaattinen salaus.

### Miksi Key Vault, ei Application Settings?

Osassa 2 tallensit Storage-asetukset Application Settingsiin. Se toimi koska ne ovat *konfiguraatioarvoja* — ei haittaa vaikka ne näkyisivät portaalissa. Mutta `ModerationService:ApiKey` on oikea salaisuus: jos se vuotaa, ulkopuolinen voi käyttää moderointipalvelua nimissäsi.

```
Application Settings:
  ✓ Storage:Provider = "azure"          ← Konfiguraatioarvo, ei salaisuus
  ✓ Storage:AccountName = "stgallery..."← Julkinen tieto, ei haittaa
  ✗ ModerationService:ApiKey = "sk-..." ← SALAISUUS — näkyy portaalissa selväkielisenä
                                           kaikille joilla on Azure-portaalin pääsy!

Key Vault:
  ✓ ModerationService--ApiKey          ← Salattu, versioitu, auditoitu, pääsy RBAC:lla rajattu
```

Key Vault lisää Application Settingsiin verrattuna:
- **Salaus lepotilassa** — arvo salataan automaattisesti, ei näy portaalissa selväkielisenä
- **Versiohistoria** — jokainen päivitys luo uuden version, vanhat säilyvät (palautus mahdollista)
- **Auditointi** — Key Vault kirjaa lokiin *kuka* luki salaisuuden ja *milloin* (Azure Monitor)
- **RBAC-pääsynhallinta** — voidaan antaa tarkat lukuoikeudet juuri niille identiteeteille jotka tarvitsevat
- **Rotaatio** — salaisuuden arvon voi vaihtaa Key Vaultissa ilman koodimuutosta; sovellus hakee uuden arvon automaattisesti seuraavan käynnistyksen yhteydessä

---

### Vaihe 1: Luo Key Vault

**1.1** Luo Key Vault RBAC-pohjaisella oikeuksien hallinnalla:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Hae yläpalkin hakukentästä **"Key vaults"** → klikkaa **"+ Create"**
2. Täytä **Basics**-välilehti:
   - **Resource Group**: valitse `rg-gallery-<etunimi>`
   - **Key vault name**: `kv-gallery-<etunimi>` *(nimen on oltava maailmanlaajuisesti uniikki)*
   - **Region**: `Sweden Central`
   - **Pricing tier**: `Standard`
3. Siirry **Access configuration** -välilehdelle:
   - **Permission model**: valitse **`Azure role-based access control (recommended)`**
4. Klikkaa **"Review + create"** → **"Create"**
5. Odota kunnes resurssi on valmis ja avaa se

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true
```

</details>

`--enable-rbac-authorization true` (tai portaalissa *Azure role-based access control*) tarkoittaa, että oikeudet hallitaan RBAC-rooleilla (kuten Blob Storagessa) — ei vanhemmalla Access Policy -mallilla. RBAC on suositeltu tapa.

> **Tärkeää:** RBAC-pohjaisessa Key Vaultissa luojalla ei automaattisesti ole oikeutta kirjoittaa salaisuuksia. Ennen Vaihetta 1.2 sinun täytyy antaa itsellesi `Key Vault Secrets Officer` -rooli. Tee se Key Vaultin **Access control (IAM)** -sivulta samalla tavalla kuin Osassa 2 annettiin rooleja (+ Add → Add role assignment → hae "Key Vault Secrets Officer" → Members: valitse oma käyttäjätilisi). CLI:llä:
>
> ```bash
> USER_ID=$(az ad signed-in-user show --query id --output tsv)
> KV_ID=$(az keyvault show --name $KEY_VAULT_NAME --query id --output tsv)
> az role assignment create --assignee $USER_ID --role "Key Vault Secrets Officer" --scope $KV_ID
> ```

**1.2** Lisää salaisuus Key Vaultiin:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa Key Vault (`kv-gallery-<etunimi>`)
2. Vasemmasta valikosta klikkaa **"Secrets"** (kohdassa *Objects*)
3. Klikkaa **"+ Generate/Import"**
4. Täytä:
   - **Upload options**: `Manual`
   - **Name**: `ModerationService--ApiKey`
   - **Secret value**: `sk-moderation-azure-production-key-12345`
5. Klikkaa **"Create"**

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "ModerationService--ApiKey" \
  --value "sk-moderation-azure-production-key-12345"
```

</details>

> **Miksi `--` (kaksi viivaa) nimessä?**
> Key Vault ei hyväksy `:` tai `__` merkkejä salaisuuden nimessä. ASP.NET Core Key Vault -provider muuntaa `--` automaattisesti konfiguraatiohierarkiaksi: `ModerationService--ApiKey` → `ModerationService:ApiKey`.

**1.3** Varmista salaisuus lisätty:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Key Vaultin **"Secrets"**-sivulla näet listan salaisuuksista
2. Klikkaa `ModerationService--ApiKey` → näet versiohistorian
3. Klikkaa versiota → **"Show Secret Value"** näyttää arvon

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az keyvault secret list \
  --vault-name $KEY_VAULT_NAME \
  --output table
```

</details>

---

### Vaihe 2: Anna App Servicelle oikeus lukea salaisuuksia

Managed Identity on jo aktivoitu Osassa 2 (`PRINCIPAL_ID`). Nyt annat sille oikeudet myös Key Vaultiin.

<details>
<summary><strong>▶ Azure Portal</strong></summary>

Roolimääritys tehdään Key Vaultin kautta — samalla tavalla kuin Osassa 2 annettiin Storage-oikeudet.

1. Avaa Key Vault (`kv-gallery-<etunimi>`)
2. Vasemmasta valikosta klikkaa **"Access control (IAM)"**
3. Klikkaa **"+ Add"** → **"Add role assignment"**
4. **Role**-välilehdellä: hae hakukentästä `Key Vault Secrets User` → valitse se → **"Next"**
5. **Members**-välilehdellä:
   - **Assign access to**: valitse `Managed identity`
   - Klikkaa **"+ Select members"**
   - Avautuvassa sivupalkissa: **Managed identity** -alasvetovalikosta valitse `App Service`
   - Listasta löydät `gallery-api-<etunimi>` — valitse se → **"Select"**
6. Klikkaa **"Review + assign"** → **"Review + assign"** uudelleen

Roolimääritys ilmestyy **"Role assignments"** -välilehdelle.

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

**2.1** Hae Key Vault -resurssin ID:

```bash
KV_ID=$(az keyvault show \
  --name $KEY_VAULT_NAME \
  --query id \
  --output tsv)
```

**2.2** Hae Principal ID (jos ei muistissa Osasta 2):

```bash
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)
```

**2.3** Anna `Key Vault Secrets User` -rooli:

```bash
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Key Vault Secrets User" \
  --scope $KV_ID
```

</details>

`Key Vault Secrets User` antaa oikeuden **lukea** salaisuuksia — muttei kirjoittaa, listata kaikkia tai hallita Key Vaultia. Jälleen minimaalinen oikeus siihen mitä tarvitaan.

> **Huom.** Jos RBAC-roolimääritys epäonnistuu portaalissa (roolia ei löydy) tai CLI:ssä (`AuthorizationFailed`), kyseessä voi olla tilauskohtainen ABAC-rajoite. Pyydä tällöin opettajaa ajamaan role assignment -komento puolestasi.

---

### Vaihe 3: Integroi Key Vault sovellukseen

### Miten Key Vault -integraatio toimii?

Key Vaultin arvo ei siirry koodiin "manuaalisesti". Sen sijaan Key Vault lisätään **konfiguraatiolähteeksi** samaan putkeen kuin `appsettings.json` ja Application Settings. ASP.NET Core hakee salaisuuden automaattisesti käynnistyksen yhteydessä.

Tämä on integraation kulku kokonaisuudessaan:

```
Program.cs käynnistyy:
  1. builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential())
     → ASP.NET Core yhdistää Key Vaultiin (Managed Identity todistaa sovelluksen identiteetin)
     → Hakee kaikki salaisuudet: "ModerationService--ApiKey" → "ModerationService:ApiKey"

  2. builder.Services.Configure<ModerationServiceOptions>(config.GetSection("ModerationService"))
     → Lukee konfiguraatiosta (nyt Key Vault on osa sitä)
     → Täyttää ModerationServiceOptions.ApiKey = "[Key Vaultista haettu arvo]"

  3. Sovellus käynnistyy, ModerationServiceClient saa IOptions<ModerationServiceOptions>
     → _options.ApiKey sisältää Key Vaultista haetun salaisuuden
     → Ei mitään erityiskoodia kontrollerissa tai käyttötapauksessa!
```

Sovelluskoodiin ei tarvitse lisätä erillisiä Key Vault SDK -kutsuja kontrollereihin tai käyttötapauksiin — konfiguraatioputki hoitaa kaiken.

**3.1** Lisää `Azure.Extensions.AspNetCore.Configuration.Secrets` NuGet-paketti `GalleryApi.WebApi`-projektiin:

```bash
cd GalleryApi.WebApi
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

**3.2** Muokkaa `GalleryApi.WebApi/Program.cs`. Lisää Key Vault konfiguraatiolähteeksi **ennen** muita `builder.Services`-kutsuja:

```csharp
using Azure.Identity;
// ... muut using-lauseet ...

var builder = WebApplication.CreateBuilder(args);

// Key Vault konfiguraatiolähteeksi
// VaultUrl tulee Application Settingsistä — ei kovakoodattu tänne
var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// ... muu koodi jatkuu normaalisti
```

> **Miksi `if (!string.IsNullOrEmpty(...))`?**
> Lokaalisti kehitettäessä `KeyVault:VaultUrl` on tyhjä (ei ole asetettu User Secretsiin), joten Key Vault -integraatio ei yritä yhdistää. Azuressa arvo on Application Settingsissa — integraatio aktivoituu automaattisesti.

**3.3** Aseta Key Vault -URL App Servicen Application Settingsiin:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa App Service (`gallery-api-<etunimi>`)
2. Vasemmasta valikosta klikkaa **"Environment variables"**
3. Klikkaa **"+ Add"** ja lisää:
   - **Name**: `KeyVault__VaultUrl`
   - **Value**: `https://kv-gallery-<etunimi>.vault.azure.net/`
4. Klikkaa **"Apply"** → vahvista **"Confirm"**

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "KeyVault__VaultUrl=https://${KEY_VAULT_NAME}.vault.azure.net/"
```

</details>

**3.4** Julkaise päivitetty sovellus:

<details>
<summary><strong>▶ Visual Studio (Publish-toiminto)</strong></summary>

1. Klikkaa **Solution Explorerissa** `GalleryApi.WebApi`-projektia hiiren oikealla → **"Publish..."**
2. Jos Publish-profiili on jo olemassa Osasta 2, klikkaa suoraan **"Publish"**
3. Jos profiilia ei ole, luo se samoin kuin Osassa 2 (Azure → App Service Linux → valitse `gallery-api-<etunimi>`)
4. Odota kunnes Output-ikkunaan tulee `Publish Succeeded`

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

Varmista, että olet `GalleryApi.WebApi`-hakemistossa:

```bash
cd GalleryApi.WebApi
dotnet publish -c Release -o ./publish

# Windows PowerShell
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force

# Linux / macOS
# zip -r deploy.zip ./publish/*

az webapp deployment source config-zip \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip
```

</details>

**3.5** Käynnistä App Service uudelleen:

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

---

### Vaihe 4: Testaa Key Vault -integraatio

**4.1** Nyt konfiguraation prioriteettijärjestys App Servicessa on:

```
Key Vault                ← ModerationService:ApiKey  (korkein prioriteetti)
Application Settings     ← Storage:Provider, Storage:AccountName, KeyVault:VaultUrl
appsettings.json         ← Oletusarvot (matalin prioriteetti)
```

**4.2** Tarkista sovelluksen loki — ei pitäisi olla Key Vault -virheitä:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa App Service (`gallery-api-<etunimi>`)
2. Vasemmasta valikosta klikkaa **"Log stream"** (kohdassa *Monitoring*)
3. Odota hetki — lokit alkavat virrata reaaliajassa
4. Etsi mahdollisia Key Vault -virheitä (esim. `KeyVaultReferenceException`, `Forbidden`)

> **Huom.** Jos Log stream on tyhjä, ota lokit käyttöön: **Diagnostic settings** → **App Service Logs** → aseta **Application Logging (Filesystem)** päälle.

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp log tail \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP
```

</details>

**4.3** Testaa Swaggerissa kuvan lataus toimii edelleen (salaisuus luetaan nyt Key Vaultista).

**4.4** Varmista, ettei `ModerationService:ApiKey` ole Application Settingsissä — se on nyt Key Vaultissa eikä sitä tarvita muualla:

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Avaa App Service → **"Environment variables"**
2. Tarkista lista: `KeyVault__VaultUrl`, `Storage__Provider`, `Storage__AccountName` ja `Storage__ContainerName` pitäisi näkyä
3. `ModerationService`-alkuisia avaimia **ei saa** olla — ne tulevat nyt Key Vaultista

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az webapp config appsettings list \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --output table
```

</details>

---

## Osa B: Infrastructure as Code (IaC) Bicepillä

Tähän asti olet luonut Azure-resurssit käsin komentoriviltä. Tässä osiossa automatisoit koko infran **Bicep-templateilla** — yhden tiedoston muutos ja yksi komento riittää.

### Mikä on Infrastructure as Code (IaC)?

**Infrastructure as Code** tarkoittaa, että infrastruktuuri (palvelimet, tietokannat, verkkoasetukset) kuvataan koodina tiedostoon — ei tehdä käsin portaalissa tai komennoilla.

Mitä hyötyä tästä on?

```
Manuaalinen työ:                    Infrastructure as Code (Bicep):
────────────────────                ──────────────────────────────────────
az group create                     az deployment group create \
az appservice plan create    →        --template-file main.bicep
az webapp create                      --parameters ...
az storage account create           
az storage container create         Yksi komento — kaikki resurssit kerralla.
az keyvault create                  
az role assignment create (x2)      Jos poistat kaiken ja ajat uudelleen,
az webapp identity assign           saat täsmälleen saman lopputuloksen.
az webapp config appsettings set    
                                    Tiedosto on versiohallinnassa Gitissä
                                    → muutoshistoria, code review, palautus.
```

### Mikä on Bicep?

**Bicep** on Azuren oma IaC-kieli, joka kääntyy ARM-templateksi (Azure Resource Manager JSON). Se on huomattavasti luettavampaa kuin suora JSON.

Bicep-tiedoston perusrakenne:

```bicep
// 1. Parametrit — arvot jotka annetaan deployauksen yhteydessä
param appName string          // pakollinen
param location string = resourceGroup().location  // valinnainen, oletusarvo

// 2. Muuttujat — johdetut arvot parametreista
var storageAccountName = 'st${appName}'

// 3. Resurssit — Azure-resurssit joita luodaan/hallitaan
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
}

// 4. Tulosteet — arvoja joita voidaan lukea deployauksen jälkeen
output storageUrl string = storageAccount.properties.primaryEndpoints.blob
```

Resurssien nimet muodostuvat `'Tyyppi@apiVersio'`-syntaksilla. Bicep käyttää viimeisin stabiili API-versio automaattisesti jos käytät VS Code Bicep -laajennosta.

---

### Vaihe 5: Luo Bicep-tiedostot

Luo projektin juureen `iac/`-kansio. Kaikki Bicep-tiedostot menevät sinne.

**5.1** Luo `iac/main.bicep`:

```bicep
targetScope = 'resourceGroup'

@description('Sovelluksen nimi (maailmanlaajuisesti uniikki)')
param appName string

@description('Azure-sijainti')
param location string = resourceGroup().location

@description('ModerationService API-avain — ei tallenneta tiedostoon!')
@secure()
param moderationApiKey string

// Johda resurssien nimet parametrista
var storageAccountName = 'stgallery${replace(appName, '-', '')}'
var keyVaultName = 'kv-${appName}'
var appServicePlanName = 'plan-${appName}'

// ── Storage Account ────────────────────────────────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storageAccount.name}/default/photos'
  properties: {
    publicAccess: 'Blob'
  }
}

// ── App Service Plan ───────────────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// ── App Service (Managed Identity aktivoitu suoraan) ─────────────────────
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        { name: 'Storage__Provider',     value: 'azure' }
        { name: 'Storage__AccountName',  value: storageAccountName }
        { name: 'Storage__ContainerName',value: 'photos' }
        { name: 'KeyVault__VaultUrl',    value: 'https://${keyVaultName}.vault.azure.net/' }
      ]
    }
  }
}

// ── Key Vault ──────────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

resource moderationSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'ModerationService--ApiKey'
  properties: {
    value: moderationApiKey
  }
}

// ── RBAC: App Service → Blob Storage ──────────────────────────────────────
resource storageBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, webApp.identity.principalId, 'StorageBlobDataContributor')
  scope: storageAccount
  properties: {
    // Storage Blob Data Contributor -roolin sisäänrakennettu GUID
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── RBAC: App Service → Key Vault ─────────────────────────────────────────
resource keyVaultSecretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webApp.identity.principalId, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    // Key Vault Secrets User -roolin sisäänrakennettu GUID
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── Tulosteet ──────────────────────────────────────────────────────────────
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output storageAccountName string = storageAccountName
output keyVaultName string = keyVaultName
```

**5.2** Luo `iac/main.bicepparam` — parametritiedosto:

```bicep
using './main.bicep'

// Vaihda omaksi
param appName = 'gallery-api-<etunimi>'

// EI tallenneta tänne — annetaan deploy-komennossa
param moderationApiKey = ''
```

> **Tärkeää:** Älä tallenna oikeaa API-avainta `.bicepparam`-tiedostoon — se menisi Gitiin! Annataan se erikseen deploy-komennossa.

---

### Vaihe 6: Ota Bicep käyttöön

**6.1** Varmista Bicep-työkalu asennettu:

```bash
az bicep install
az bicep version
```

**6.2** Luo uusi Resource Group Bicep-deploymentia varten (tai käytä olemassa olevaa):

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Hae **"Resource groups"** → klikkaa **"+ Create"**
2. **Resource group**: `rg-gallery-bicep`, **Region**: `Sweden Central`
3. Klikkaa **"Review + create"** → **"Create"**

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
az group create \
  --name rg-gallery-bicep \
  --location swedencentral
```

</details>

**6.3** Validoi Bicep-tiedosto ensin — hyvä tapa tarkistaa virheet ennen oikeaa deployausta:

```bash
az deployment group validate \
  --resource-group rg-gallery-bicep \
  --template-file iac/main.bicep \
  --parameters iac/main.bicepparam \
  --parameters moderationApiKey="sk-test-key"
```

**6.4** Ota infrastruktuuri käyttöön:

```bash
# Linux / macOS (bash)
az deployment group create \
  --resource-group rg-gallery-bicep \
  --template-file iac/main.bicep \
  --parameters iac/main.bicepparam \
  --parameters moderationApiKey="sk-moderation-azure-production-key" \
  --name "gallery-infra-$(date +%Y%m%d%H%M)"

# Windows PowerShell
az deployment group create `
  --resource-group rg-gallery-bicep `
  --template-file iac/main.bicep `
  --parameters iac/main.bicepparam `
  --parameters moderationApiKey="sk-moderation-azure-production-key" `
  --name "gallery-infra-$(Get-Date -Format 'yyyyMMddHHmm')"
```

Deployment kestää 3-5 minuuttia. Azure luo kaikki resurssit ja RBAC-roolimääritykset automaattisesti.

**6.5** Tarkista tulosteet:

```bash
az deployment group show \
  --resource-group rg-gallery-bicep \
  --name "gallery-infra-..." \
  --query properties.outputs \
  --output table
```

Näet `webAppUrl`:n — sovelluksen URL Azuressa.

**6.6** Julkaise sovellus Bicepillä luotuun App Serviceen. Varmista, että olet `GalleryApi.WebApi`-hakemistossa:

```bash
cd GalleryApi.WebApi
dotnet publish -c Release -o ./publish

# Windows PowerShell
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force

# Linux / macOS
# zip -r deploy.zip ./publish/*

az webapp deployment source config-zip \
  --name gallery-api-<etunimi> \
  --resource-group rg-gallery-bicep \
  --src deploy.zip
```

---

### Vaihe 7: Vertailu — manuaalivaiheet vs. Bicep

| | Manuaalisesti (Osat 2 & 3) | Bicepillä |
|---|---|---|
| Resurssien luominen | ~10 komentoa | 1 deployment |
| RBAC-roolimääritykset | 2 erillistä komentoa | Automaattisesti templatessa |
| Managed Identity | Erillinen komento | `identity: { type: 'SystemAssigned' }` |
| Toistettavuus | Mahdolliset käsivirheet | Aina täsmälleen sama lopputulos |
| Dokumentaatio | Erilliset muistiinpanot | Koodi on dokumentaatio |
| Versionhallinta | Ei | Git-historia |
| Ympäristöt (dev/prod) | Erilliset skriptit | Eri parametritiedostot |

**Milloin manuaalisesti, milloin Bicepillä?**

- Manuaalisesti: Kokeiluihin, kertakäyttöiseen testaukseen, yksittäisen resurssin muuttamiseen
- Bicepillä: Toistettava infra, tiimityö, useita ympäristöjä (dev/test/prod), tuotanto

---

## Siivous

<details>
<summary><strong>▶ Azure Portal</strong></summary>

1. Hae **"Resource groups"** ja avaa `rg-gallery-<etunimi>`
2. Klikkaa **"Delete resource group"** → kirjoita Resource Groupin nimi vahvistuskenttään → **"Delete"**
3. Jos teit Bicep-deploymentin, toista sama `rg-gallery-bicep`-ryhmälle
4. **Key Vault soft delete:** Hae **"Key vaults"** → klikkaa yläpalkin **"Manage deleted vaults"** → valitse Key Vault → **"Purge"**

</details>

<details>
<summary><strong>▶ Azure CLI</strong></summary>

```bash
# Poista manuaalisesti luodut resurssit
az group delete --name $RESOURCE_GROUP --yes --no-wait

# Poista Bicep-resurssit
az group delete --name rg-gallery-bicep --yes --no-wait

# Key Vault menee "soft delete" -tilaan — purge jos haluat poistaa kokonaan
az keyvault purge --name $KEY_VAULT_NAME --location $LOCATION
```

</details>

---

## Koko arkkitehtuurin yhteenveto

Tässä on mitä olet rakentanut kolmen osan aikana:

```
KEHITYSYMPÄRISTÖ                     TUOTANTOYMPÄRISTÖ (Azure)
─────────────────────────────        ─────────────────────────────────────
LocalStorageService                  AzureBlobStorageService
  → wwwroot/uploads/                   → Azure Blob Storage
  → URL: /uploads/albumId/photo.jpg    → URL: https://...blob.core.windows.net/...

User Secrets                         Key Vault
  ModerationService:ApiKey             ModerationService--ApiKey
  → .microsoft/usersecrets/            → Salattu, versioitu, auditoitu

appsettings.json                     Application Settings
  Storage:Provider = "local"           Storage:Provider = "azure"
                                       Storage:AccountName = "stgallery..."
                                       KeyVault:VaultUrl = "https://..."

Sama sovelluskoodi molemmissa!
Vain konfiguraatio eroaa.
```

| Osa | Konseptit |
|---|---|
| Osa 1 | Clean Architecture, kovakoodattu salaisuus, User Secrets, Options Pattern, LocalStorageService |
| Osa 2 | App Service, Application Settings, DefaultAzureCredential, Managed Identity, RBAC, AzureBlobStorageService |
| Osa 3 | Key Vault, salaisuudet vs. konfiguraatioarvot, Key Vault -integraatio `Program.cs`:ssä, Bicep IaC |

## Soveltamishaaste (suositus)

Varmista että osaat käyttää tekniikkaa myös eri tilanteessa:
1. Lisää Key Vaultiin toinen salaisuus, esim. `ModerationService--BaseUrl`.
2. Poista vastaava arvo `appsettings.json`:sta tai jätä siihen vain placeholder.
3. Varmista, että `IOptions<ModerationServiceOptions>` saa arvon Key Vaultista.
4. Tee sama muutos myös Bicep-templateen niin, että salaisuus syntyy automaattisesti deployssa.

---

## Palautustarkistuslista

- [ ] Key Vault luotu ja `ModerationService--ApiKey` tallennettu sinne
- [ ] RBAC-roolimääritys (`Key Vault Secrets User`) tehty Key Vaultiin
- [ ] `AddAzureKeyVault` lisätty `Program.cs`:ään
- [ ] `KeyVault__VaultUrl` asetettu Application Settingsiin
- [ ] Sovellus julkaistu uudelleen — Key Vault -integraatio toimii
- [ ] Kuvien lataus toimii edelleen (Managed Identity käytössä molempiin: Blob Storage + Key Vault)
- [ ] Bicep-tiedostot (`iac/main.bicep`, `iac/main.bicepparam`) luotu
- [ ] Bicep-deployment suoritettu onnistuneesti uuteen Resource Groupiin
- [ ] Vastaukset `questions-part3.md`-tiedostossa

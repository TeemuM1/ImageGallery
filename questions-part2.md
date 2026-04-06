# Kysymykset — Osa 2: Azure-julkaisu

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät.

---

## Azure Blob Storage

**1.** Mitä eroa on `LocalStorageService.UploadAsync`:n ja `AzureBlobStorageService.UploadAsync`:n palauttamilla URL-arvoilla? Miksi ne eroavat?

> Vastauksesi: LocalStorageService palauttaa URL:n muodossa /upload/albumId/photo.jpg ja BlobStorageService taas https://...blob.core.windows.net/.. muodossa. Ero johtuu siitä, että LocalStoragessa kuva sijaitsee lokaalilla levyllä, kun BlobStorage taas Azuren Blob Containerissa, joka on pilvipalvelu.

---

**2.** `AzureBlobStorageService` luo `BlobServiceClient`:n käyttäen `DefaultAzureCredential()` eikä yhteysmerkkijonoa. Mitä etua tästä on? Mitä `DefaultAzureCredential` tekee eri ympäristöissä?

> Vastauksesi: Etuna on, että voit tunnistautua Azureen ilman avaimia. Se kokeilee automaattisesti eri tapoja kuten Managed Identity, Azure CLI sekä Visual Studio kirjautuminen. Tärkein hyöty on se, että sama koodi toimii kehityskoneella ja Azuressa. Kehitysympäristössä se käyttää Azure CLI kirjautumista ja Azuressa Managed Identityä.

---

**3.** Blob Container luodaan `--public-access blob` -asetuksella. Mitä tämä tarkoittaa: mitä pystyy tekemään ilman tunnistautumista, ja mikä vaatii Managed Identityn?

> Vastauksesi: Kuvien URL:it ovat julkisia joten niitä voidaan katsoa/lukea ilman tunnistautumista, mutta kuvien lataaminen vaatii tunnistautumisen Managed Identityllä.

---

## Application Settings

**4.** Application Settings ylikirjoittavat `appsettings.json`:n arvot. Selitä tämä mekanismi: miten se toimii ja miksi se on hyödyllistä eri ympäristöjä varten?

> Vastauksesi: Application Settings on App Servicen ominaisuus, joka mahdollistaa konfiguraatioarvojen syöttämisen sovellukselle. Application Setting sopii arvoille, jotka eivät ole salaisuuksia.

---

**5.** Application Settingsissa käytetään `Storage__Provider` (kaksi alaviivaa), mutta koodissa luetaan `configuration["Storage:Provider"]` (kaksoispiste). Miksi?

> Vastauksesi: Application Settings ei salli kaksoispistettä, mutta ASP.NET Core tunnistaa kaksi alaviivaa hierarkkisina avaimina, joten `Storage__Provider` vastaa `Storage:Provider`-avainta koodissa.

---

**6.** Mitkä konfiguraatioarvot soveltuvat Application Settingsiin, ja mitkä eivät? Anna esimerkki kummastakin tässä tehtävässä.

> Vastauksesi: Sopivia arvoja Application Settingsiin ovat ei-salaiset konfiguraatioarvot, kuten `Storage:Provider` tai `Logging:LogLevel`. Ei-sopivia arvoja ovat salaisuudet, kuten API-avaimet tai yhteysmerkkijonot, jotka tulisi tallentaa Azure Key Vaultiin tai User Secrets -työkaluun. Tässä työssä application settings sisältää esim Storage__Provider ja Storage__AccountName. Key Vault taas sisältää esim ModerationService__ApiKey.

---

## Managed Identity ja RBAC

**7.** Selitä omin sanoin: mitä tarkoittaa "System-assigned Managed Identity"? Mitä tapahtuu tälle identiteetille, jos App Service poistetaan?

> Vastauksesi: Tarkoittaa sitä, että Azure luo automaattisesti identiteetin App Servicelle. Identiteetti poistetaan automaattisesti silloin, kun App Service poistetaan.

---

**8.** App Servicelle annettiin `Storage Blob Data Contributor` -rooli Storage Accountin tasolle — ei koko subscriptionin tasolle. Miksi tämä on parempi tapa? Mikä periaate tähän liittyy?

> Vastauksesi: Tässä noudatetaan vähimmän oikeuden periaatetta eli principle of least privilege, joka tarkoittaa sitä, että identiteetille annetaan vain ne oikeudet, jotka se tarvitsee toimiakseen. Antamalla rooli vain Storage Accountin tasolle, rajoitetaan identiteetin oikeuksia vain siihen resurssiin, jota se tarvitsee, eikä koko subscriptioniin.

---



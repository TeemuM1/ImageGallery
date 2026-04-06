# Kysymykset — Osa 3: Key Vault ja Infrastructure as Code

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät.

---

## Key Vault

**1.** Miksi `ModerationService:ApiKey` tallennettiin Key Vaultiin eikä Application Settingsiin? Mitä lisäarvoa Key Vault tuo Application Settingsiin verrattuna?

> Vastauksesi: Koska Key Vault on tarkoitettu salaisuuksien hallintaan. Se tarjoaa lisäarvoa, kuten salauksen lepotilassa, versiohistorian, auditoinnin ja RBAC pääsynhallinnan.

---

**2.** Key Vault -salaisuuden nimi on `ModerationService--ApiKey` (kaksi väliviivaa), mutta koodissa se luetaan `configuration["ModerationService:ApiKey"]` (kaksoispiste). Miksi käytetään `--`?

> Vastauksesi: Koska Key Vault ei salli kaksoispistettä salaisuuden nimessä, mutta ASP.NET Core tunnistaa kaksi väliviivaa hierarkkisina avaimina, joten `ModerationService--ApiKey on vastaava `ModerationService:ApiKey`-avainta koodissa.

---

**3.** `Program.cs`:ssä Key Vault lisätään konfiguraatiolähteeksi `if (!string.IsNullOrEmpty(keyVaultUrl))`-ehdolla. Miksi tämä ehto on tärkeä? Mitä tapahtuisi ilman sitä?

> Vastauksesi: Koska lokaalisti kehittäessä keyVaultUrl on tyhjä, joten ilman ehtoa sovellus yrittäisi hakea Key Vaultista konfiguraatiota ja epäonnistuisi. Ehto varmistaa, että Key Vaultia käytetään vain silloin, kun URL on määritetty, mikä yleensä tapahtuu Azuressa.

---

**4.** Kun sovellus on käynnissä Azuressa, konfiguraation prioriteettijärjestys on: Key Vault → Application Settings → `appsettings.json`. Selitä millä arvolla `ModerationService:ApiKey` lopulta ladataan — ja käy läpi jokainen askel siitä, miten arvo päätyy sovelluksen `IOptions<ModerationServiceOptions>`:iin.

> Vastauksesi: Aluksi Program.cs käynnistyy ja ASP.NET Core yhdistää Key Vaultiin ja hakee kaikki salaisuudet > Lukee konfiguraatiosta ModerationService kohdan ja täyttää ModeratioServiceOptions.ApiKey arvon Key Vaultista haetulla arvolla > Sovellus käynnistyy ja ModerationServiceClient saa IOptions<ModerationServiceOptions> ja _options.ApiKey sisältää Key Vaultista haetun salaisuuden eikä mitää erityistä koodia tarvita kontrollerissa.

---

**5.** Mitä eroa on `Key Vault Secrets User` ja `Key Vault Secrets Officer` -roolien välillä? Miksi annettiin nimenomaan `Secrets User`?

> Vastauksesi: User saa oikeuden lukea salaisuuksia ja Office saa myös kirjoittaa, listata ja hallita Key Vaultia. App Servicelle riittää pelkkä salaisuuksien lukuoikeus.

---

## Infrastructure as Code (Bicep)

**6.** Bicep-templatessa RBAC-roolimääritykset tehdään suoraan (`storageBlobRole`, `keyVaultSecretsRole`). Mitä etua tällä on verrattuna siihen, että ajat erilliset `az role assignment create` -komennot käsin?

> Vastauksesi: Se nopeuttaa ja helpottaa työtä eikä aikaa kulu manuaaliseen roolimääritysten tekemiseen. Lisäksi se varmistaa, että kaikki tarvittavat roolit määritetään joka ikiselle deploymentille, eikä mikään jää vahingossa tekemättä.

---

**7.** Bicep-parametritiedostossa `main.bicepparam` on `param moderationApiKey = ''` — arvo jätetään tyhjäksi. Miksi? Miten oikea arvo annetaan?

> Vastauksesi: Se jätetään tyhjäksi, koska arvo on salaisuus ja se annetaan erikseen deploy-komennossa.

---

**8.** Bicep-templatessa `webApp`-resurssin `identity`-lohkossa on `type: 'SystemAssigned'`. Mitä tämä tekee, ja mitä manuaalista komentoa se korvaa?

> Vastauksesi: Se luo automaattisesti identiteetin App Servicelle. Manuaalisesti tämä vastaisi `az webapp identity assign` -komentoa, joka luo Managed Identityn App Servicelle.

---

**9.** RBAC-roolimäärityksen nimi generoidaan `guid()`-funktiolla:

```bicep
name: guid(storageAccount.id, webApp.identity.principalId, 'StorageBlobDataContributor')
```

Miksi nimi generoidaan näin eikä esimerkiksi kovakoodatulla merkkijonolla? Mitä tapahtuisi jos nimi olisi sama kaikissa deploymenteissa?

> Vastauksesi: Koska RBAC-roolimäärityksen nimen pitää olla uniikki, niin ei voida käyttää kovakoodattua merkkijonoa. Jos nimi olisi sama kaikissa deploymenteissa, niin deployment epäonnistuu, koska roolimääritys on jo olemassa.

---

**10.** Olet nyt rakentanut saman infrastruktuurin kahdella tavalla: manuaalisesti (Osat 2 & 3) ja Bicepillä (Osa 3). Kuvaile konkreettisesti yksi tilanne, jossa IaC-lähestymistapa on selvästi manuaalista parempi. Kuvaile myös tilanne, jossa manuaalinen tapa riittää.

> Vastauksesi: Jos kyseessä on pieni projekti, jossa on vain muutama resurssi niin manuaalinen tapa on riittävä. Isommissa projekteissa, joissa on paljon resursseja ja monimutkaisia riippuvuuksia, IaC on selvästi parempi, koska se automatisoi infrastruktuurin luomisen ja hallinnan, mikä säästää aikaa ja vähentää virheiden mahdollisuutta. Lisäksi IaC mahdollistaa versionhallinnan infrastruktuurikoodille, mikä helpottaa muutosten seurantaa ja rollbackia tarvittaessa.

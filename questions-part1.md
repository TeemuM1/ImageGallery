# Kysymykset — Osa 1: Lokaali kehitys

Vastaa kysymyksiin omin sanoin. Lyhyet, selkeät vastaukset riittävät — tarkoitus on osoittaa, että olet ymmärtänyt konseptit.

---

## Clean Architecture

**1.** Selitä omin sanoin: mitä tarkoittaa, että `UploadPhotoUseCase` "ei tiedä" tallennetaanko kuva paikalliselle levylle vai Azureen? Näytä koodirivit, jotka osoittavat tämän.

> Vastauksesi: UploadPhotoUseCase käyttää IStorageService-rajapintaa, joka on määritelty GalleryApi.Domain-kerroksessa. Tämä tarkoittaa, että UploadPhotoUseCase ei ole riippuvainen mistään tietystä tallennusratkaisusta (kuten LocalStorageService tai AzureBlobStorageService). Koodirivit, jotka osoittavat tämän, löytyvät UploadPhotoUseCase-luokasta riviltä: 12, 24, 28 ja 54

---

**2.** Miksi `IStorageService`-rajapinta on määritelty `GalleryApi.Domain`-kerroksessa, mutta `LocalStorageService` on `GalleryApi.Infrastructure`-kerroksessa? Mitä hyötyä tästä jaosta on?

> Vastauksesi: Domain-kerros sisältää liiketoimintalogiikan ja rajapinnat ja Infrastructure-kerros sisältää teknisen toteutuksen. Mahdollistaa sen, että sovelluslogiikka pysyy riippumattomana tallennustekniikasta.

---

**3.** Testit käyttävät `Mock<IAlbumRepository>`. Mitä mock-objekti tarkoittaa, ja miksi Clean Architecture tekee tämän testaustavan mahdolliseksi?

> Vastauksesi: Mock-objekti on simuloitu versio oikeasta objektista, joka toteuttaa saman rajapinnan. Clean Architecture mahdollistaa tämän, koska liiketoimintalogiikka on erillään teknisistä toteutuksista, joten testit voivat käyttää mock-objekteja riippuvuuksien sijaan.

---

## Salaisuuksien hallinta

**4.** Kovakoodattu API-avain on ongelma, vaikka repositorio olisi yksityinen. Selitä kaksi eri syytä miksi.

> Vastauksesi: 1. Git-historia sisältää silti avaimet. 2. On olemassa botteja, jotka skannaavat avaimia ja ne voidaan löytää nopeasti, joka johtaa väärinkäyttöön.

---

**5.** Riittääkö se, että poistat kovakoodatun avaimen myöhemmässä commitissa? Perustele vastauksesi.

> Vastauksesi: Ei riitä, koska Git-historia sisältää silti avaimet.

---

**6.** Minne User Secrets tallennetaan käyttöjärjestelmässä? (Mainitse sekä Windows- että Linux/macOS-polut.) Miksi tämä sijainti on turvallinen?

> Vastauksesi: Windows: `%APPDATA%\Microsoft\UserSecrets\`, Linux/macOS: `~/.microsoft/usersecrets/`. Sijaitsee projektin ulkopuolella vain paikallisesti joten on turvallinen.

---

## Options Pattern ja konfiguraatio

**7.** Mitä hyötyä on `IOptions<ModerationServiceOptions>`:n käyttämisestä verrattuna siihen, että luetaan arvo suoraan `IConfiguration`-rajapinnalta (`configuration["ModerationService:ApiKey"]`)?

> Vastauksesi: Tyyppiturvallisuus, IntelliSense-tuki, helppo testattavuus sekä ei tarvitse muistaa avaimen tarkkaa merkkijonoa ulkoa.

---

**8.** ASP.NET Core lukee konfiguraation useista lähteistä prioriteettijärjestyksessä. Listaa lähteet korkeimmasta matalimpaan ja selitä, mikä arvo lopulta käytetään, kun sama avain on sekä `appsettings.json`:ssa että User Secretsissä.

> Vastauksesi: 1. User Secrets 2. appsettins.Development.json 3. appsettings.json. Kun sama avain on olemassa, niin käytetään User Secretsissä olevaa arvoa, koska se on korkeammalla prioriteettilistalla.

---

**9.** `DependencyInjection.cs`:ssä valitaan tallennustoteutus näin:

```csharp
var provider = configuration["Storage:Provider"] ?? "local";
if (provider == "azure")
    services.AddScoped<IStorageService, AzureBlobStorageService>();
else
    services.AddScoped<IStorageService, LocalStorageService>();
```

Miksi käytetään konfiguraatioarvoa `env.IsDevelopment()`-tarkistuksen sijaan? Mitä haittaa olisi `if (env.IsDevelopment()) { käytä lokaalia }`-lähestymistavassa?

> Vastauksesi: Käyttämällä konfiguraatioarvoa, voidaan helposti vaihtaa tallennustoteutusta ilman, että tarvitsee muuttaa koodia. `env.IsDevelopment()`-tarkistuksen käyttäminen rajoittaisi tallennustoteutuksen vain kehitysympäristöön, eikä sitä voisi käyttää esimerkiksi staging- tai production-ympäristöissä ilman koodimuutoksia.

---

## Tiedostotallennus

**10.** Kun lataat kuvan, `imageUrl`-kentän arvo on `/uploads/abc123-..../photo.jpg`. Miten tähän URL:iin pääsee selaimella? Mihin koodiin tämä perustuu?

> Vastauksesi: Selaimella pääsee URL:iin, koska `Program.cs`:ssä on määritetty staattisten tiedostojen palveleminen `app.UseStaticFiles()`-kutsulla. Tämä mahdollistaa sen, että kaikki `wwwroot`-kansiossa olevat tiedostot (mukaan lukien `uploads`-kansio) ovat saatavilla URL-polun kautta.

---

**11.** Mitä tapahtuu jos yrität ladata tiedoston jonka MIME-tyyppi on `application/pdf`? Missä tiedostossa ja millä koodirivillä tämä käyttäytyminen on määritelty?

> Vastauksesi: Tiedoston lataus epäonnistuu, koska `UploadPhotoUseCase` tarkistaa MIME-tyypin ennen tallennusta. Tämä käyttäytyminen on määritelty `UploadPhotoUseCase.cs`-tiedostossa, rivillä 15 ja 16.

---

**12.** `DeletePhotoUseCase` poistaa tiedoston kutsumalla `_storageService.DeleteAsync(photo.FileName, photo.AlbumId)` — ei `photo.ImageUrl`:lla. Miksi?

> Vastauksesi: Koska `photo.ImageUrl` sisältää URL-polun, joka ei välttämättä vastaa suoraan tallennustiedoston nimeä tai sijaintia. `photo.FileName` ja `photo.AlbumId` tarjoavat tarvittavat tiedot oikean tiedoston löytämiseksi ja poistamiseksi tallennuspalvelusta.

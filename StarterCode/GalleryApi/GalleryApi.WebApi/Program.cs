using GalleryApi.Application;
using GalleryApi.Infrastructure;
using GalleryApi.Infrastructure.Moderation;
using GalleryApi.Infrastructure.Options;
using GalleryApi.Infrastructure.Persistence;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Konfiguraatio-osiot (Options Pattern)
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<ModerationServiceOptions>(
    builder.Configuration.GetSection(ModerationServiceOptions.SectionName));

builder.Services.AddSingleton<ModerationServiceClient>();

// Sovellus- ja infrastruktuurikerrokset
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Gallery API",
        Version = "v1",
        Description = "Kuvakirjasto API — Clean Architecture -esimerkki"
    });
});

var app = builder.Build();

// Tietokanta — luodaan automaattisesti jos ei ole olemassa
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GalleryDbContext>();
    db.Database.EnsureCreated();
}



app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

// Staattisten tiedostojen jako — tarjoilee wwwroot-kansion sisällön
// Tarvitaan paikallisesti tallennettujen kuvien näyttämiseen
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();

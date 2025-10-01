using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using HackathonBloomWatch.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configurar la conexión a SQLServer
builder.Services.AddDbContext<HackathonBWContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SQLConnection"), x => x.UseNetTopologySuite()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// para migración y creación de la base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<HackathonBWContext>();
    context.Database.EnsureCreated(); // crea la base de datos si no existe
}

app.UseHttpsRedirection();
//app.UseStaticFiles();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".kml"] = "application/vnd.google-earth.kml+xml";
provider.Mappings[".geojson"] = "application/geo+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

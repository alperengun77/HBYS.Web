using HBYS.Web.Data;
using HBYS.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LogService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.Use(async (context, next) =>
{
    await next();

    string? controller = context.Request.RouteValues["controller"]?.ToString();
    string? action = context.Request.RouteValues["action"]?.ToString();

    if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
    {
        return;
    }

    bool loglanacakMi =
        context.Request.Method == "POST" ||
        action == "Delete";

    if (!loglanacakMi)
    {
        return;
    }

    if (controller == "Account" && action == "Login")
    {
        return;
    }

    if (controller == "Log")
    {
        return;
    }

    if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 400)
    {
        return;
    }

    string modul = ModulAdiGetir(controller);
    string islemTipi = IslemTipiGetir(action);
    string islem = modul + " " + islemTipi;

    string aciklama = $"{modul} modülünde {islemTipi.ToLower()} işlemi yapıldı.";

    var logService = context.RequestServices.GetRequiredService<LogService>();
    logService.Logla(modul, islemTipi, islem, aciklama);
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

static string ModulAdiGetir(string controller)
{
    return controller switch
    {
        "Hasta" => "Hasta",
        "Doktor" => "Doktor",
        "Poliklinik" => "Poliklinik",
        "Randevu" => "Randevu",
        "Muayene" => "Muayene",
        "Recete" => "Reçete",
        "Tahlil" => "Tahlil",
        "Kullanici" => "Kullanıcı",
        "Rol" => "Rol",
        "Dashboard" => "Dashboard",
        "Rapor" => "Rapor",
        "Database" => "Veritabanı",
        _ => controller
    };
}

static string IslemTipiGetir(string action)
{
    return action switch
    {
        "Create" => "Ekleme",
        "Edit" => "Güncelleme",
        "Delete" => "Silme",
        "YedekAl" => "Yedekleme",
        _ => "İşlem"
    };
}
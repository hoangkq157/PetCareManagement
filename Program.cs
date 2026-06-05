using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PetCareDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PetCareConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PetCareDbContext>(options =>options.UseSqlServer(builder.Configuration.GetConnectionString("PetCareConnection")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
 
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();



app.Run();

using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 209715200;
});

// 2. Configurar FormOptions (por si acaso usas multipart/form-data en el futuro)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

Log.Logger = new LoggerConfiguration()
    // 1. Silencia los Queries SQL (Nivel Información)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)

    // 2. SILENCIA LOS WARNINGS DE MODELO (Esto quitará los mensajes de Decimal y Byte Identity)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Model.Validation", Serilog.Events.LogEventLevel.Error)

    // 3. Silencia logs de infraestructura que no sean errores
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", Serilog.Events.LogEventLevel.Error)
    .Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("SourceContext")
        && logEvent.Properties["SourceContext"].ToString().Contains("Microsoft.EntityFrameworkCore.Database.Command")) // OPCIÓN B: Excluir totalmente
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/CostManagament_log-.txt",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 50_000_000,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
builder.Host.UseSerilog();
builder.Services.Configure<IISServerOptions>(options => {
    options.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<ParametrosConfig>(builder.Configuration.GetSection("Catalogos"));
// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss";
    });
//Se reemplaza la forma de agregar servicios de la capa de infraestructura para evitar problemas de dependencias circulares
//builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
//builder.Services.AddScoped<IProcesoParametro, ProcesoParametro>();
//builder.Services.AddScoped<ICostoMaterialEmpaque, CostoMaterialEmpaque>();
//builder.Services.AddScoped<IMateriaPrima, MateriaPrima>();
//builder.Services.AddSingleton<OpenJsonToInClauseInterceptor>();
//builder.Services.AddScoped<CalculoCostosFeature>();
builder.Services.AddInfrastructureServices();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<CostManagementDbContext>((serviceProvider, options) =>
{

    var interceptor = serviceProvider.GetRequiredService<OpenJsonToInClauseInterceptor>();
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => {
                sqlOptions.CommandTimeout(180); // Establece el timeout por defecto aquí
            })
    .AddInterceptors(interceptor)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    //.ConfigureWarnings(w => w.Ignore(
    //           Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.ModelValidationKeyDefaultValueWarning,
    //           Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.ByteIdentityColumnWarning
    //       ));

});


builder.Services.AddDbContextFactory<SongDbContext>((serviceProvider, options) =>
{

    var interceptor = serviceProvider.GetRequiredService<OpenJsonToInClauseInterceptor>();
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionSong"))
    .AddInterceptors(interceptor)
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    //.ConfigureWarnings(w => w.Ignore(
    //           Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.ModelValidationKeyDefaultValueWarning,
    //           Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.ByteIdentityColumnWarning
    //       ));
});


builder.Services.AddDbContextFactory<CostosDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionCostos"))
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    //.ConfigureWarnings(w => w.Ignore(
    //           Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.ModelValidationKeyDefaultValueWarning,
    //           Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.ByteIdentityColumnWarning
    //       ));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ========== USAR CORS (ANTES DE UseAuthorization) ==========
app.UseCors("AllowAll");
//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

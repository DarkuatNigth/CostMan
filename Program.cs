using CostManagement.Aplicación.Features;
using CostManagement.Dominio.Entidades;
using CostManagement.Infraestructura.DBContext;
using CostManagement.Infraestructura.Utils;
using CostManagementService.Infraestructura.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Text;


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
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    // 1. Silencia los Queries SQL (Nivel Información)
    //.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)

    //// 2. SILENCIA LOS WARNINGS DE MODELO (Esto quitará los mensajes de Decimal y Byte Identity)
    //.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Model.Validation", Serilog.Events.LogEventLevel.Error)

    //// 3. Silencia logs de infraestructura que no sean errores
    //.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", Serilog.Events.LogEventLevel.Error)
    //.Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("SourceContext")
    //    && logEvent.Properties["SourceContext"].ToString().Contains("Microsoft.EntityFrameworkCore.Database.Command")) // OPCIÓN B: Excluir totalmente
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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CostManagementService", Version = "v1" });
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
    // Definici�n de seguridad para JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Por favor, agregue el token para estar autenticado",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuers = builder.Configuration.GetSection("Jwt:Issuer").Get<string[]>(),
            ValidAudiences = builder.Configuration.GetSection("Jwt:Audience").Get<string[]>(),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

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

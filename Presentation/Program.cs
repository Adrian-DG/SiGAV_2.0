using Application;
using Infrastructure;
using Infrastructure.Persistance.Seeding;
using Presentation.Middleware;
using Presentation.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// CORS: solo para clientes web (front desk, Expo web). Las apps nativas no lo necesitan.
// Orígenes por ambiente en "Cors:AllowedOrigins"; sin configurar no se permite ninguno.
const string CorsPolicy = "SiGAVClients";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// https://localhost:{7148/5282}/openapi/v1.json
builder.Services.AddOpenApi(opt =>
{ 
    opt.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "SiGAV API";
        document.Info.Description = "SiGAV API for managing and processing data.";
        document.Info.Version = "v1";
        document.Info.Summary = "API for SiGAV application";
        return Task.CompletedTask;
    });

    // Autenticación JWT Bearer en la documentación (Scalar)
    opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    opt.AddOperationTransformer<AuthorizeOperationTransformer>();
});

var app = builder.Build();

// Esquema y carga inicial según la sección "Seed" (idempotente: solo llena tablas vacías)
await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // https://localhost:{7148/5282}/scalar
    app.MapScalarApiReference(options => options
        .WithTitle("SiGAV API")
        .AddPreferredSecuritySchemes([BearerSecuritySchemeTransformer.SchemeName])
        .AddHttpAuthentication(BearerSecuritySchemeTransformer.SchemeName, _ => { })
        // Conserva el token al recargar la página (solo en el navegador de quien lo usa)
        .EnablePersistentAuthentication());
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// Antes de la autenticación: el preflight (OPTIONS) no lleva token
app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

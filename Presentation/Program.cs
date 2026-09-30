using Application;
using Microsoft.AspNetCore.HttpOverrides;
using Infrastructure;
using Infrastructure.Persistance.Seeding;
using Presentation.Middleware;
using Presentation.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Detrás de un proxy (Dev Tunnels, IIS, nginx...): la URL pública llega en X-Forwarded-*.
// Sin esto HTTPS redirige a https://localhost y el documento OpenAPI (Scalar) apunta a localhost.
// Solo se aceptan de proxies en loopback (valor por defecto), así un cliente externo no puede falsearlos.
builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders =
    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost);

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
// Primero: todo lo que sigue (redirección HTTPS, OpenAPI, logs) debe ver el esquema y host públicos
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // https://localhost:{7148/5282}/scalar
    app.MapScalarApiReference(options => options
        .WithTitle("SiGAV API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .AddPreferredSecuritySchemes([BearerSecuritySchemeTransformer.SchemeName])
        .AddHttpAuthentication(BearerSecuritySchemeTransformer.SchemeName, _ => { })
        // Conserva el token al recargar la página (solo en el navegador de quien lo usa)
        .EnablePersistentAuthentication());

    // Con un Dev Tunnel activo, Visual Studio pasa su URL pública en VS_TUNNEL_URL: la raíz abierta
    // desde localhost lleva a Scalar en el túnel. Solo la raíz; la API nunca redirige (rompería a
    // los clientes locales, como Expo web).
    var tunnelUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
    if (tunnelUrl is not null)
        app.Logger.LogInformation("Dev Tunnel activo: {TunnelUrl}/scalar/v1", tunnelUrl);

    app.MapGet("/", (HttpRequest request) =>
            tunnelUrl is not null && !tunnelUrl.EndsWith(request.Host.Value!, StringComparison.OrdinalIgnoreCase)
                ? Results.Redirect($"{tunnelUrl}/scalar/v1")
                : Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// Antes de la autenticación: el preflight (OPTIONS) no lleva token
app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

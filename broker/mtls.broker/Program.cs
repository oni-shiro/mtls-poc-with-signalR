using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.WebHost.ConfigureKestrel(serverops =>
{
    Console.WriteLine($"Configuring Kestrel for mTLS : {serverops}");

#pragma warning disable SYSLIB0057
    serverops.ConfigureHttpsDefaults(https =>
    {
        Console.WriteLine($"Configuring HTTPS defaults for mTLS : {https}");
        var certificate = new X509Certificate2(
            @"..\..\security\certs\broker.pfx",
            "password",
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
        );
        // Add the certificate to the HTTPS configuration
        https.ServerCertificate = certificate;

        https.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            Console.WriteLine($"Validating client certificate: {certificate.Subject}");
            var rootCertificate = new X509Certificate2(
                @"..\..\security\certs\rootCA.crt"
            );

            return errors == SslPolicyErrors.None && 
                   certificate.Issuer == rootCertificate.Subject &&
                   certificate.Verify() &&
                   certificate.GetPublicKeyString() == rootCertificate.GetPublicKeyString();
        };
    });
#pragma warning restore SYSLIB0057
});

builder.Services.AddControllers();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "Broker is running securely.");
// Configure the HTTP request pipeline.
app.Run();


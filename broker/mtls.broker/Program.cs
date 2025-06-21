using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

Console.WriteLine("[Broker] starting mTLS broker...");
var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
Console.WriteLine("Configuring kestrel for mTLS...");
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

        https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        https.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            Console.WriteLine($"Validating client certificate: {certificate.Subject}");
            var rootCertificate = new X509Certificate2(
                @"..\..\security\certs\rootCA.cer"
            );

            chain.ChainPolicy.ExtraStore.Add(rootCertificate);

            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            bool isChainValid = chain.Build(certificate);
            chain.ChainStatus
                .ToList()
                .ForEach(status => Console.WriteLine($"Chain status: {status.Status} - {status.StatusInformation}"));
            if (!isChainValid)
            {
                Console.WriteLine($"Certificate chain validation failed: {errors}");
            }
            Console.WriteLine($"Certificate validation result: {isChainValid}, Errors: {errors}");
            return isChainValid && errors == SslPolicyErrors.None;
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


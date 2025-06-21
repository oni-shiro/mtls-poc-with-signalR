using System.Security.Cryptography.X509Certificates;

namespace Mtls.Worker.Configuration;

public static class HttpClientExtension
{
    public static IServiceCollection AddNamedHttpClient(this IServiceCollection services)
    {
#pragma warning disable SYSLIB0057
        services.AddHttpClient("workerClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7000");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                Console.WriteLine($"In ServerCertifacteCustomCallback: {cert?.Subject}");
                // Accept all certificates for demonstration purposes
                // In production, you should validate the certificate properly
                return true;
            };

            var certificate = new X509Certificate2(
                @"..\..\security\certs\worker.pfx",
                "password",
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
            );

            handler.ClientCertificates.Add(certificate);
            return handler;
        });
#pragma warning restore SYSLIB0057

        return services;
    }
}
# mtls-poc-with-signalR
mtls-poc-with-signalR

# Generating Certificates
Here we are generating one root certificate that will work as local CA and that needs to be added to the trusted store after generating.
```
$rootCert = New-SelfSignedCertificate `
    -Subject "CN=MyRootCA" `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyUsageProperty Sign `
    -KeyUsage CertSign `
    -NotAfter (Get-Date).AddYears(10) `
    -FriendlyName "MyRootCA" `
    -TextExtension @("2.5.29.19={critical}{text}CA=true&pathlength=3") # ✅ CA=true explicitly

# export the certificate as both cer and pfx file
Export-Certificate -Cert $rootCert -FilePath "$certPath\rootCA.cer"
Export-PfxCertificate -Cert $rootCert -FilePath "$certPath\rootCA.pfx" -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)

```
Add it to the trusted store
```
Import-Certificate -FilePath ".\security\certs\rootCA.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```
After this we are generating the certificates for broker and worker with `Signer` as `$rootCert`

```
# ============================
# 🔵 2. Create Broker (Server) Cert
# ============================
$brokerCert = New-SelfSignedCertificate `
    -Subject "CN=broker" `
    -DnsName "localhost" `
    -Signer $rootCert `
    -KeyExportPolicy Exportable `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -FriendlyName "MyBroker" `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension @(
        "2.5.29.19={text}CA=false", # Basic Constraints
        "2.5.29.37={text}1.3.6.1.5.5.7.3.1" # EKU: Server Authentication
    )


Export-Certificate -Cert $brokerCert -FilePath "$certPath\broker.cer"
Export-PfxCertificate -Cert $brokerCert -FilePath "$certPath\broker.pfx" -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)

# ============================
# 🔴 3. Create Worker (Client) Cert
# ============================
$workerCert = New-SelfSignedCertificate `
    -Subject "CN=worker" `
    -Signer $rootCert `
    -KeyExportPolicy Exportable `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -FriendlyName "MyWorker" `
    -NotAfter (Get-Date).AddYears(5) `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension @(
        "2.5.29.19={text}CA=false", # Basic Constraints
        "2.5.29.37={text}1.3.6.1.5.5.7.3.2" # EKU: Client Authentication
    )


Export-Certificate -Cert $workerCert -FilePath "$certPath\worker.cer"
Export-PfxCertificate -Cert $workerCert -FilePath "$certPath\worker.pfx" -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)
```
## Resource
[SelfSignCertificate Documentation](https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate?view=windowsserver2025-ps)

## Point of Concerns
- Need to figure out better format to export the certificates i.e pem file or something.
- Need to figure out how to automate this where for broker the `DnsName` is picked depending on the machine it is getting installed
- Automation script for trusting the CA in all the required machines or vm
- If possible use OpenSSL

# Broker code (Server side certificate)   
## Configure Kestrel to load the certificate
```
        var certificate = new X509Certificate2(
            @"..\..\security\certs\broker.pfx",
            "password",
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
        );
        // Add the certificate to the HTTPS configuration
        https.ServerCertificate = certificate;
```
### Points of Concern
-  Load certificate more securely
- Probably use env variables or encrypted store to load the paths
- Ditch pfx file as this gives security warning
- Using system's `MachineKeySet` to store the private key
    - Requires admin privileges
    - Storing it in machine store means it is available for all the users
- Require further discussion

## Add validation for incoming client certificate
```
        https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        https.ClientCertificateValidation = (certificate, chain, errors) =>
        {
            Console.WriteLine($"Validating client certificate: {certificate.Subject}");
            var rootCertificate = new X509Certificate2(
                @"..\..\security\certs\rootCA.cer"
            );

            chain.ChainPolicy.ExtraStore.Add(rootCertificate);
            
            // Currently not checking revocation, needs to figure out how to do it properly offline
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
```
# Worker(Client) side configuration

## Configure http client to embed client certificate
```

            var certificate = new X509Certificate2(
                @"..\..\security\certs\worker.pfx",
                "password",
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
            );

            handler.ClientCertificates.Add(certificate);
```
## Define a server validation custom callback
```

            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                Console.WriteLine($"In ServerCertifacteCustomCallback: {cert?.Subject}");
                // Accept all certificates for demonstration purposes
                // Define custom delegates to validate the server certificate
                return true;
            };
```

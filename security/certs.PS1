# ============================
# 🟢 1. Create Root CA
# ============================
$certPath = "$PSScriptRoot\certs"
New-Item -ItemType Directory -Force -Path $certPath | Out-Null

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

Export-Certificate -Cert $rootCert -FilePath "$certPath\rootCA.cer"
Export-PfxCertificate -Cert $rootCert -FilePath "$certPath\rootCA.pfx" -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)


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

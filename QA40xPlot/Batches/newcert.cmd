#run from admin console
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=YourName" -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -CertStoreLocation "Cert:\CurrentUser\My"
# Export the certificate to a .pfx file (which includes the private key):
$certPath = "Cert:\CurrentUser\My\$($cert.Thumbprint)"
Export-PfxCertificate -Cert $certPath -FilePath "C:\Path\To\YourCertificate.pfx" -Password (ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText)
# Export the certificate to a .cer file (which does not include the private key):
Export-Certificate -Cert $certPath -FilePath "C:\Path\To\YourCertificate.cer"
# use it
Set-AuthenticodeSignature -FilePath "C:\Path\To\YourScript.ps1" -Certificate $cert

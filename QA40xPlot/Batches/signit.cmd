$certificatePassword = ConvertTo-SecureString -String "MyPassword" -Force -AsPlainText
$certificatePath = "pathtopfx"
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$certificate.Import($certificatePath, $certificatePassword, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
$msiFilePath="pathtomsi"
Set-AuthenticodeSignature -FilePath $msiFilePath -Certificate $certificate

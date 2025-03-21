$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert
Set-AuthenticodeSignature -HashAlgorithm sha256 -TimeStampServer "http://timestamp.digicert.com" .\QA40xPlotSetup\Release\QA40xPlotSetup.msi $cert


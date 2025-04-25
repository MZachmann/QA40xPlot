# this signs the MSI file output
$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert
Set-AuthenticodeSignature -HashAlgorithm sha256 -TimeStampServer "http://timestamp.digicert.com" .\QA40xPlotSetup\Release\QA40xPlotSetup.msi $cert
compress-archive .\QA40xPlotSetup\Release\QA40xPlotSetup.msi .\QA40xPlotSetup\Release\QA40xPlotSetup.zip

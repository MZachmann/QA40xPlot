note that makecert is here->"\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x86\makecert.exe" 
to find it run the Developer PowerShell view then 
>whereis makecert

will show the full path

To sign the packaging first create a key file and cert file and then embed a certificate
in the root of your personal certificate store

>makecert -n "CN=mycnname,E=mail.email.com,C=US,S=NY" -a sha256 -eku 1.3.6.1.5.5.7.3.3 -r -sv \build\mzroot.pvk \build\mzroot.cer -ss Root -sr localMachine

This will create a certificate in the root and also place files in \build containing the key and cert.
Next create a certificate for code signing using the first cert as a root 
and put it into personal certificates

>makecert -pe -n "CN=mycnname2,E=mail.email.com,C=US,S=NY" -ss MY -a sha256 -eku 1.3.6.1.5.5.7.3.3 -iv \build\mzroot.pvk -ic \build\mzroot.cer

now we can rely on powershell stuff to get the right certificate and use it to sign the msi

>$cert = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert

>Set-AuthenticodeSignature -HashAlgorithm sha256 -TimeStampServer "http://timestamp.digicert.com" .\QA40xPlotSetup\Release\QA40xPlotSetup.msi $cert

It should be noted that the root certificate should be created as administrator and the personal certificate as
the user with whom you need to sign the apps

# Funzione per selezionare una cartella tramite finestra grafica
function Scegli-Cartella {
Add-Type -AssemblyName System.Windows.Forms
$dialog = New-Object System.Windows.Forms.FolderBrowserDialog
if ($dialog.ShowDialog() -eq "OK") {
return $dialog.SelectedPath
}
else {
throw "Nessuna cartella selezionata."
}
}
# Richiedi il nome del certificato
$nomeCertificato = Read-Host "Inserisci il nome (DNS Name) del certificato (es:
miosito.local)"
# Richiedi la password per il file .pfx (mascherata)
$passwordPfxSecure = Read-Host "Inserisci la password per proteggere il file
.pfx" -AsSecureString
# Seleziona la cartella di output
try {
$cartellaOutput = Scegli-Cartella
Write-Host "Cartella selezionata:" $cartellaOutput
} catch {
Write-Host $_.Exception.Message
exit
}
# Crea il certificato autofirmato nello store della macchina locale
$cert = New-SelfSignedCertificate `
-DnsName $nomeCertificato `
-CertStoreLocation "cert:\LocalMachine\My"
Write-Host "Certificato creato con Subject: $($cert.Subject)"
# Percorsi dei file da esportare
$pfxPath = Join-Path $cartellaOutput "$nomeCertificato.pfx"
$cerPath = Join-Path $cartellaOutput "$nomeCertificato.cer"
# Esporta il certificato + chiave privata (.pfx)
Export-PfxCertificate -Cert $cert.PSPath -FilePath $pfxPath -Password
$passwordPfxSecure
Write-Host "File .pfx esportato in: $pfxPath"
# Esporta solo il certificato pubblico (.cer)
Export-Certificate -Cert $cert.PSPath -FilePath $cerPath
Write-Host "File .cer esportato in: $cerPath"
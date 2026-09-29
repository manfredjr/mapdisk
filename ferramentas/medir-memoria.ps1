# Mede o pico de memoria de uma varredura pela linha de comando.
# Uso: powershell -File ferramentas\medir-memoria.ps1 C:
# O resultado fica so na tela. Nao grave em arquivo do repositorio.
param([Parameter(Mandatory = $true)][string]$Alvo)
$exe = Join-Path $PSScriptRoot '..\publicar\mapdisk.exe'
$saida = Join-Path $PSScriptRoot '..\.superpowers\rascunho\medir-memoria.txt'
$p = Start-Process -FilePath $exe -ArgumentList 'varrer', $Alvo -RedirectStandardOutput $saida -PassThru -WindowStyle Hidden
$pico = 0
while (-not $p.HasExited) {
    try { $p.Refresh(); if ($p.PeakWorkingSet64 -gt $pico) { $pico = $p.PeakWorkingSet64 } } catch { }
    Start-Sleep -Milliseconds 200
}
"Pico de memoria: {0:N0} MB" -f ($pico / 1MB)

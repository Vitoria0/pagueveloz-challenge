# Script de limpeza para remover informações pessoais antes do commit

Write-Host "Limpando informações pessoais do projeto..." -ForegroundColor Yellow

# Remover arquivos de log
Write-Host "Removendo arquivos de log..." -ForegroundColor Cyan
Get-ChildItem -Path . -Recurse -Include "*.log" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path . -Recurse -Directory -Filter "logs" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Remover arquivos de build e binários
Write-Host "Removendo arquivos de build..." -ForegroundColor Cyan
Get-ChildItem -Path . -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path . -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Remover arquivos temporários do Visual Studio
Write-Host "Removendo arquivos temporários..." -ForegroundColor Cyan
Get-ChildItem -Path . -Recurse -Include "*.user", "*.suo", "*.userosscache", "*.sln.docstates" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

# Remover arquivos de cache do NuGet
Write-Host "Removendo cache do NuGet..." -ForegroundColor Cyan
Get-ChildItem -Path . -Recurse -Directory -Filter ".nuget" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Limpar arquivos de configuração do Git (se existirem)
Write-Host "Verificando configurações do Git..." -ForegroundColor Cyan
if (Test-Path ".git\config") {
    $config = Get-Content ".git\config" -Raw
    if ($config -match "user\.name|user\.email") {
        Write-Host "AVISO: Arquivo .git/config contém informações pessoais. Revise manualmente." -ForegroundColor Red
    }
}

Write-Host "`nLimpeza concluída!" -ForegroundColor Green
Write-Host "`nIMPORTANTE: Revise os seguintes arquivos antes do commit:" -ForegroundColor Yellow
Write-Host "  - README.md" -ForegroundColor White
Write-Host "  - .git/config (se existir)" -ForegroundColor White
Write-Host "  - Qualquer arquivo de configuração que possa conter caminhos pessoais" -ForegroundColor White


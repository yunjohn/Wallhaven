param([switch]$LiveTests)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet build Wallhaven.slnx -c Release -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    if ($LiveTests) { dotnet run --project tests/Wallhaven.Tests -c Release --no-build -- --live }
    else { dotnet run --project tests/Wallhaven.Tests -c Release --no-build }
    if ($LASTEXITCODE -ne 0) { throw 'Checks failed' }
    dotnet run --project tests/Wallhaven.UiTests -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'WPF UI checks failed' }
    dotnet publish src/Wallhaven.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    New-Item -ItemType Directory -Force -Path dist | Out-Null
    Copy-Item -LiteralPath artifacts/publish/Wallhaven.exe -Destination dist/Wallhaven.exe -Force
    Copy-Item -LiteralPath USER-GUIDE.md -Destination dist/使用说明.md -Force
    $releaseHash = Get-FileHash -LiteralPath dist/Wallhaven.exe -Algorithm SHA256
    "$($releaseHash.Hash)  Wallhaven.exe" | Set-Content -LiteralPath dist/SHA256.txt -Encoding utf8
    $releaseVersion = ([xml](Get-Content -LiteralPath src/Wallhaven.App/Wallhaven.App.csproj -Raw)).Project.PropertyGroup.Version
    Compress-Archive -Path dist/Wallhaven.exe,dist/使用说明.md,dist/SHA256.txt -DestinationPath "dist/Wallhaven-$releaseVersion-win-x64.zip" -Force
    Get-FileHash -LiteralPath dist/Wallhaven.exe -Algorithm SHA256 | Format-List
} finally { Pop-Location }

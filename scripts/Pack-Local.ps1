./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath .\DG.XrmPluginCore\CHANGELOG.md -CsprojPath .\DG.XrmPluginCore\DG.XrmPluginCore.csproj
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath .\DG.XrmPluginCore.Abstractions\CHANGELOG.md -CsprojPath .\DG.XrmPluginCore.Abstractions\DG.XrmPluginCore.Abstractions.csproj

dotnet build --configuration Release
dotnet pack

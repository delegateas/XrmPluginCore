./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath .\XrmPluginCore\CHANGELOG.md -CsprojPath .\XrmPluginCore\XrmPluginCore.csproj
./scripts/Set-VersionFromChangelog.ps1 -ChangelogPath .\XrmPluginCore.Abstractions\CHANGELOG.md -CsprojPath .\XrmPluginCore.Abstractions\XrmPluginCore.Abstractions.csproj

dotnet build --configuration Release
dotnet pack

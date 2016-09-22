# Delete old versions.
Remove-Item *.nupkg

# Create new versions.
Foreach ($spec in $(Get-Item *.nuspec))
{
    .\NuGet.exe pack "$spec"
}
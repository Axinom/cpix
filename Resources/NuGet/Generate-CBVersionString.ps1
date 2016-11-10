$ErrorActionPreference = "Stop"

$ns = @{
	spec = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"
}

$versionNode = Select-Xml -Xpath "/spec:package/spec:metadata/spec:version" -Path "Cpix.nuspec" -Namespace $ns
$version = $versionNode.Node.InnerText

Write-Host "NuSpec version is $version"

# If there is already some -pre01 or whatever suffix, get rid of it.
$version = ($version -split "-")[0]

Write-Host "Cleaned version is $version"

$suffix = "-cb-" + $(Get-Date -Format "yyyyMMdd-HHmmss")

$version = $version + $suffix

Write-Host "CB version is $version"

Write-Host "##vso[task.setvariable variable=CbNugetPackageVersion;]$version"
param (
	[string]$server = $( Read-Host "NuGet server URL" ),
	[string]$apiKey = $( Read-Host "API key" )
)

if (!$server) { throw "You must provide the server URL" }
if (!$apiKey) { throw "You must provide the API key" }

$review = $False

Foreach ($package in $(Get-Item *.nupkg))
{
    .\NuGet.exe push "$package" "$apiKey" -s "$server"
	
	if (!$?) { $review = $True }
}

if ($review)
{
	Write-Host "Some errors occurred during publishing - please review before existing."
	Read-Host "Press enter to exit"
}
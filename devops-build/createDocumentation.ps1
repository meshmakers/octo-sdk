param ($configuration = "Release")

dotnet tool update --global MMXMLDoc2Markdown

$modulePath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$baseBinPath = Join-Path $modulePath "../src"
$baseOutputPath = Join-Path $modulePath "../bin/$configuration/documentation"

# Clean directory
if (Test-Path -Path $baseOutputPath) {
    Write-Host "Remove existing documentation at '$baseOutputPath'"
    Remove-Item -Path $baseOutputPath -Recurse -Force 
}

# Create XML documentation for Libraries
$outputPath = "$baseOutputPath/apiReference/Communication.Contracts"
$sourcePath = "$baseBinPath/Communication.Contracts/bin/$configuration/net8.0/Meshmakers.Octo.Communication.Contracts.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath

$outputPath = "$baseOutputPath/apiReference/Sdk.Common"
$sourcePath = "$baseBinPath/Sdk.Common/bin/$configuration/net8.0/Meshmakers.Octo.Sdk.Common.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath

$outputPath = "$baseOutputPath/apiReference/Sdk.Common.Web"
$sourcePath = "$baseBinPath/Sdk.Common.Web/bin/$configuration/net8.0/Meshmakers.Octo.Sdk.Common.Web.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath

$outputPath = "$baseOutputPath/apiReference/Sdk.ServiceClient"
$sourcePath = "$baseBinPath/Sdk.ServiceClient/bin/$configuration/net8.0/Meshmakers.Octo.Sdk.ServiceClient.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath
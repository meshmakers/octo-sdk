param ($configuration = "Release", $frameworkVersion = "net10.0")

# Old package (MMXMLDoc2Markdown) and the renamed one both provide the 'mmxmldoc2md' command;
# remove the old one first so the update below doesn't conflict on a reused build agent.
# Only uninstall when it is actually present: an unconditional 'dotnet tool uninstall' exits 1
# and writes to stderr when the tool is missing, which Windows PowerShell turns into a
# terminating error under the pipeline's ErrorActionPreference=Stop ('2>$null' does not suppress
# this for native commands) - failing the step on every agent that no longer has the old package.
if (dotnet tool list --global | Select-String -Quiet 'mmxmldoc2markdown') {
    dotnet tool uninstall --global MMXMLDoc2Markdown
}
dotnet tool update --global Meshmakers.XMLDoc2Markdown

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
$sourcePath = "$baseBinPath/Communication.Contracts/bin/$configuration/$frameworkVersion/Meshmakers.Octo.Communication.Contracts.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath

$outputPath = "$baseOutputPath/apiReference/Sdk.Common"
$sourcePath = "$baseBinPath/Sdk.Common/bin/$configuration/$frameworkVersion/Meshmakers.Octo.Sdk.Common.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath

$outputPath = "$baseOutputPath/apiReference/Sdk.Common.Web"
$sourcePath = "$baseBinPath/Sdk.Common.Web/bin/$configuration/$frameworkVersion/Meshmakers.Octo.Sdk.Common.Web.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath

$outputPath = "$baseOutputPath/apiReference/Sdk.ServiceClient"
$sourcePath = "$baseBinPath/Sdk.ServiceClient/bin/$configuration/$frameworkVersion/Meshmakers.Octo.Sdk.ServiceClient.dll"
Write-Host "Creating documentation for $sourcePath, doc is generated at $outputPath"
mmxmldoc2md $sourcePath $outputPath
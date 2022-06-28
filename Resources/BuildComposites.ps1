# Powershell Script to generate Windows composites.

[CmdletBinding(PositionalBinding=$false)]
Param(
    [switch]$AspNetComposite = $false,
    [switch]$BundleAspNet = $false,
    [string]$CompositesType = "0",
    [string]$DotnetVersionNumber = "7.0",
    [switch]$FrameworkComposite = $false,
    [string]$PartialAspnetComposites = "0",
    [string]$PartialFrameworkComposites = "0",
    [switch]$UseAvx2 = $false
)

function Set-BuildEnvironment
{
    $Env:ASPNET_COMPOSITE = $AspNetComposite
    $Env:BUNDLE_ASPNET = $BundleAspNet
    $Env:COMPOSITES_TYPE = $CompositesType
    $Env:DOTNET_VERSION_NUMBER = $DotnetVersionNumber
    $Env:FRAMEWORK_COMPOSITE = $FrameworkComposite
    $Env:PARTIAL_ASPNET_COMPOSITES = $PartialAspnetComposites
    $Env:PARTIAL_FRAMEWORK_COMPOSITES = $PartialFrameworkComposites
    $Env:USE_AVX2 = $UseAvx2
}

$ResourcesDir = $PSScriptRoot

Set-BuildEnvironment
Set-Location "$ResourcesDir/BuildEngine"
Invoke-Expression -Command 'dotnet build BuildEngine.csproj -c Release'

$BuildEnginePath = Get-ChildItem -Path "$ResourcesDir/BuildEngine/bin" `
                                 -Recurse                              `
                                 -Filter 'BuildEngine.exe'             `
                 | Select-Object FullName

$BuildEngineExe = $BuildEnginePath.FullName
Write-Host $BuildEngineExe
& "$BuildEngineExe" $ResourcesDir

# Powershell Script to generate Windows composites.

[CmdletBinding(PositionalBinding=$false)]
Param(
    [switch]$AspNetComposite,
    [switch]$BundleAspNet,
    [string]$CompositesType,
    [string]$DotnetVersionNumber,
    [switch]$FrameworkComposite,
    [switch]$UseAvx2
)

$BasePath = $PSScriptRoot
$OutputDir = "$BasePath/win-output-$CompositesType"

if (-Not (Test-Path -Path $OutputDir))
{
    New-Item -Path $OutputDir -ItemType Directory
}

$DotnetPath = "$BasePath/DotnetWindows/dotnet$DotnetVersionNumber"

$CoreLibPath = Get-ChildItem -Path "$DotnetPath/shared"           `
                             -Recurse                             `
                             -Filter 'System.Private.CoreLib.dll' `
             | Select-Object FullName

$AspNetPath = Get-ChildItem -Path "$DotnetPath/shared"         `
                            -Recurse                           `
                            -Filter 'Microsoft.AspNetCore.dll' `
            | Select-Object FullName

$Crossgen2Path = Get-ChildItem -Path "$BasePath"       `
                               -Recurse                `
                               -Filter 'crossgen2.exe' `
               | Select-Object FullName

$FxPath = $CoreLibPath.FullName -Replace '\\[^\\]+$'
$AspPath = $AspNetPath.FullName -Replace '\\[^\\]+$'
$Crossgen2Exe = $Crossgen2Path.FullName
$Crossgen2Path = Split-Path -Path $Crossgen2Path.FullName

$Crossgen2Args = [System.Collections.Generic.List[string]]::new()
$Crossgen2Args.Add('--targetos=Windows')
$Crossgen2Args.Add('--targetarch=x64')

if ($UseAvx2)
{
    Write-Host 'Will apply AVX2 Instruction Set...'
    $Crossgen2Args.Add('--instruction-set:avx2')
}

if (Test-Path -Path "$Crossgen2Path/StandardOptimizationData.mibc")
{
    Write-Host "Using StandardOptimizationData.mibc..."
    $Crossgen2Args.Add("--mibc=$Crossgen2Path/StandardOptimizationData.mibc")
}

if ($FrameworkComposite)
{
    Write-Host 'Compiling Framework Composites...'
    $CompositeFile = 'framework'

    $FxCompositeArgs = [System.Collections.Generic.List[string]]::new($Crossgen2Args)
    $FxCompositeArgs.Add('--composite')
    $FxCompositeArgs.Add("$FxPath/*.dll")

    if ($BundleAspNet)
    {
        Write-Host 'ASP.NET will be bundled into the composite image...'
        $FxCompositeArgs.Add("$AspPath/*.dll")
        $CompositeFile += '-aspnet'
    }

    $FxCompositeArgs.Add("--out=$OutputDir/$CompositeFile.r2r.dll")
    & "$Crossgen2Exe" $fxCompositeArgs.ToArray()
}
else
{
    # Once we figure out how it works, here goes the non-composites processing.
}

if ($AspNetComposite -And (-Not $BundleAspNet))
{
    Write-Host 'Compiling ASP.NET Framework Composites...'
    $AspCompositeArgs = [System.Collections.Generic.List[string]]::new($Crossgen2Args)
    $AspCompositeArgs.Add('--composite')
    $AspCompositeArgs.Add("$AspPath/*.dll")
    $AspCompositeArgs.Add("--reference=$FxPath/*.dll")
    $AspCompositeArgs.Add("--out=$OutputDir/aspnetcore.r2r.dll")
    & "$Crossgen2Exe" $AspCompositeArgs.ToArray()
}

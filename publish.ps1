param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,

    [Parameter(Mandatory)]
    [System.String]$ProjectPath,
    
    [System.String]$DeployPath
)

if ($DeployPath.Equals("") -Or $DeployPath.Equals("Build")){
    Write-Host "Fix DeployPath"
    $DeployPath = "$ValheimPath\BepInEx\plugins"
}

$PrePackagePath = "$ProjectPath\Prepackage"

Write-Host "Target:          $Target"
Write-Host "TargetPath:      $TargetPath"
Write-Host "TargetAssembly:  $TargetAssembly"
Write-Host "ValheimPath:     $ValheimPath"
Write-Host "ProjectPath:     $ProjectPath"
Write-Host "DeployPath:      $DeployPath"
Write-Host "PrePackagePath:  $PrePackagePath"

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ directory is missing"}
}

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

# Create the mdb file
$pdb = "$TargetPath\$name.pdb"
if (Test-Path -Path "$pdb") {
    Write-Host "Create mdb file for plugin $name"
    Invoke-Expression "& `"$(Get-Location)\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

if ($Target.Equals("Debug")) 
{
    $PluginPath = New-Item -Type Directory -Path "$DeployPath\$name" -Force
    Write-Host "Copy $TargetAssembly to $PluginPath"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$PluginPath" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$PluginPath" -Force
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$PluginPath" -Force
    Copy-Item -Path "$PrePackagePath\Translations" -Destination "$PluginPath\" -Recurse -Force
}

if($Target.Equals("Release")) 
{
    Write-Host "Packaging for ThunderStore..."
    $Package="Package"
    $PackagePath="$ProjectPath\$Package"

    Write-Host "$PackagePath\$TargetAssembly"
    $PackagePluginsPath = New-Item -Type Directory -Path "$PackagePath\plugins\$name" -Force
    $PackageReleasePath = New-Item -Type Directory -Path "$ProjectPath\Release" -Force
    

    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$PackagePluginsPath\" -Force
    Copy-Item -Path "$PrePackagePath\Translations" -Destination "$PackagePluginsPath\" -Recurse -Force
    Copy-Item -Path "$PrePackagePath\README.md" -Destination "$PackagePath\" -Force
    Copy-Item -Path "$PrePackagePath\manifest.json" -Destination "$PackagePath\" -Force
    Copy-Item -Path "$ProjectPath\..\Images\icon.png" -Destination "$PackagePath\" -Force
    
    $CompressedOutputFilename = "$PackageReleasePath\$name-new.zip"
    Compress-Archive -Path "$PackagePath\*" -DestinationPath "$CompressedOutputFilename" -Force
    Write-Host "Package ready: $CompressedOutputFilename"
}

# Pop Location
Pop-Location
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

########
# You need to run the following command as admin, only once.
# It installs a module that fixes a bug with Compress-Archive using backward slashes as path separators, which should be forward slashes.
# https://github.com/PowerShell/Microsoft.PowerShell.Archive/issues/48#issuecomment-491968156
#
#   Install-Module Microsoft.PowerShell.Archive -MinimumVersion 1.2.3.0 -Repository PSGallery -Force
########

if ([version]$(Get-Module -ListAvailable Microsoft.PowerShell.Archive | Sort-Object Version -Descending  | Select-Object Version -First 1  | Select-Object @{n='ModuleVersion'; e={$_.Version -as [string]}} | Select-Object ModuleVersion -ExpandProperty ModuleVersion) -lt [version]"1.2.3.0")
{
    Write-Error -ErrorAction Stop -Message "Microsoft.PowerShell.Archive is not the correct version. Read the comments in publish.ps1 to fix this."
}

if ($DeployPath.Equals("") -Or $DeployPath.Equals("Build")){
    Write-Host "Fix DeployPath"
    $DeployPath = "$ValheimPath\BepInEx\plugins"
}

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Add some more locations
$SolutionPath = $(Get-Location)
$PackagePath = New-Item -Type Directory -Path "$SolutionPath\Package\$Target" -Force
$ReleasePath = New-Item -Type Directory -Path "$SolutionPath\Release" -Force
$DocsPath = "$SolutionPath\Docs"
$PrePackagePath = "$ProjectPath\Prepackage"

# Print an overview of variables
Write-Host "Target:             $Target"
Write-Host "TargetPath:         $TargetPath"
Write-Host "TargetAssembly:     $TargetAssembly"
Write-Host "ValheimPath:        $ValheimPath"
Write-Host "ProjectPath:        $ProjectPath"
Write-Host "DeployPath:         $DeployPath"
Write-Host "SolutionPath:       $SolutionPath"
Write-Host "PackagePath:        $PackagePath"
Write-Host "ReleasePath:        $ReleasePath"
Write-Host "DocsPath:           $DocsPath"
Write-Host "PrePackagePath:     $PrePackagePath"

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
    Invoke-Expression "& `"$SolutionPath\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
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

    Write-Host "Packaging debug release..."
    
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$PackagePath\" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$PackagePath\" -Force
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$PackagePath\" -Force

    $CompressedOutputFilename = "$ReleasePath\$name-debug-new.zip"
    Compress-Archive -Path "$PackagePath\*" -DestinationPath "$CompressedOutputFilename" -Force
    Write-Host "Debug package ready: $CompressedOutputFilename"
}

if($Target.Equals("Release")) 
{
    Write-Host "Copying SolutionDir docs... "
    Copy-Item -Path "$DocsPath\SolutionDir\*" -Destination "$SolutionPath\" -Exclude @("*.tt") -Recurse -Force

    Write-Host "Packaging for ThunderStore..."
    $PackagePluginPath = New-Item -Type Directory -Path "$PackagePath\plugins\$name" -Force
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$PackagePluginPath\" -Force
    Copy-Item -Path "$PrePackagePath\Translations" -Destination "$PackagePluginPath\" -Recurse -Force
    Copy-Item -Path "$SolutionPath\Images\icon.png" -Destination "$PackagePath\" -Force
    
    $CompressedOutputFilename = "$ReleasePath\$name-new.zip"
    Compress-Archive -Path "$PackagePath\*" -DestinationPath "$CompressedOutputFilename" -Force
    Write-Host "Package ready: $CompressedOutputFilename"
}

# Pop Location
Pop-Location
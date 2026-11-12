#Requires -Version 7.2
[CmdletBinding()]
param([string]$OutputDirectory='',[string]$DotnetPath='dotnet',[switch]$SkipChecks)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(!$OutputDirectory){$OutputDirectory=Join-Path $taskRoot 'dist/rog-kb-rgb-studio-full'}
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)
$taskLiteOutput=$taskOutput -replace '-full$','-lite'
if($taskLiteOutput -eq $taskOutput){$taskLiteOutput=$taskOutput+'-lite'}
$taskDist=[IO.Path]::GetFullPath((Join-Path $taskRoot 'dist'))
if(!$taskOutput.StartsWith($taskDist+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be a new directory under this checkout/dist.'}
$taskFullZip=Join-Path $taskDist 'ROG-KB-RGB-Studio-win-x64-full.zip'
$taskLiteZip=Join-Path $taskDist 'ROG-KB-RGB-Studio-win-x64-lite.zip'
$taskSourceZip=Join-Path $taskDist 'ROG-KB-RGB-Studio-source.zip'
$taskHashes=Join-Path $taskDist 'SHA256SUMS.txt'
foreach($taskNewPath in @($taskOutput,$taskLiteOutput,$taskFullZip,$taskLiteZip,$taskSourceZip,$taskHashes)){
 if(Test-Path -LiteralPath $taskNewPath){throw "Output already exists; preserve it or choose a clean release destination: $taskNewPath"}
}
function Run-Dotnet([string[]]$Arguments){& $DotnetPath @Arguments;if($LASTEXITCODE -ne 0){throw "dotnet failed ($LASTEXITCODE): $($Arguments -join ' ')"}}
function Save-Licence([string]$Uri,[string]$Destination){
 for($taskAttempt=1;$taskAttempt -le 3;$taskAttempt++){
  try{Invoke-WebRequest -Uri $Uri -OutFile $Destination -TimeoutSec 30;return}
  catch{if($taskAttempt -eq 3){throw};Start-Sleep -Seconds 2}
 }
}
$taskSolution=Join-Path $taskRoot 'ROG-KB-RGB-Studio.slnx'
Run-Dotnet -Arguments @('restore',$taskSolution,'-r','win-x64','-p:SelfContained=true','-p:PublishAot=false','-p:NuGetAudit=true','-p:NuGetAuditMode=all')
$taskAuditText=(& $DotnetPath list $taskSolution package --vulnerable --include-transitive --no-restore --format json) -join "`n"
if($LASTEXITCODE -ne 0){throw 'Dependency vulnerability check failed.'}
$taskAudit=$taskAuditText | ConvertFrom-Json
foreach($taskProject in $taskAudit.projects){foreach($taskFramework in $taskProject.frameworks){foreach($taskDependency in @($taskFramework.topLevelPackages)+@($taskFramework.transitivePackages)){if($taskDependency.vulnerabilities){throw "Known vulnerable package blocks publication: $($taskDependency.id) $($taskDependency.resolvedVersion)"}}}}
if(!$SkipChecks){
 Run-Dotnet -Arguments @('build',$taskSolution,'-t:Rebuild','-c','Release','--no-restore','-m:1','-p:PublishAot=false','-p:SkipHelperCopy=true')
 Run-Dotnet -Arguments @('test',$taskSolution,'-c','Release','--no-build','--no-restore')
}
foreach($taskProject in @('RSS_II_RGB.App','RSS_II_RGB.SensorsHost','RSS_II_RGB.GifBakeHost')){
 $taskDestination=switch($taskProject){'RSS_II_RGB.SensorsHost'{Join-Path $taskOutput 'sensorshost'}'RSS_II_RGB.GifBakeHost'{Join-Path $taskOutput 'gifbakehost'}default{$taskOutput}}
 Run-Dotnet -Arguments @('publish',(Join-Path $taskRoot "src/$taskProject/$taskProject.csproj"),'-c','Release','-r','win-x64','--self-contained','true','--no-restore','-p:PublishAot=false','-p:PublishTrimmed=false','-p:SkipHelperCopy=true','-o',$taskDestination)
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination $taskOutput
foreach($taskDocument in @('README.md','README.en.md')){Copy-Item -LiteralPath (Join-Path $taskRoot $taskDocument) -Destination $taskOutput}
Copy-Item -LiteralPath (Join-Path $taskRoot 'NOTICE.md') -Destination $taskOutput
Set-Content -LiteralPath (Join-Path $taskOutput 'Start-Studio.cmd') -Encoding ascii -Value '@echo off', 'start "" "%~dp0RSS_II_RGB.App.exe" --experimental-falchion75'
# Copy before generating notices so Lite receives only its own dependency licences.
$null=New-Item -ItemType Directory -Path $taskLiteOutput
Get-ChildItem -LiteralPath $taskOutput -Force | Where-Object {$_.Name -ne 'gifbakehost'} | Copy-Item -Destination $taskLiteOutput -Recurse
function Write-PackageNotices([string]$taskOutput){
$taskLicenses=Join-Path $taskOutput 'licenses';$null=New-Item -ItemType Directory -Path $taskLicenses
$taskPackages=@{}
foreach($taskDeps in Get-ChildItem -LiteralPath $taskOutput -Filter '*.deps.json' -File -Recurse){
 $taskJson=Get-Content -LiteralPath $taskDeps.FullName -Raw | ConvertFrom-Json
 foreach($taskLibrary in $taskJson.libraries.PSObject.Properties){if($taskLibrary.Value.type -in @('package','runtimepack')){$taskPackages[($taskLibrary.Name -replace '^runtimepack\.','')]=$true}}
}
$taskCache=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'}
$taskNotice=[Collections.Generic.List[string]]::new();$taskNotice.Add('# Bundled third-party packages');$taskNotice.Add('Generated from the published dependency manifests. The software retains its original licences and copyright notices. See licenses/ for full texts and package notices.')
$taskExpressions=@{}
foreach($taskPackage in $taskPackages.Keys | Sort-Object){
 $taskId,$taskVersion=$taskPackage -split '/',2
 $taskDirectory=Join-Path $taskCache "$($taskId.ToLowerInvariant())/$($taskVersion.ToLowerInvariant())"
 $taskSpec=Get-ChildItem -LiteralPath $taskDirectory -Filter '*.nuspec' -File | Select-Object -First 1
 if(!$taskSpec){throw "Package metadata missing: $taskPackage"}
 [xml]$taskXml=Get-Content -LiteralPath $taskSpec.FullName -Raw
 $taskLicence=$taskXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='license']")
 $taskCopyright=$taskXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='copyright']")
 $taskAuthors=$taskXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='authors']")
 $taskNotice.Add("`n## $taskPackage`nLicence: $($taskLicence.InnerText)`nAuthors: $($taskAuthors.InnerText)`n$($taskCopyright.InnerText)`nPackage: https://www.nuget.org/packages/$taskId/$taskVersion")
 if($taskLicence.type -eq 'expression'){
  foreach($taskMatch in [regex]::Matches($taskLicence.InnerText,'[A-Za-z0-9][A-Za-z0-9.+-]*')){if($taskMatch.Value -notin @('AND','OR','WITH')){$taskExpressions[$taskMatch.Value]=$true}}
 }
 foreach($taskText in Get-ChildItem -LiteralPath $taskDirectory -File -Recurse | Where-Object {$_.Name -match '^(?i:licen[cs]e|notice|copyright|copying|third.party)' -and $_.Extension -in @('.txt','.md','.html','')}){
  $taskRelative=[IO.Path]::GetRelativePath($taskDirectory,$taskText.FullName)
  $taskTarget=Join-Path $taskLicenses "$taskId-$taskVersion/$taskRelative"
  $null=New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($taskTarget)) -Force
  Copy-Item -LiteralPath $taskText.FullName -Destination $taskTarget
 }
}
foreach($taskExpression in $taskExpressions.Keys){
 $taskLicenceFile=Join-Path $taskLicenses "$taskExpression.txt"
 Save-Licence "https://raw.githubusercontent.com/spdx/license-list-data/main/text/$taskExpression.txt" $taskLicenceFile
}
# Inter's font licence is separate from the Avalonia wrapper's MIT licence.
Save-Licence 'https://raw.githubusercontent.com/rsms/inter/master/LICENSE.txt' (Join-Path $taskLicenses 'Inter-OFL.txt')
$taskNotice | Set-Content -LiteralPath (Join-Path $taskOutput 'THIRD-PARTY-NOTICES.md') -Encoding utf8
}
foreach($taskVariant in @($taskOutput,$taskLiteOutput)){
 Write-PackageNotices $taskVariant
$taskForbidden=Get-ChildItem -LiteralPath $taskVariant -File -Recurse | Where-Object {$_.Name -like 'settings*.json' -or $_.Extension -in @('.gif','.kbanim','.log')}
if($taskForbidden){throw 'Personal/cache files unexpectedly present in release.'}
foreach($taskComponent in @('RSS_II_RGB.App','sensorshost/RSS_II_RGB.SensorsHost')+$(if($taskVariant -eq $taskOutput){@('gifbakehost/RSS_II_RGB.GifBakeHost')}else{@()})){
 $taskRuntime=if($taskComponent.Contains('/')){([IO.Path]::GetDirectoryName($taskComponent))+'/coreclr.dll'}else{'coreclr.dll'}
 foreach($taskRequired in @("$taskComponent.exe","$taskComponent.deps.json","$taskComponent.runtimeconfig.json",$taskRuntime)){
  if(!(Test-Path -LiteralPath (Join-Path $taskVariant $taskRequired) -PathType Leaf)){throw "Required self-contained component missing: $taskVariant/$taskRequired"}
 }
}
if($taskVariant -eq $taskLiteOutput -and (Get-ChildItem -LiteralPath $taskVariant -Recurse -Force | Where-Object {$_.Name -match '(?i)gifbakehost|magick'})){throw 'Lite unexpectedly contains the GIF component or Magick dependencies.'}
}
Compress-Archive -LiteralPath $taskOutput -DestinationPath $taskFullZip -CompressionLevel Optimal
Compress-Archive -LiteralPath $taskLiteOutput -DestinationPath $taskLiteZip -CompressionLevel Optimal
Add-Type -AssemblyName System.IO.Compression
$taskArchive=[IO.Compression.ZipFile]::Open($taskSourceZip,[IO.Compression.ZipArchiveMode]::Create)
try{
 foreach($taskSource in Get-ChildItem -LiteralPath $taskRoot -Recurse -File -Force | Where-Object {
  $taskParts=[IO.Path]::GetRelativePath($taskRoot,$_.FullName) -split '[/\\]'
  !(@('bin','obj','dist','.git','.vs','.idea','docs') | Where-Object {$_ -in $taskParts}) -and $_.Name -ne 'CLAUDE.md' -and $_.Extension -notin @('.user','.suo','.log')
 }){
  $taskRelative=[IO.Path]::GetRelativePath($taskRoot,$taskSource.FullName).Replace('\','/')
  $null=[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($taskArchive,$taskSource.FullName,$taskRelative,[IO.Compression.CompressionLevel]::Optimal)
 }
}finally{$taskArchive.Dispose()}
Get-FileHash -LiteralPath $taskFullZip,$taskLiteZip,$taskSourceZip -Algorithm SHA256 | ForEach-Object {"$($_.Hash)  $([IO.Path]::GetFileName($_.Path))"} | Set-Content -LiteralPath $taskHashes -Encoding ascii
[pscustomobject]@{Full=$taskFullZip;Lite=$taskLiteZip;Source=$taskSourceZip;Hashes=$taskHashes}

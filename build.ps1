param (
    # path to dotnet executable,
    [string]$dotnet = 'C:\Program Files\dotnet\dotnet.exe',
    # path to Unity Editor version 2019.4.39.f1 executable.
    [string]$unity = 'C:\Program Files\Unity\Hub\Editor\2019.4.39f1\Editor\Unity.exe'
)

if (-Not (Test-Path -Path "$PSScriptRoot/build")) {
    New-Item -Path $PSScriptRoot -Name "build" -ItemType "directory" | Out-Null
}
if (-Not (Test-Path -Path "$PSScriptRoot/build/OWKT")) {
    New-Item -Path "$PSScriptRoot/build" -Name "OWKT" -ItemType "directory" | Out-Null
}
if (-Not (Test-Path -Path "$PSScriptRoot/build/OWKT/Assets")) {
    New-Item -Path "$PSScriptRoot/build/OWKT" -Name "Assets" -ItemType "directory" | Out-Null
}
if (-Not (Test-Path -Path "$PSScriptRoot/assetbundle/Assets/AssetBundle")) {
    New-Item -Path "$PSScriptRoot/assetbundle/Assets" -Name "AssetBundle" -ItemType "directory" | Out-Null
}

Start-Process -FilePath $dotnet -ArgumentList "build","`"$($PSScriptRoot)/mod/OWKT.sln`"","--configuration Release" -NoNewWindow -Wait
Start-Process -FilePath $unity -ArgumentList "-quit","-projectPath `"$($PSScriptRoot)/assetbundle`"","-batchmode","-nographics","-noUpm","-executeMethod Build.BuildAssetBundle" -NoNewWindow -Wait

$AssetBundlePath = "$PSScriptRoot/assetbundle/Assets/AssetBundle"
Copy-Item -Path "$AssetBundlePath/owkt","$AssetBundlePath/owkt.manifest" -Destination "$PSScriptRoot/build/OWKT/Assets"
$ModPath = "$PSScriptRoot/mod/OWKT/bin/Release"
Copy-Item -Path "$ModPath/default-config.json","$ModPath/manifest.json","$ModPath/OWKT.dll" -Destination "$PSScriptRoot/build/OWKT"

Compress-Archive -Force -Path "$PSScriptRoot/build/OWKT/*" -DestinationPath "$PSScriptRoot/build/OWKT.zip"
Remove-Item ..\QuickLook.Plugin.MarkmapViewer.qlplugin -ErrorAction SilentlyContinue

$files = Get-ChildItem -Path ..\QuickLook.Plugin\QuickLook.Plugin.MarkmapViewer\bin\Release\ -Exclude *.pdb,*.xml
Compress-Archive $files ..\QuickLook.Plugin.MarkmapViewer.zip
Move-Item ..\QuickLook.Plugin.MarkmapViewer.zip ..\QuickLook.Plugin.MarkmapViewer.qlplugin
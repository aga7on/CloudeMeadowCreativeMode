$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $here
try {
  $csc = 'C:\Program Files\dotnet\sdk\10.0.102\Roslyn\bincore\csc.dll'
  $refs = @(
    '/noconfig','/nostdlib+','/target:library','/optimize+','/deterministic+','/langversion:5','/out:bin\CloudMeadow.CreativeMode.dll',
    '/reference:..\Cloud Meadow_Data\Managed\mscorlib.dll',
    '/reference:..\Cloud Meadow_Data\Managed\System.dll',
    '/reference:..\Cloud Meadow_Data\Managed\System.Core.dll',
    '/reference:..\Cloud Meadow_Data\Managed\System.Xml.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.CoreModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.UI.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.UIModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.TextRenderingModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.IMGUIModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.Physics2DModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.JSONSerializeModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\Game.dll',
    '/reference:..\Cloud Meadow_Data\Managed\Common.dll',
    '/reference:..\BepInEx\core\BepInEx.dll',
    '/reference:..\BepInEx\core\BepInEx.Harmony.dll',
    '/reference:..\BepInEx\core\0Harmony.dll'
  )
  $src = @('Plugin.cs','CreativeEditorUGUI.cs','ReflectionUtil.cs','GameApi.cs','GameApi.Quest.cs','TransactionManager.cs','FarmPatches.cs','LogSink.cs','GameEventsListener.cs','MovementPatches.cs')
  if (!(Test-Path .\bin)) { New-Item -ItemType Directory -Path .\bin | Out-Null }
  & dotnet $csc @refs @src
  if ($LASTEXITCODE -ne 0) { throw "C# compiler failed with exit code $LASTEXITCODE" }
}
finally {
  Pop-Location
}

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
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.IMGUIModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\UnityEngine.Physics2DModule.dll',
    '/reference:..\Cloud Meadow_Data\Managed\Game.dll',
    '/reference:..\Cloud Meadow_Data\Managed\Common.dll',
    '/reference:..\BepInEx\core\BepInEx.dll',
    '/reference:..\BepInEx\core\BepInEx.Harmony.dll',
    '/reference:..\BepInEx\core\0Harmony.dll'
  )
  $src = @('Plugin.cs','ReflectionUtil.cs','UIOverlay.cs','UIOverlay.Player.cs','UIOverlay.Farm.cs','UIOverlay.Inventory.cs','UIOverlay.Cheats.cs','UIOverlay.OverviewParty.cs','UIOverlay.Quests.cs','GameApi.cs','GameApi.Quest.cs','FarmPatches.cs','LogSink.cs','GameEventsListener.cs','MovementPatches.cs')
  if (!(Test-Path .\bin)) { New-Item -ItemType Directory -Path .\bin | Out-Null }
  & dotnet $csc @refs @src
}
finally {
  Pop-Location
}

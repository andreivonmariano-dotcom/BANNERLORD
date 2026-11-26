param([Parameter(Mandatory=$true)][int]$TargetPid,[Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference='Stop'
if($Name -notmatch '^game-[a-z0-9-]+$'){throw 'invalid lab frame name'}
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;using System.Runtime.InteropServices;
public static class LabGameFrame{
[StructLayout(LayoutKind.Sequential)]public struct RECT{public int Left,Top,Right,Bottom;}
[StructLayout(LayoutKind.Sequential)]public struct POINT{public int X,Y;}
[DllImport("user32.dll")]public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")]public static extern bool GetClientRect(IntPtr h,out RECT r);
[DllImport("user32.dll")]public static extern bool ClientToScreen(IntPtr h,ref POINT p);
[DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")]public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")]public static extern bool IsIconic(IntPtr h);
[DllImport("user32.dll")]public static extern IntPtr WindowFromPoint(POINT p);
[DllImport("user32.dll")]public static extern IntPtr GetAncestor(IntPtr h,uint flags);
}
'@
[void][LabGameFrame]::SetProcessDPIAware()
$p=Get-Process -Id $TargetPid -ErrorAction Stop;$h=$p.MainWindowHandle
if($p.ProcessName -ne 'TaleWorlds.MountAndBlade.Launcher' -or $p.MainWindowTitle -notlike 'Mount and Blade II Bannerlord - Singleplayer PID:*' -or $p.SessionId -ne (Get-Process -Id $PID).SessionId -or !$p.Responding -or [LabGameFrame]::IsIconic($h)){throw 'invalid game target'}
[void][LabGameFrame]::SetForegroundWindow($h);Start-Sleep -Milliseconds 400
if([LabGameFrame]::GetForegroundWindow() -ne $h){throw 'not foreground game'}
$r=New-Object LabGameFrame+RECT;$o=New-Object LabGameFrame+POINT
if(![LabGameFrame]::GetClientRect($h,[ref]$r) -or ![LabGameFrame]::ClientToScreen($h,[ref]$o)){throw 'client bounds'}
$w=$r.Right;$hh=$r.Bottom;if($w -lt 640 -or $hh -lt 360 -or $w*$hh -gt 16000000){throw 'bad game size'}
foreach($yy in @(5,[int]($hh*.25),[int]($hh*.5),[int]($hh*.75),($hh-5))){foreach($xx in @(5,[int]($w*.25),[int]($w*.5),[int]($w*.75),($w-5))){$pt=New-Object LabGameFrame+POINT;$pt.X=$o.X+$xx;$pt.Y=$o.Y+$yy;if([LabGameFrame]::GetAncestor([LabGameFrame]::WindowFromPoint($pt),2) -ne $h){throw 'game occluded'}}}
$b=New-Object Drawing.Bitmap($w,$hh);$gr=[Drawing.Graphics]::FromImage($b)
try{
$gr.CopyFromScreen($o.X,$o.Y,0,0,$b.Size);if([LabGameFrame]::GetForegroundWindow() -ne $h){throw 'focus lost'}
$dir=Join-Path $env:LOCALAPPDATA 'BannerlordResearchLab\DesktopLab'
$path=Join-Path $dir ($Name+'.png');$preview=Join-Path $dir ($Name+'-preview.jpg')
if((Test-Path $path) -or (Test-Path $preview)){throw 'capture exists'}
$b.Save($path,[Drawing.Imaging.ImageFormat]::Png)
$small=New-Object Drawing.Bitmap($b,960,540);try{$small.Save($preview,[Drawing.Imaging.ImageFormat]::Jpeg)}finally{$small.Dispose()}
'FRAME '+$w+'x'+$hh+' origin='+$o.X+','+$o.Y+' handle='+$h+' responding='+$p.Responding
'SHA '+(Get-FileHash $path).Hash
'PREVIEW '+$preview
}finally{$gr.Dispose();$b.Dispose()}
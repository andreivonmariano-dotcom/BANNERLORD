param([Parameter(Mandatory=$true)][int]$TargetPid,[Parameter(Mandatory=$true)][string]$Reference,[Parameter(Mandatory=$true)][string]$ExpectedSha)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class LabPlay {
 [StructLayout(LayoutKind.Sequential)] public struct RECT {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)] public struct POINT {public int X,Y;}
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT r);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
 [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h,uint flags);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
}
'@
[void][LabPlay]::SetProcessDPIAware()
$p=Get-Process -Id $TargetPid -ErrorAction Stop
$h=$p.MainWindowHandle
if($p.ProcessName -ne 'TaleWorlds.MountAndBlade.Launcher' -or $p.MainWindowTitle -ne 'M&B II: Bannerlord' -or $h -eq 0 -or $p.SessionId -ne (Get-Process -Id $PID).SessionId){throw 'target identity'}
[void][LabPlay]::SetForegroundWindow($h);Start-Sleep -Milliseconds 500
if([LabPlay]::GetForegroundWindow() -ne $h){throw 'foreground denied'}
$r=New-Object LabPlay+RECT
if(![LabPlay]::GetWindowRect($h,[ref]$r) -or $r.Right-$r.Left -ne 923 -or $r.Bottom-$r.Top -ne 560){throw 'bounds changed'}
$ld=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord\Configs\LauncherData.xml'
[xml]$x=Get-Content $ld -Raw
$selected=@($x.SelectNodes('//UserModData')|Where-Object {$_.IsSelected -eq 'true'}|ForEach-Object {$_.Id})
$expected=@('Bannerlord.Harmony','Bannerlord.ButterLib','Bannerlord.UIExtenderEx','Bannerlord.MBOptionScreen','Native','SandBoxCore','Sandbox','CustomBattle','StoryMode','BirthAndDeath','NavalDLC','CUNativeObservation')
if(($selected -join '|') -ne ($expected -join '|')){throw 'profile changed'}
if((Get-FileHash -LiteralPath $Reference).Hash -ne $ExpectedSha){throw 'reference mismatch'}
$old=[Drawing.Bitmap]::FromFile($Reference)
$b=New-Object Drawing.Bitmap(220,44);$gr=[Drawing.Graphics]::FromImage($b)
try{
if($old.Width -ne 648 -or $old.Height -ne 458){throw 'reference bounds'}
foreach($yy in @(394,415,435)){foreach($xx in @(87,190,305)){$pt=New-Object LabPlay+POINT;$pt.X=$r.Left+5+$xx;$pt.Y=$r.Top+100+$yy;if([LabPlay]::GetAncestor([LabPlay]::WindowFromPoint($pt),2) -ne $h){throw 'play area occluded'}}}
$gr.CopyFromScreen($r.Left+5+87,$r.Top+100+394,0,0,$b.Size)
$matches=0;$count=0
for($yy=0;$yy -lt 44;$yy+=3){for($xx=0;$xx -lt 220;$xx+=3){$c=$b.GetPixel($xx,$yy);$d=$old.GetPixel($xx+87,$yy+394);if([Math]::Abs([int]$c.R-[int]$d.R)+[Math]::Abs([int]$c.G-[int]$d.G)+[Math]::Abs([int]$c.B-[int]$d.B) -le 16){$matches++};$count++}}
'PLAY_REGION_MATCH '+$matches+'/'+$count
if($matches/$count -lt 0.95){throw 'play region differs; no click'}
}finally{$gr.Dispose();$b.Dispose();$old.Dispose()}
$point=New-Object LabPlay+POINT;$point.X=$r.Left+202;$point.Y=$r.Top+516
if([LabPlay]::GetForegroundWindow() -ne $h -or [LabPlay]::GetAncestor([LabPlay]::WindowFromPoint($point),2) -ne $h){throw 'target changed'}
if(![LabPlay]::SetCursorPos($point.X,$point.Y)){throw 'cursor move failed'}
if([LabPlay]::GetForegroundWindow() -ne $h -or [LabPlay]::GetAncestor([LabPlay]::WindowFromPoint($point),2) -ne $h){throw 'target changed'}
[LabPlay]::mouse_event(2,0,0,0,[UIntPtr]::Zero);[LabPlay]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
'PLAY_CLICK_SENT_NOT_CONTINUE'
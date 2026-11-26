param([Parameter(Mandatory=$true)][int]$TargetPid,[Parameter(Mandatory=$true)][string]$ReferenceName,[Parameter(Mandatory=$true)][string]$ExpectedSha,[Parameter(Mandatory=$true)][ValidateSet('Sandbox','ObservedClick','Escape','SceneMenu')][string]$Action,[int]$X=-1,[int]$Y=-1,[int]$W=100,[int]$H=40)
$ErrorActionPreference='Stop'
if($ReferenceName -notmatch '^game-[a-z0-9-]+$'){throw 'invalid lab frame name'}
if($Action -eq 'Sandbox'){$X=308;$Y=520;$W=150;$H=36}
if($X -lt 10 -or $Y -lt 10 -or $W -lt 20 -or $H -lt 20 -or $W -gt 600 -or $H -gt 300){throw 'invalid region'}
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;using System.Runtime.InteropServices;
public static class LabGameAction{
[StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk,wScan;public uint dwFlags,time;public UIntPtr dwExtraInfo; }
[StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int dx,dy;public uint mouseData,dwFlags,time;public UIntPtr dwExtraInfo; }
[StructLayout(LayoutKind.Explicit)] public struct INPUTUNION { [FieldOffset(0)]public KEYBDINPUT ki;[FieldOffset(0)]public MOUSEINPUT mi; }
[StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type;public INPUTUNION data; }
[DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint n,INPUT[] input,int size);
public static uint Scan(ushort scan,bool up){INPUT i=new INPUT();i.type=1;i.data.ki.wScan=scan;i.data.ki.dwFlags=8u|(up?2u:0u);return SendInput(1,new[]{i},Marshal.SizeOf(typeof(INPUT)));}
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
[DllImport("user32.dll")]public static extern bool SetCursorPos(int x,int y);
[DllImport("user32.dll")]public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
[DllImport("user32.dll")]public static extern void keybd_event(byte vk,byte scan,uint flags,UIntPtr extra);
}
'@
[void][LabGameAction]::SetProcessDPIAware()
$p=Get-Process -Id $TargetPid -ErrorAction Stop;$handle=$p.MainWindowHandle
if($p.ProcessName -ne 'TaleWorlds.MountAndBlade.Launcher' -or $p.MainWindowTitle -notlike 'Mount and Blade II Bannerlord - Singleplayer PID:*' -or $p.SessionId -ne (Get-Process -Id $PID).SessionId -or !$p.Responding -or [LabGameAction]::IsIconic($handle)){throw 'invalid game target'}
$ld=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Mount and Blade II Bannerlord\Configs\LauncherData.xml'
[xml]$xml=Get-Content $ld -Raw
$selected=@($xml.SelectNodes('//UserModData')|Where-Object {$_.IsSelected -eq 'true'}|ForEach-Object {$_.Id})
$expected=@('Bannerlord.Harmony','Bannerlord.ButterLib','Bannerlord.UIExtenderEx','Bannerlord.MBOptionScreen','Native','SandBoxCore','Sandbox','CustomBattle','StoryMode','BirthAndDeath','NavalDLC','CUNativeObservation')
if(($selected -join '|') -ne ($expected -join '|')){throw 'profile changed'}
[void][LabGameAction]::SetForegroundWindow($handle);Start-Sleep -Milliseconds 400
if([LabGameAction]::GetForegroundWindow() -ne $handle){throw 'not foreground game'}
$r=New-Object LabGameAction+RECT;$o=New-Object LabGameAction+POINT
if(![LabGameAction]::GetClientRect($handle,[ref]$r) -or ![LabGameAction]::ClientToScreen($handle,[ref]$o)){throw 'client bounds'}
if($r.Right -ne 1920 -or $r.Bottom -ne 1080){throw 'resolution changed'}
$left=$X-[int]($W/2);$top=$Y-[int]($H/2)
if($left -lt 0 -or $top -lt 0 -or $left+$W -gt $r.Right -or $top+$H -gt $r.Bottom){throw 'out of bounds'}
$path=Join-Path $env:LOCALAPPDATA ('BannerlordResearchLab\DesktopLab\'+$ReferenceName+'.png')
if((Get-FileHash -LiteralPath $path).Hash -ne $ExpectedSha){throw 'reference changed'}
$old=[Drawing.Bitmap]::FromFile($path);$b=New-Object Drawing.Bitmap($W,$H);$gr=[Drawing.Graphics]::FromImage($b)
try{
if($old.Width -ne $r.Right -or $old.Height -ne $r.Bottom){throw 'reference resolution'}
foreach($yy in @($top,$Y,($top+$H-1))){foreach($xx in @($left,$X,($left+$W-1))){$pt=New-Object LabGameAction+POINT;$pt.X=$o.X+$xx;$pt.Y=$o.Y+$yy;if([LabGameAction]::GetAncestor([LabGameAction]::WindowFromPoint($pt),2) -ne $handle){throw 'action area occluded'}}}
$gr.CopyFromScreen($o.X+$left,$o.Y+$top,0,0,$b.Size)
$matches=0;$count=0
for($yy=0;$yy -lt $H;$yy+=2){for($xx=0;$xx -lt $W;$xx+=2){$c=$b.GetPixel($xx,$yy);$d=$old.GetPixel($left+$xx,$top+$yy);if([Math]::Abs([int]$c.R-[int]$d.R)+[Math]::Abs([int]$c.G-[int]$d.G)+[Math]::Abs([int]$c.B-[int]$d.B) -le 24){$matches++};$count++}}
'REGION_MATCH '+$matches+'/'+$count
if($matches/$count -lt 0.92){throw 'visual mismatch; no input'}
}finally{$gr.Dispose();$b.Dispose();$old.Dispose()}
$point=New-Object LabGameAction+POINT;$point.X=$o.X+$X;$point.Y=$o.Y+$Y
if([LabGameAction]::GetForegroundWindow() -ne $handle -or [LabGameAction]::GetAncestor([LabGameAction]::WindowFromPoint($point),2) -ne $handle){throw 'target changed'}
if($Action -eq 'SceneMenu'){
 if($ReferenceName -ne 'game-aut005-start'){throw 'only observed scene'}
 if([LabGameAction]::Scan(1,$false) -ne 1){throw 'input rejected'}
 try{Start-Sleep -Milliseconds 220}finally{if([LabGameAction]::Scan(1,$true) -ne 1){throw 'release rejected'}}
}elseif($Action -eq 'Escape'){
 [LabGameAction]::keybd_event(27,0,0,[UIntPtr]::Zero);try{Start-Sleep -Milliseconds 90}finally{[LabGameAction]::keybd_event(27,0,2,[UIntPtr]::Zero)}
}else{
 if(![LabGameAction]::SetCursorPos($point.X,$point.Y)){throw 'cursor move failed'}
 if([LabGameAction]::GetForegroundWindow() -ne $handle -or [LabGameAction]::GetAncestor([LabGameAction]::WindowFromPoint($point),2) -ne $handle){throw 'target changed'}
 [LabGameAction]::mouse_event(2,0,0,0,[UIntPtr]::Zero);try{Start-Sleep -Milliseconds 90}finally{[LabGameAction]::mouse_event(4,0,0,0,[UIntPtr]::Zero)}
}
'ACTION_SENT '+$Action+' X='+$X+' Y='+$Y+' (poststate unverified)'
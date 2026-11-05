taskkill /F /IM customNotifyPos.exe /IM topleft.exe /IM topcenter.exe /IM topright.exe 2>nul
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /platform:anycpu32bitpreferred /out:"customNotifyPos.exe" "customNotifyPos.cs"

PAUSE

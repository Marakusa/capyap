Name "CapYap"
OutFile "Install_CapYap.exe"
RequestExecutionLevel user
SilentInstall silent

Icon "${ICONFILE}"

InstallDir "$LOCALAPPDATA\CapYap"

Section "Install"

  SetOutPath "$INSTDIR"
  File /r "${WORKINGDIR}\*.*"

  WriteUninstaller "$INSTDIR\uninstall.exe"

  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "DisplayName" "CapYap"
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "DisplayIcon" '"$INSTDIR\CapYap.exe"'
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "Publisher" "Your Company Name"

  CreateShortCut "$SMPROGRAMS\CapYap.lnk" "$INSTDIR\CapYap.exe" "" "$INSTDIR\CapYap.exe" 0

  CreateShortCut "$DESKTOP\CapYap.lnk" "$INSTDIR\CapYap.exe" "" "$INSTDIR\CapYap.exe" 0

  Exec '"$INSTDIR\CapYap.exe"'

SectionEnd

Section "Uninstall"
  
  Delete "$SMPROGRAMS\CapYap.lnk"
  Delete "$DESKTOP\CapYap.lnk"

  RMDir /r "$INSTDIR"

  DeleteRegKey HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap"

SectionEnd

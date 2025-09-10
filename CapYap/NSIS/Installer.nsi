!include "MUI2.nsh"

!define MUI_COMPONENTSPAGE_SMALLDESC
!define MUI_INSTFILESPAGE_COLORS "FFFFFF 000000" ; Background and text colors

Name "CapYap ${APP_VERSION}"
OutFile "CapYap_${APP_VERSION}-${PLATFORM}.exe"
RequestExecutionLevel user

Icon "${ICONFILE}"

InstallDir "$LOCALAPPDATA\CapYap"

!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

AutoCloseWindow true
ShowInstDetails nevershow

Section "Install"

  SetOutPath "$INSTDIR"
  File /r /x "publish\*" "${WORKINGDIR}\*.*"

  WriteUninstaller "$INSTDIR\uninstall.exe"

  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "DisplayName" "CapYap"
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "DisplayIcon" '"$INSTDIR\CapYap.exe"'
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "Publisher" "Marakusa"
  WriteRegStr HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap" "DisplayVersion" "${APP_VERSION}"

  CreateShortCut "$SMPROGRAMS\CapYap.lnk" "$INSTDIR\CapYap.exe" "" "$INSTDIR\CapYap.exe" 0

  CreateShortCut "$DESKTOP\CapYap.lnk" "$INSTDIR\CapYap.exe" "" "$INSTDIR\CapYap.exe" 0

  Exec '"$INSTDIR\CapYap.exe"'

SectionEnd

Section "Uninstall"
  !insertmacro MUI_UNGETLANGUAGE

  Delete "$SMPROGRAMS\CapYap ${APP_VERSION}.lnk"
  Delete "$DESKTOP\CapYap ${APP_VERSION}.lnk"

  RMDir /r "$INSTDIR"

  DeleteRegKey HKEY_CURRENT_USER "Software\Microsoft\Windows\CurrentVersion\Uninstall\CapYap"

SectionEnd

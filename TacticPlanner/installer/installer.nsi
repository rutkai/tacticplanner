!include "FileAssociation.nsh"

; TacticPlanner.nsi

;--------------------------------


; The name of the installer
Name "Tactic planner"

; The file to write
OutFile "install.exe"

; The default installation directory
InstallDir $PROGRAMFILES\TacticPlanner

; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------

; Functions


!define DOT_MAJOR "4"
!define DOT_MINOR "0"

; Usage
; Define in your script two constants:
;   DOT_MAJOR "(Major framework version)"
;   DOT_MINOR "{Minor framework version)"
; 
; Call IsDotNetInstalled
; This function will abort the installation if the required version 
; or higher version of the .NET Framework is not installed.  Place it in
; either your .onInit function or your first install section before 
; other code.
Function IsDotNetInstalled
 
  StrCpy $0 "0"
  StrCpy $1 "SOFTWARE\Microsoft\.NETFramework" ;registry entry to look in.
  StrCpy $2 0
 
  StartEnum:
    ;Enumerate the versions installed.
    EnumRegKey $3 HKLM "$1\policy" $2
 
    ;If we don't find any versions installed, it's not here.
    StrCmp $3 "" noDotNet notEmpty
 
    ;We found something.
    notEmpty:
      ;Find out if the RegKey starts with 'v'.  
      ;If it doesn't, goto the next key.
      StrCpy $4 $3 1 0
      StrCmp $4 "v" +1 goNext
      StrCpy $4 $3 1 1
 
      ;It starts with 'v'.  Now check to see how the installed major version
      ;relates to our required major version.
      ;If it's equal check the minor version, if it's greater, 
      ;we found a good RegKey.
      IntCmp $4 ${DOT_MAJOR} +1 goNext yesDotNetReg
      ;Check the minor version.  If it's equal or greater to our requested 
      ;version then we're good.
      StrCpy $4 $3 1 3
      IntCmp $4 ${DOT_MINOR} yesDotNetReg goNext yesDotNetReg
 
    goNext:
      ;Go to the next RegKey.
      IntOp $2 $2 + 1
      goto StartEnum
 
  yesDotNetReg:
    ;Now that we've found a good RegKey, let's make sure it's actually
    ;installed by getting the install path and checking to see if the 
    ;mscorlib.dll exists.
    EnumRegValue $2 HKLM "$1\policy\$3" 0
    ;$2 should equal whatever comes after the major and minor versions 
    ;(ie, v1.1.4322)
    StrCmp $2 "" noDotNet
    ReadRegStr $4 HKLM $1 "InstallRoot"
    ;Hopefully the install root isn't empty.
    StrCmp $4 "" noDotNet
    ;build the actuall directory path to mscorlib.dll.
    StrCpy $4 "$4$3.$2\mscorlib.dll"
    IfFileExists $4 yesDotNet noDotNet
 
  noDotNet:
    ;Nope, something went wrong along the way.  Looks like the 
    ;proper .NET Framework isn't installed.  
    MessageBox MB_OK "You must have v${DOT_MAJOR}.${DOT_MINOR} or greater of the .NET Framework installed.  Aborting!"
    Abort
 
  yesDotNet:
    ;Everything checks out.  Go on with the rest of the installation.
 
FunctionEnd

Function .onInit
  Call IsDotNetInstalled
FunctionEnd

;--------------------------------

; Pages

Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "TacticPlanner (required)"

  SectionIn RO
  
  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File "TacticPlanner.exe"
  File "AvalonDock.dll"
  File "WPFToolkit.Extended.dll"
  File "WriteableBitmapEx.Wpf.dll"
  File /r "maps"
  File /r "stamps"

  ; Register file assiciations
  ${registerExtension} "$INSTDIR\TacticPlanner.exe" ".tactic" "Tactic planner file"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TacticPlanner" "DisplayName" "TacticPlanner"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TacticPlanner" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TacticPlanner" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TacticPlanner" "NoRepair" 1
  WriteUninstaller "uninstall.exe"
  
SectionEnd

; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

  CreateDirectory "$SMPROGRAMS\TacticPlanner"
  CreateShortCut "$SMPROGRAMS\TacticPlanner\TacticPlanner.lnk" "$INSTDIR\TacticPlanner.exe" "" "$INSTDIR\TacticPlanner.exe" 0
  CreateShortCut "$SMPROGRAMS\TacticPlanner\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
  CreateShortCut "$DESKTOP\TacticPlanner.lnk" "$INSTDIR\TacticPlanner.exe" "" "$INSTDIR\TacticPlanner.exe" 0
  
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TacticPlanner"

  ; Remove files and uninstaller
  Delete $INSTDIR\TacticPlanner.exe
  Delete $INSTDIR\AvalonDock.dll
  Delete $INSTDIR\WPFToolkit.Extended.dll
  Delete $INSTDIR\WriteableBitmapEx.Wpf.dll
  RMDir /r $INSTDIR\maps
  RMDir /r $INSTDIR\stamps
  Delete $INSTDIR\uninstall.exe

  ; Remove file associations
  ${unregisterExtension} ".tactic" "Tactic planner file"

  ; Remove shortcuts, if any
  Delete "$SMPROGRAMS\TacticPlanner\*.*"
  Delete "$DESKTOP\TacticPlanner.lnk"

  ; Remove directories used
  RMDir "$SMPROGRAMS\TacticPlanner"
  RMDir "$INSTDIR"

SectionEnd

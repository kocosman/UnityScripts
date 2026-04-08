#Requires AutoHotkey v2.0
;#NoTrayIcon
Persistent

; ============================
; CONFIGURATION
; ============================

appName := "MyProject.exe"
appDir  := "C:\Users\Your\Project\Directory"

config := {
    appName: appName,
    appDir: appDir,
    appPath: appDir . "\" . appName,
    logFile: appDir . "\AppMonitorLog.txt",

    checkInterval: 5000,
    focusInterval: 30000,
    forceAlwaysOnTop: true
}

; ============================
; TIMERS
; ============================

SetTimer CheckApp, config.checkInterval
SetTimer EnsureFocus, config.focusInterval

; ============================
; FUNCTIONS
; ============================

CheckApp() {
    global config

    if !ProcessExists(config.appName) {
        Run config.appPath

        Log("Restarted application: " config.appName)
    }
}

EnsureFocus() {
    global config

    hwnd := WinExist("ahk_exe " config.appName)
    if !hwnd
        return

    ; Restore if minimized
    if WinGetMinMax("ahk_id " hwnd) = -1
        WinRestore "ahk_id " hwnd

    if config.forceAlwaysOnTop {
        WinSetAlwaysOnTop true, "ahk_id " hwnd
    } else {
        ; Temporary topmost bump (safer default)
        WinSetAlwaysOnTop true, "ahk_id " hwnd
        WinActivate "ahk_id " hwnd
        WinSetAlwaysOnTop false, "ahk_id " hwnd
    }
}

ProcessExists(name) {
    try {
        for proc in ComObjGet("winmgmts:").ExecQuery(
            "SELECT * FROM Win32_Process WHERE Name='" name "'"
        )
            return true
    } catch {
        return false
    }
    return false
}

Log(message) {
    global config

    timestamp := FormatTime(A_Now, "yyyy-MM-dd HH:mm:ss")
    FileAppend "[" timestamp "] " message "`n", config.logFile
}

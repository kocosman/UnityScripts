#Requires AutoHotkey v2.0
;#NoTrayIcon
Persistent

SetTimer CheckApp, 5000  ; Check every 5 seconds

appName := "AppToRun.exe"
appPath := "C:\Users\ProjectAddressDirectory\AppToRun.exe"
appDir := A_PathSplit(appPath).Dir
logFile := appDir . "\AppMonitorLog.txt"

CheckApp() {
    global appName, appPath, logFile
    if !ProcessExist(appName) {
        Run appPath
        timestamp := FormatTime(A_Now, "yyyy-MM-dd_HH:mm:ss")
        FileAppend "[" timestamp "] Restarted application: " appName "`n", logFile
    }
}

ProcessExist(name) {
    return ProcessExistPID := ProcessExistRaw(name)
}

ProcessExistRaw(name) {
    try {
        for proc in ComObjGet("winmgmts:").ExecQuery("Select * from Win32_Process where Name='" name "'")
            return true
    } catch {
        return false
    }
    return false
}

A_PathSplit(path) {
    ; Returns a map with Dir, File, Ext
    SplitPath path, &name, &dir, &ext, &nameNoExt, &drive
    return {Dir: dir, File: name, Ext: ext}
}

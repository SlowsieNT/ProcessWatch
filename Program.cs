using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;

namespace processwatch
{
    class Program
    {
        static List<string> vList = new List<string>();
        static void Main(string[] aArgs) {
            if (null == aArgs || aArgs.Length < 1) {
                Console.WriteLine("processwatch <ExeName-Contains-CaseInsensitive> <Argument-Contains-CaseInsensitive> <FileName>");
                Console.WriteLine("--- Examples ---");
                Console.WriteLine("processwatch devenv");
                Console.WriteLine("processwatch devenv proj-name");
                Console.WriteLine("processwatch devenv proj-name log.log");
                Console.WriteLine("processwatch devenv proj-name log-%stime%.log");
                Console.WriteLine("processwatch devenv proj-name log-%mtime%.log");
                Console.WriteLine("processwatch devenv proj-name log-%StIme%.log");
                Console.WriteLine("processWatch devenv proj-name log-%MtimE%.log");
                Console.WriteLine("--- Variables (Case-Insensitive) ---");
                Console.WriteLine("%stime% - Unix Timestamp (UTC+00)");
                Console.WriteLine("%mtime% - Unix Timestamp Milliseconds (UTC+00)");
                return;
            }
            SetTitle();
            Stopwatch vSW = new Stopwatch();
            while (true) {
                var vFProc = FindProc(aArgs);
                if (null != vFProc && !vSW.IsRunning)
                    vSW.Start();
                var vElapsed = GetElapsedSeconds(vSW);
                if (vSW.IsRunning)
                    Console.Title = "Elapsed: " + vElapsed;
                if (null == vFProc)
                    if (vSW.IsRunning) {
                        vList.Add(""+ vElapsed);
                        vSW.Stop();
                        vSW.Reset();
                        Console.WriteLine("Session lasted: {0}s", vElapsed);
                        if (aArgs.Length > 2) {
                            string vFileName = aArgs[2];
                            vFileName = ReplaceCI(vFileName, "%stime%", "" + (long)GetUT());
                            vFileName = ReplaceCI(vFileName, "%mtime%", "" + (long)(GetUT()*1000));
                            File.WriteAllText(vFileName, string.Join(",", vList.ToArray()));
                        }
                    }
                    else SetTitle();
                Thread.Sleep(35);
            }
        }
        static string ReplaceCI(string aString, string aOldStr, string aNewStr) {
            aOldStr = aOldStr.ToLower();
            string vOut = "";
            for (int vI = 0; vI < aString.Length; vI++) {
                var vNL = aString.Length - vI;
                if (vNL > aOldStr.Length)
                    vNL = aOldStr.Length;
                string vCurStr = aString.Substring(vI, vNL);
                if (aOldStr == vCurStr.ToLower()) {
                    vOut += aNewStr;
                    vI += (-1) + aOldStr.Length;
                }
                else vOut += aString[vI];
            }
            return vOut;
        }
        static void SetTitle() {
            Console.Title = "Process Watcher";
        }
        static double GetUT() {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
        static double GetElapsedSeconds(Stopwatch aSW) {
            return Math.Round(aSW.Elapsed.TotalMilliseconds / 1e3, 3);
        }
        static Process FindProc(string[] aArgs) {
            Process vFProc = null;
            foreach (var p in Process.GetProcesses()) {
                if (p.ProcessName.ToLower().Contains(aArgs[0].ToLower())) {
                    try {
                        var vCmdStr = GetCommandLine(p);
                        if (1 < aArgs.Length && vCmdStr.ToLower().Contains(aArgs[1].ToLower()))
                            vFProc = p;
                        else if (1 == aArgs.Length) vFProc = p;
                    } catch { }
                }
            }
            return vFProc;
        }
        private static string GetCommandLine(Process aProcess) {
            using (ManagementObjectSearcher vWMI = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + aProcess.Id))
            using (ManagementObjectCollection vOs = vWMI.Get())
                return vOs.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
        }
    }
}

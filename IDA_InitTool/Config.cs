using System;
using System.IO;
using Microsoft.Win32;
using IWshRuntimeLibrary;
using static IDA77_InitTool.Checker.IDAChecker;

namespace IDA77_InitTool
{
    class Config
    {
        public static bool SetIDAReg(string pyFilePath)
        {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            if (key == null)
            {
                return false;
            }
            else
            {
                key = key.CreateSubKey("Hex-Rays").CreateSubKey("IDA");
                key.SetValue("AutoCheckUpdates", 0);
                key.SetValue("AutoRequestUpdates", 0);
                key.SetValue("AutoUseLumina", 0);

                if (pyFilePath != null)
                {
                    key.SetValue("Python3TargetDLL", pyFilePath);
                }
                key.Close();
                return true;
            }
        }

        public static void CreateDesktopShortcut(string shortcutName, string targetPath, string description)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktopPath, shortcutName + ".lnk");
            var shortcut = (IWshShortcut)(new WshShell()).CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = description;
            shortcut.Save();
        }

        public static void SetIDADesktopShortcut(IDAExistFlag idaCheckFlag, string idaPath, string ida64Path)
        {
            if ((idaCheckFlag & IDAExistFlag.IDA32) == IDAExistFlag.IDA32)
            {
                CreateDesktopShortcut("IDA Pro (x32)", idaPath, "The Interactive Disassembler");
            }
            if ((idaCheckFlag & IDAExistFlag.IDA64) == IDAExistFlag.IDA64)
            {
                CreateDesktopShortcut("IDA Pro (x64)", ida64Path, "The Interactive Disassembler");
            }
        }
    }
}

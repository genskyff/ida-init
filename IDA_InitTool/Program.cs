using System;
using System.IO;
using static System.Console;
using static System.ConsoleColor;
using static IDA77_InitTool.Checker.IDAChecker;
using static IDA77_InitTool.Checker.PythonChecker;
using static IDA77_InitTool.Config;

namespace IDA77_InitTool
{
    class Program
    {
        static void Main(string[] args)
        {
            ForegroundColor = Cyan;
            WriteLine("==============================================");
            WriteLine("     IDA Initialization Tool by Binwalker");
            WriteLine("==============================================\n");
            ResetColor();

            var currentPath = Environment.CurrentDirectory;
            var idaPath = Path.Combine(currentPath, "ida.exe");
            var ida64Path = Path.Combine(currentPath, "ida64.exe");
            var pyDirPath = Path.Combine(currentPath, "python38");
            var pyFilePath = Path.Combine(pyDirPath, "python38.dll");
            var idaCheckFlag = CheckIDA(idaPath, ida64Path);
            var pyCheckFlag = CheckPython(pyDirPath, pyFilePath);

            if (idaCheckFlag == IDAExistFlag.None)
            {
                ForegroundColor = Red;
                WriteLine("Error: Please move this program to the IDA folder.");
                ResetColor();
                goto Exit;
            }
            else if (idaCheckFlag == IDAExistFlag.IDA32)
            {
                ForegroundColor = Yellow;
                WriteLine($"Warning: Cannot find <{Path.GetFileName(ida64Path)}> in IDA folder.\n");
                ResetColor();
                WriteLine("IDA x32 path: " + idaPath);
            }
            else if (idaCheckFlag == IDAExistFlag.IDA64)
            {
                ForegroundColor = Yellow;
                WriteLine($"Warning: Cannot find <{Path.GetFileName(idaPath)}> in IDA folder.\n");
                ResetColor();
                WriteLine("IDA x64 path: " + ida64Path);
            }
            else if (idaCheckFlag == (IDAExistFlag.IDA32 | IDAExistFlag.IDA64))
            {
                WriteLine("IDA x32 path: " + idaPath);
                WriteLine("IDA x64 path: " + ida64Path);
            }

            WriteLine("\n-----------------------------------\n");
            Write("Initialize without modifying details? (y/n) (Default: Yes): ");
            var cosInput = ReadLine()?.ToLower();
            if (cosInput == null || cosInput.Length == 0 ||
                cosInput.CompareTo("n") != 0 && cosInput.CompareTo("no") != 0)
            {
                if (pyCheckFlag == PythonExistFlag.None)
                {
                    ForegroundColor = Red;
                    WriteLine($"\nError: Cannot find <{Path.GetFileName(pyDirPath)}/> in IDA folder.");
                    ResetColor();
                    goto Exit;
                }
                else if (pyCheckFlag == PythonExistFlag.Dir)
                {
                    ForegroundColor = Red;
                    WriteLine($"\nError: Cannot find <{Path.GetFileName(pyFilePath)}> in <python38/>.");
                    ResetColor();
                    goto Exit;
                }

                if (!SetIDAReg(pyFilePath))
                {
                    ForegroundColor = Red;
                    WriteLine(@"\nError: Open registry key <HKEY_CURRENT_USER\SOFTWARE> failed.");
                    ResetColor();
                    goto Exit;
                }
                else
                {
                    WriteLine("\nEmbedded python path: " + $"{pyDirPath}");
                }

                SetIDADesktopShortcut(idaCheckFlag, idaPath, ida64Path);
                WriteLine("Desktop shortcut created.");

                goto OK;
            }

            WriteLine("\n-----------------------------------\n");
            Write("Use the embedded python? (y/n) (Default: Yes): ");
            var pyInput = ReadLine()?.ToLower();
            if (pyInput == null || pyInput.Length == 0 ||
                pyInput.CompareTo("n") != 0 && pyInput.CompareTo("no") != 0)
            {
                if (pyCheckFlag == PythonExistFlag.None)
                {
                    ForegroundColor = Red;
                    WriteLine($"\nError: Cannot find <{Path.GetFileName(pyDirPath)}/> in IDA folder.");
                    ResetColor();
                    goto Exit;
                }
                else if (pyCheckFlag == PythonExistFlag.Dir)
                {
                    ForegroundColor = Red;
                    WriteLine($"\nError: Cannot find <{Path.GetFileName(pyFilePath)}> in <python38/>.");
                    ResetColor();
                    goto Exit;
                }

                if (!SetIDAReg(pyFilePath))
                {
                    ForegroundColor = Red;
                    WriteLine(@"\nError: Open registry key <HKEY_CURRENT_USER\SOFTWARE> failed.");
                    ResetColor();
                    goto Exit;
                }
                else
                {
                    WriteLine("\nEmbedded python path: " + $"{pyDirPath}");
                }
            }

            WriteLine("\n-----------------------------------\n");
            Write("Create a desktop shortcut? (y/n) (Default: Yes): ");
            var scInput = ReadLine()?.ToLower();
            if (scInput == null || scInput.Length == 0 ||
                scInput.CompareTo("n") != 0 && scInput.CompareTo("no") != 0)
            {
                SetIDADesktopShortcut(idaCheckFlag, idaPath, ida64Path);
                WriteLine("\nDesktop shortcut created.");
            }

        OK:
            WriteLine("\n-----------------------------------\n");
            WriteLine("Disable IDA automatic networking.");
            WriteLine("\n-----------------------------------\n");
            ForegroundColor = Green;
            WriteLine("Done.");
            ResetColor();
        Exit:
            WriteLine("\nPress any key to exit.");
            ReadLine();
        }
    }
}

using System;
using System.IO;
using static System.Console;
using static System.ConsoleColor;
using static IDA90_InitTool.Checker.IDAChecker;
using static IDA90_InitTool.Checker.PythonChecker;
using static IDA90_InitTool.Config;

namespace IDA90_InitTool
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
            var pyDirPath = Path.Combine(currentPath, "python312");
            var pyFilePath = Path.Combine(pyDirPath, "python312.dll");
            var idaCheckFlag = CheckIDA(idaPath);
            var pyCheckFlag = CheckPython(pyDirPath, pyFilePath);

            if (idaCheckFlag == IDAExistFlag.None)
            {
                ForegroundColor = Red;
                WriteLine("Error: Please move this program to the IDA folder.");
                ResetColor();
                goto Exit;
            }
            else
            {
                WriteLine("IDA path: " + idaPath);
            }

            WriteLine("\n-----------------------------------\n");
            Write("Initialize without modifying details? (Y/n): ");
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
                    WriteLine($"\nError: Cannot find <{Path.GetFileName(pyFilePath)}> in <python312/>.");
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

                SetIDADesktopShortcut(idaCheckFlag, idaPath);
                WriteLine("Desktop shortcut created.");

                goto OK;
            }

            WriteLine("\n-----------------------------------\n");
            Write("Use the embedded python? (Y/n): ");
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
                    WriteLine($"\nError: Cannot find <{Path.GetFileName(pyFilePath)}> in <python312/>.");
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
            Write("Create a desktop shortcut? (Y/n): ");
            var scInput = ReadLine()?.ToLower();
            if (scInput == null || scInput.Length == 0 ||
                scInput.CompareTo("n") != 0 && scInput.CompareTo("no") != 0)
            {
                SetIDADesktopShortcut(idaCheckFlag, idaPath);
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

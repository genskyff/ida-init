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
            WriteLine("=======================================");
            WriteLine("     IDA 7.7 绿化工具 by Binwalker");
            WriteLine("=======================================\n");
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
                WriteLine("错误: 请将本程序放到 IDA 根目录");
                ResetColor();
                goto Exit;
            }
            else if (idaCheckFlag == IDAExistFlag.IDA32)
            {
                ForegroundColor = Yellow;
                WriteLine($"警告: 未在 IDA 根目录下找到 <{Path.GetFileName(ida64Path)}>\n");
                ResetColor();
                WriteLine("IDA x32 路径: " + idaPath);
            }
            else if (idaCheckFlag == IDAExistFlag.IDA64)
            {
                ForegroundColor = Yellow;
                WriteLine($"警告: 未在 IDA 根目录下找到 <{Path.GetFileName(idaPath)}>\n");
                ResetColor();
                WriteLine("IDA x64 路径: " + ida64Path);
            }
            else if (idaCheckFlag == (IDAExistFlag.IDA32 | IDAExistFlag.IDA64))
            {
                WriteLine("IDA x32 路径: " + idaPath);
                WriteLine("IDA x64 路径: " + ida64Path);
            }

            WriteLine("\n------------------------------\n");
            Write("是否一键绿化而不单独设置选项? (y/n) (默认: Yes): ");
            var cosInput = ReadLine()?.ToLower();
            if (cosInput == null || cosInput.Length == 0 ||
                cosInput.CompareTo("n") != 0 && cosInput.CompareTo("no") != 0)
            {
                if (pyCheckFlag == PythonExistFlag.None)
                {
                    ForegroundColor = Red;
                    WriteLine($"\n错误: 未在 IDA 根目录下找到 <{Path.GetFileName(pyDirPath)}> 目录");
                    ResetColor();
                    goto Exit;
                }
                else if (pyCheckFlag == PythonExistFlag.Dir)
                {
                    ForegroundColor = Red;
                    WriteLine($"\n错误: 未在 <python38> 目录下找到 <{Path.GetFileName(pyFilePath)}> 文件");
                    ResetColor();
                    goto Exit;
                }

                if (!SetIDAReg(pyFilePath))
                {
                    ForegroundColor = Red;
                    WriteLine(@"\n错误: 打开注册项 <HKEY_CURRENT_USER\SOFTWARE> 失败");
                    ResetColor();
                    goto Exit;
                }
                else
                {
                    WriteLine("\n绿化版 Python 路径: " + $"{pyDirPath}");
                    WriteLine("已禁用 IDA 自动联网");
                }

                SetIDADesktopShortcut(idaCheckFlag, idaPath, ida64Path);
                WriteLine("已创建桌面快捷方式");

                goto OK;
            }

            WriteLine("\n------------------------------\n");
            Write("是否使用绿化版 Python? (y/n) (默认: Yes): ");
            var pyInput = ReadLine()?.ToLower();
            if (pyInput == null || pyInput.Length == 0 ||
                pyInput.CompareTo("n") != 0 && pyInput.CompareTo("no") != 0)
            {
                if (pyCheckFlag == PythonExistFlag.None)
                {
                    ForegroundColor = Red;
                    WriteLine($"\n错误: 未在 IDA 根目录下找到 <{Path.GetFileName(pyDirPath)}> 目录");
                    ResetColor();
                    goto Exit;
                }
                else if (pyCheckFlag == PythonExistFlag.Dir)
                {
                    ForegroundColor = Red;
                    WriteLine($"\n错误: 未在 <python38> 目录下找到 <{Path.GetFileName(pyFilePath)}> 文件");
                    ResetColor();
                    goto Exit;
                }

                if (!SetIDAReg(pyFilePath))
                {
                    ForegroundColor = Red;
                    WriteLine(@"\n错误: 打开注册项 <HKEY_CURRENT_USER\SOFTWARE> 失败");
                    ResetColor();
                    goto Exit;
                }
                else
                {
                    WriteLine("\n绿化版 Python 路径: " + $"{pyDirPath}");
                    WriteLine("已禁用 IDA 自动联网");
                }
            }

            WriteLine("\n------------------------------\n");
            Write("是否创建桌面快捷方式? (y/n) (默认: Yes): ");
            var scInput = ReadLine()?.ToLower();
            if (scInput == null || scInput.Length == 0 ||
                scInput.CompareTo("n") != 0 && scInput.CompareTo("no") != 0)
            {
                SetIDADesktopShortcut(idaCheckFlag, idaPath, ida64Path);
                WriteLine("\n已创建桌面快捷方式");
            }

        OK:
            WriteLine("\n------------------------------\n");
            ForegroundColor = Green;
            WriteLine("绿化完成");
            ResetColor();
        Exit:
            WriteLine("\n请按任意键退出");
            ReadLine();
        }
    }
}

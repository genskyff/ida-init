using System.IO;

namespace IDA77_InitTool
{
    class Checker
    {
        public class IDAChecker
        {
            [System.Flags]
            public enum IDAExistFlag : byte
            {
                None = 0b_00,
                IDA32 = 0b_01,
                IDA64 = 0b_10,
            }

            public static IDAExistFlag CheckIDA(string idaPath, string ida64Path)
            {
                var idaCheckFlag = IDAExistFlag.None;
                if (File.Exists(idaPath))
                {
                    idaCheckFlag |= IDAExistFlag.IDA32;
                }
                if (File.Exists(ida64Path))
                {
                    idaCheckFlag |= IDAExistFlag.IDA64;
                }
                return idaCheckFlag;
            }
        }

        public class PythonChecker
        {
            [System.Flags]
            public enum PythonExistFlag : byte
            {
                None = 0b_00,
                Dir = 0b_01,
                File = 0b_10,

            }

            public static PythonExistFlag CheckPython(string pyDirPath, string pyFilePath)
            {
                var pyCheckFlag = PythonExistFlag.None;
                if (Directory.Exists(pyDirPath))
                {
                    pyCheckFlag |= PythonExistFlag.Dir;
                }
                if (File.Exists(pyFilePath))
                {
                    pyCheckFlag |= PythonExistFlag.File;
                }
                return pyCheckFlag;
            }
        }
    }
}

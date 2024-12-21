using System.IO;

namespace IDA90_InitTool
{
    class Checker
    {
        public class IDAChecker
        {
            [System.Flags]
            public enum IDAExistFlag : byte
            {
                None = 0b_00,
                IDA = 0b_01,
            }

            public static IDAExistFlag CheckIDA(string idaPath)
            {
                var idaCheckFlag = IDAExistFlag.None;
                if (File.Exists(idaPath))
                {
                    idaCheckFlag |= IDAExistFlag.IDA;
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
                File = 0b_10
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

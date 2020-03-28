using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KWRunner
{
    class Hooker
    {
        //C:\Users\yushi\Desktop\KWRunner.exe --user mc1 --jar C:\Users\yushi\Desktop\ --version 1.14.32.1 --serverdir C:\Users\yushi\Desktop\server\
        //Hooker引用

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        /************ CheckUserExist ***********************/
        [DllImport("Netapi32.dll")]
        extern static int NetUserEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string servername,
            int level,
            int filter,
            out IntPtr bufptr,
            int prefmaxlen,
            out int entriesread,
            out int totalentries,
            out int resume_handle);

        [DllImport("Netapi32.dll")]

        extern static int NetApiBufferFree(IntPtr Buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_0
        {
            public string Username;
        }

        static public void Hook(Process targetProcess, string version)
        {
            IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, targetProcess.Id);

            // searching for the address of LoadLibraryA and storing it in a pointer
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // name of the dll we want to inject
            string dllName = "C:\\Plugin\\BDSJSRunner\\" + version + ".dll";

            Console.WriteLine("Try to hook " + dllName);

            // alocating some memory on the target process - enough to store the name of the dll
            // and storing its address in a pointer
            IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            // writing the name of the dll there
            UIntPtr bytesWritten;
            if (!WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten))
            {
                Console.WriteLine("Hook Failed Cannot write memory");
            }
            else
            {
                CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
                // creating a thread that will call LoadLibraryA with allocMemAddress as argument
            }
        }

        static public void IsUserExist(string username)
        {
            int EntriesRead;
            int TotalEntries;
            int Resume;
            IntPtr bufPtr;
            NetUserEnum(null, 0, 2, out bufPtr, -1, out EntriesRead, out TotalEntries, out Resume);
            if (EntriesRead > 0)
            {
                USER_INFO_0[] Users = new USER_INFO_0[EntriesRead];
                IntPtr iter = bufPtr;
                for (int i = 0; i < EntriesRead; i++)
                {
                    Users[i] = (USER_INFO_0)Marshal.PtrToStructure(iter,
                        typeof(USER_INFO_0));
                    iter = (IntPtr)((int)iter + Marshal.SizeOf(typeof(USER_INFO_0)));
                    if (Users[i].Username == username) return;
                }
                NetApiBufferFree(bufPtr);
            }
            //User Not Set
            Console.WriteLine("User Not Exist, Creating User");
            string passwd = GetRandomString(15, true, true, true, true, "");
            if (UserControl.Commons.CreateLocalWindowsAccount(username, passwd, "MCS" + username, "The User of the BDS " + username, "mc", false, false))
            {
                Console.WriteLine("Creating User Done, Your Unique Password is " + passwd);
                SetValue("User", username, passwd);
            }
            else
            {
                Console.WriteLine("Creating User Failed, Please Contact Sale");
                Environment.Exit(-5);
            }
        }

        [DllImport("kernel32")]
        //                        读配置文件方法的6个参数：所在的分区（section）、键值、     初始缺省值、     StringBuilder、   参数长度上限、配置文件路径
        private static extern int GetPrivateProfileString(string section, string key, string deVal, StringBuilder retVal,
            int size, string filePath);

        [DllImport("kernel32")]
        //                            写配置文件方法的4个参数：所在的分区（section）、  键值、     参数值、        配置文件路径
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        public static void SetValue(string section, string key, string value)
        {
            //获得当前路径，当前是在Debug路径下
            string strPath = Environment.CurrentDirectory + "\\Password.ini";
            WritePrivateProfileString(section, key, value, strPath);
        }

        public static string GetValue(string section, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            string strPath = Environment.CurrentDirectory + "\\Password.ini";
            //最好初始缺省值设置为非空，因为如果配置文件不存在，取不到值，程序也不会报错
            GetPrivateProfileString(section, key, "NaN", sb, 255, strPath);
            return sb.ToString();
        }

        public static string GetRandomString(int length, bool useNum, bool useLow, bool useUpp, bool useSpe, string custom)
        {
            byte[] b = new byte[4];
            new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(b);
            Random r = new Random(BitConverter.ToInt32(b, 0));
            string s = null, str = custom;
            if (useNum == true) { str += "0123456789"; }
            if (useLow == true) { str += "abcdefghijklmnopqrstuvwxyz"; }
            if (useUpp == true) { str += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; }
            if (useSpe == true) { str += "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"; }
            for (int i = 0; i < length; i++)
            {
                s += str.Substring(r.Next(0, str.Length - 1), 1);
            }
            return s;
        }
    }
}

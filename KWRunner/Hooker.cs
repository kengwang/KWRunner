using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace KWRunner
{
    class Hooker
    {
        //C:\Users\yushi\Desktop\KWRunner.exe --user mc1 --jar C:\Users\yushi\Desktop\ --version 1.14.32.1 --serverdir C:\Users\yushi\Desktop\server\
        //Hooker引用

        /************ CheckUserExist ***********************/

        static public void IsUserExist(string username)
        {

            if (!isExistUserName(username))
            {
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
        }

        private static bool isExistUserName(string username)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.StandardInput.WriteLine("net user");
            proc.StandardInput.WriteLine("exit");
            string outStr = proc.StandardOutput.ReadToEnd();
            proc.Close();
            return outStr.Contains(username + " ");
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


        /******************* Hooker Start ***********************/
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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int GetLastError();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandleA(string name);

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

        public static bool Hook(Process name, string version)
        {
            bool ok1;
            //int ok2;
            //int hwnd;
            IntPtr baseaddress;
            IntPtr hack;
            IntPtr yan;

            string dllname = "C:\\Plugin\\BDSJSRunner\\" + version + ".dll";
            uint dlllength;
            dlllength = (uint)((dllname.Length + 1) * Marshal.SizeOf(typeof(char)));

            baseaddress = VirtualAllocEx(name.Handle, IntPtr.Zero, dlllength, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);   //申请内存空间
            if (baseaddress == IntPtr.Zero) //返回0则操作失败，下面都是
            {
                Console.WriteLine("申请内存空间失败！！");
                return false;
            }
            UIntPtr bytesWritten;

            ok1 = WriteProcessMemory(name.Handle, baseaddress, Encoding.Default.GetBytes(dllname), dlllength, out bytesWritten); //写内存
            if (!ok1)
            {
                Console.WriteLine("写内存失败！！" + GetLastError().ToString());
                return false;
            }

            hack = GetProcAddress(GetModuleHandleA("Kernel32"), "LoadLibraryW"); //取得loadlibarary在kernek32.dll地址

            if (hack == IntPtr.Zero)
            {
                Console.WriteLine("无法取得函数的入口点！！");
                return false;
            }

            yan=CreateRemoteThread(name.Handle, IntPtr.Zero, 0, hack, baseaddress, 0, IntPtr.Zero);
            GetLastError();
            if (yan == IntPtr.Zero)
            {
                //VirtualFreeEx(name.Handle, baseaddress, 0, 0x00008000);
                Console.WriteLine("创建远程线程失败！！" + GetLastError().ToString());
                return false;
            }
            else
            {
                Console.WriteLine("已成功注入dll!!");
                return true;
            }
        }
    }
}







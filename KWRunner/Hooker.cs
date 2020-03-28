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


        /******************* Hooker Start ***********************/

        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;

        [DllImport("kernel32.dll")] //声明API函数
        public static extern int VirtualAllocEx(IntPtr hwnd, int lpaddress, int size, uint type, uint tect);

        [DllImport("kernel32.dll")]
        public static extern int WriteProcessMemory(IntPtr hwnd, int baseaddress, string buffer, int nsize, int filewriten);

        [DllImport("kernel32.dll")]
        public static extern int GetProcAddress(int hwnd, string lpname);

        [DllImport("kernel32.dll")]
        public static extern int GetModuleHandleA(string name);

        [DllImport("kernel32.dll")]
        public static extern int CreateRemoteThread(IntPtr hwnd, int attrib, int size, int address, int par, int flags, int threadid);

        public static void Hook(Process name, string version)
        {
            int ok1;
            //int ok2;
            //int hwnd;
            int baseaddress;
            int hack;
            int yan;
            string dllname;

            dllname = "C:\\Plugin\\BDSJSRunner\\" + version + ".dll";
            int dlllength;
            dlllength = dllname.Length + 1;
            Console.WriteLine("Try to hook " + name.ProcessName.ToLower() + " From " + dllname);

            baseaddress = VirtualAllocEx(name.Handle, 0, dlllength, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE); //申请内存空间
            if (baseaddress == 0) //返回0则操作失败，下面都是
            {
                Console.WriteLine("Applying memory space failed");
                Environment.Exit(-3);
            }

            if (WriteProcessMemory(name.Handle, baseaddress, dllname, dlllength, 0) == 0)
            {
                Console.WriteLine("Writing memory failed");
                Environment.Exit(-3);
            }

            hack = GetProcAddress(GetModuleHandleA("Kernel32"), "LoadLibraryW"); //取得loadlibarary在kernek32.dll地址

            if (hack == 0)
            {
                Console.WriteLine("Cannot Get Enterence of application!");
                Environment.Exit(-3);
            }

            yan = CreateRemoteThread(name.Handle, 0, 0, hack, baseaddress, 0, 0); //创建远程线程。

            if (yan == 0)
            {
                Console.WriteLine("Creating remote thread Faied!");
                //Environment.Exit(-3);
            }
            else
            {
                Console.WriteLine("Hook Done!");
            }
        }
    }

}


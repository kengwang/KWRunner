using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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


        public static bool Hook(Process name, string version)
        {
            string dllname = "C:\\Plugin\\BDSJSRunner\\" + version + ".dll";
            try
            {
                var parameter = new HookParameter
                {
                    Msg = "已经成功注入目标进程",
                    HostProcessId = EasyHook.RemoteHooking.GetCurrentProcessId()
                };
                EasyHook.RemoteHooking.Inject(name.Id, EasyHook.InjectionOptions.DoNotRequireStrongName, dllname, dllname,string.Empty, parameter);
                Console.WriteLine("Inject Done!");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Inject Failed! " + e.Message);
                return false;
            }
        }
    }

    [Serializable]
    public class HookParameter
    {
        public string Msg { get; set; }
        public int HostProcessId { get; set; }
    }
}


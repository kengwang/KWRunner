using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace KWRunner
{
    class Program
    {
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
        private static Process _process;

        static void Main(string[] args)
        {
            try
            {
                //处理参数
                int argc = args.Length;
                string user = "NaN";
                string jarpath = "NaN";
                string version = "NaN";
                string serverdir = "NaN";
                string port = "NaN";
                string world = "NaN";
                string player = "NaN";
                string dll = "NaN";
                string path = "NaN";
                string param = "NaN";
                string memory = "NaN";
                bool haveparam = false;
                bool highquality = false;
                string type = "NaN";//0-BDS 1-NK 2-PM
                bool nodll = false;
                bool pause = false;
                bool injecter = false;
                for (int i = 0; i < argc; i++)
                {
                    if (args[i] == "--user") user = args[++i];
                    if (args[i] == "--jar") jarpath = args[++i];
                    if (args[i] == "--version") version = args[++i];
                    if (args[i] == "--serverdir") serverdir = args[++i];
                    if (args[i] == "--port") port = args[++i];
                    if (args[i] == "--world") world = args[++i];
                    if (args[i] == "--player") player = args[++i];
                    if (args[i] == "--nodll") nodll = true;
                    if (args[i] == "--dll") dll = args[++i];
                    if (args[i] == "--hq") highquality = true;
                    if (args[i] == "--type") type = args[++i];
                    if (args[i] == "--injecter") injecter = true;
                    if (args[i] == "--pause") pause = true;
                    if (args[i] == "--memory") memory = args[++i];
                }
                if (user == "NaN" || jarpath == "NaN" || version == "NaN" || serverdir == "NaN" || port == "NaN" || world == "NaN" || player == "NaN" || type == "NaN")
                {
                    OutputMessage("Args not Completed!");
                    Environment.Exit(-4);
                }
                //serverdir += "\\";
                OutputMessage("Welcome To Use KWRunner V1.1.0! Powered by Kengwang (github@kengwang)");
                if (pause) { OutputMessage("Today is not good for playing games"); Environment.Exit(-1); }
                OutputMessage("Checking For User Exist");
                Hooker.IsUserExist(user, jarpath);
                string passwd = Hooker.GetValue("User", user, jarpath + "\\Password.ini");
                if (type.ToLower() == "bds")
                {//BDS - FLOW                    
                    OutputMessage("Checking if BDS Opened");
                    Process[] p = Process.GetProcesses();
                    foreach (Process pro in p)
                    {
                        if (pro.ProcessName=="bedrock_server")
                        {
                            if (GetProcessUserName(pro.Id) == user)
                            {
                                OutputMessage("BDS Already Started, Sending Kill Command");
                                pro.Kill();
                            }
                        }
                    }

                    OutputMessage("Checking if first start");
                    if (!File.Exists(serverdir + "\\DONOTMODIFY"))
                    {
                        //Load serverproperties
                        OutputMessage("Loading Default Server Properties");
                        File.Copy(jarpath + "\\BDS\\def.properties", serverdir + "server.properties", true);
                        File.WriteAllText(serverdir + "\\DONOTMODIFY", "0");
                    }

                    if (File.ReadAllText(serverdir + "\\DONOTMODIFY") != version)
                    {
                        OutputMessage("Copying Required File, it may take few moument");
                        if (!CopyDirectory(jarpath + "\\BDS\\" + version + "\\", serverdir + "\\", true))
                        {
                            OutputMessage("Copying Directory Failed, Please Contact Sales");
                            Environment.Exit(-3);
                        }
                        else
                        {
                            File.WriteAllText(serverdir + "\\DONOTMODIFY", version);
                            OutputMessage("Give ServerDir Control Permission to the User, it may take few moument");
                            UserControl.Commons.GiveUserPermission(serverdir, user);
                            OutputMessage("Permission All Given");
                        }
                    }
                    if (!nodll)
                    {
                        OutputMessage("Copying Plugin DLL");
                        if (File.Exists(dll))
                        {
                            File.Copy(dll, serverdir + "\\Plugin.dll", true);
                        }
                        else
                        {
                            OutputMessage("Cannot Find DLL, Please Contact Sale.");
                        }
                    }
                    if (!injecter) path = serverdir + "bedrock_server.exe";
                    else
                    {
                        OutputMessage("Using Third Party Injecter!");
                        path = jarpath + "\\MCDllInject.exe";
                        param = serverdir + "bedrock_server.exe" + " " + serverdir;
                        haveparam = true;
                    }
                }
                else if (type.ToLower() == "nk")
                {//NK FLOW
                    if (memory == "NaN") { OutputMessage("Args not complete for nukkit"); Environment.Exit(-1); }
                    path = "C:\\Program Files\\Java\\jre1.8.0_231\\bin\\java.exe";
                    param = "-Xmx" + memory + "M -Xms" + memory + "M -XX:MaxPermSize=128M -Djline.terminal=jline.UnsupportedTerminal -jar \"" + jarpath + "\\Nukkit\\" + version + ".jar" + "\" nogui";
                    haveparam = true;
                }
                OutputMessage("Configuring Server Properties");
                string[] lines = File.ReadAllLines(serverdir + "\\server.properties");
                int l = lines.Length;
                bool portset = false;
                bool port6set = false;
                for (int i = 0; i < l; i++)
                {
                    if (lines[i].Contains("server-port=")) if (!portset) { lines[i] = "server-port=" + port; portset = true; } else { lines[i] = ""; }
                    if (lines[i].Contains("server-port =")) if (!portset) { lines[i] = "server-port=" + port; portset = true; } else { lines[i] = ""; }
                    if (lines[i].Contains("server-portv6")) if (!port6set) { lines[i] = "server-portv6=" + int.Parse(port) + 1; port6set = true; } else { lines[i] = ""; }
                    if (lines[i].Contains("level-name")) lines[i] = "level-name=" + world;
                    if (lines[i].Contains("max-players")) lines[i] = "max-players=" + player;
                    if (!highquality)
                    {
                        if (lines[i].Contains("view-distance")) lines[i] = "view-distance=5";
                        if (lines[i].Contains("tick-distance")) lines[i] = "tick-distance=5";
                        if (lines[i].Contains("tick-distance")) lines[i] = "tick-distance=4";
                        if (lines[i].Contains("max-threads")) lines[i] = "max-threads=4";
                    }
                }
                if (!portset)
                {
                    lines = lines.Append("server-port=" + port).ToList().ToArray();
                    //OutputMessage("Port to " + port);
                }
                if (!port6set)
                {
                    lines = lines.Append("server-portv6=" + (int.Parse(port) + 1).ToString()).ToList().ToArray();
                    //OutputMessage("v6 to " + int.Parse(port) + 1);
                }
                File.WriteAllLines(serverdir + "server.properties", lines);

                OutputMessage("Try to Start SERVER");
                Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = path;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                if (haveparam) process.StartInfo.Arguments = param;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.WorkingDirectory = serverdir;
                process.StartInfo.RedirectStandardInput = true;  // 重定向输入
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UserName = user; //指定用户名
                process.EnableRaisingEvents = true;                      // 启用Exited事件
                process.Exited += new EventHandler(Process_Exit);   // 注册进程结束事件
                process.OutputDataReceived += new DataReceivedEventHandler(processOutputDataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(processErrorDataReceived);
                SecureString password = new SecureString();   //SecureString，安全字符，必须是char类型
                foreach (char c in passwd.ToCharArray())
                {
                    password.AppendChar(c);
                }
                process.StartInfo.Password = password; //指定明码，必须是安全字符串
                _process = process;
                try
                {
                    process.Start();
                }
                catch (System.ComponentModel.Win32Exception e)
                {
                    OutputMessage(e.Message);
                    OutputMessage(path);
                }
                if (!nodll && !injecter)
                {
                    OutputMessage("Using Original Hooker");
                    Hooker.Hook(process, serverdir + "\\Plugin.dll");
                }
                process.BeginOutputReadLine();
                //process.BeginErrorReadLine();
                bool forcestop = false;
                while (!process.HasExited)
                {
                    string input = Console.ReadLine();
                    if (input == "stop")
                    {
                        process.StandardInput.WriteLine("stop");

                        if (forcestop)
                        {
                            if (process != null)
                                process.Kill();
                            OutputMessage("Force Killed BDS Server");
                        }
                        else
                        {
                            OutputMessage("Send stop Again to force stop");
                            forcestop = true;
                        }
                    }
                    else
                    {
                        process.StandardInput.WriteLine(input);
                    }
                }
                ///*             Comment in  Release Version
            }
            catch (Exception e)
            {
                OutputMessage("KWRunner Looks Like A Crash");
                if (true)
                {
                    OutputMessage(e.StackTrace);
                }
            }
            //*/
        }

        private static void OutputMessage(string str)
        {
            Console.WriteLine(str);
        }

        private static string GetProcessUserName(int pID)
        {

            string text1 = null;

            SelectQuery query1 = new SelectQuery("Select * from Win32_Process where processID=" + pID);
            ManagementObjectSearcher searcher1 = new ManagementObjectSearcher(query1);
            try
            {
                foreach (ManagementObject disk in searcher1.Get())
                {
                    ManagementBaseObject inPar = null;
                    ManagementBaseObject outPar = null;
                    inPar = disk.GetMethodParameters("GetOwner");
                    outPar = disk.InvokeMethod("GetOwner", inPar, null);
                    text1 = outPar["User"].ToString();
                }
            }
            catch (Exception e)
            {
                text1 = "SYSTEM";
            }

            return text1;
        }
        private static void Process_Exit(object sender, EventArgs e)
        {
            OutputMessage("BDS Exit with code " + _process.ExitCode);
            Environment.Exit(_process.ExitCode);
        }

        private static void processOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e == null) return;
            if (e.Data.Contains("rhymc"))
            {
                OutputMessage("BDSJSRunner Successfully Loaded!");
            }
            else if (e.Data.Contains("can't start server"))
            {
                OutputMessage("Cannot start BDS Server!");
                if (_process != null)
                    _process.Kill();
                //由于Precess_Exit会处理,就不管啦!
            }
            else
            {
                Console.WriteLine(e.Data);
            }
        }

        private static void processErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutputMessage(e.Data);
        }

        private static bool CopyDirectory(string SourcePath, string DestinationPath, bool overwriteexisting)
        {
            bool ret = false;
            try
            {
                SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
                DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";

                if (Directory.Exists(SourcePath))
                {
                    if (Directory.Exists(DestinationPath) == false)
                        Directory.CreateDirectory(DestinationPath);

                    foreach (string fls in Directory.GetFiles(SourcePath))
                    {
                        FileInfo flinfo = new FileInfo(fls);
                        flinfo.CopyTo(DestinationPath + flinfo.Name, overwriteexisting);
                    }
                    foreach (string drs in Directory.GetDirectories(SourcePath))
                    {
                        DirectoryInfo drinfo = new DirectoryInfo(drs);
                        if (CopyDirectory(drs, DestinationPath + drinfo.Name, overwriteexisting) == false)
                            ret = false;
                    }
                }
                ret = true;
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }
    }
}

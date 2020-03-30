using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                bool nodll = false;
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
                    if (args[i] == "--injecter") injecter = true;
                }
                if (user == "NaN" || jarpath == "NaN" || version == "NaN" || serverdir == "NaN" || port == "NaN" || world == "NaN" || player == "NaN")
                {
                    Console.WriteLine("Args not Completed!");
                    Environment.Exit(-4);
                }
                //serverdir += "\\";
                Console.WriteLine("Welcome To Use KWRunner V1.0.0! Powered by Kengwang (github@kengwang)");
                Console.WriteLine("Checking For User Exist");
                Hooker.IsUserExist(user, jarpath);
                string passwd = Hooker.GetValue("User", user, jarpath + "\\Password.ini");
                Console.WriteLine("Checking if first start");
                if (!File.Exists(serverdir + "\\DONOTMODIFY"))
                {
                    //Load serverproperties
                    Console.WriteLine("Loading Default Server Properties");
                    File.Copy(jarpath + "\\BDS\\def.properties", serverdir + "server.properties", true);
                    File.WriteAllText(serverdir + "\\DONOTMODIFY", "0");
                }

                if (File.ReadAllText(serverdir + "\\DONOTMODIFY") != version)
                {
                    Console.WriteLine("Copying Required File, it may take few moument");
                    if (!CopyDirectory(jarpath + "\\BDS\\" + version + "\\", serverdir + "\\", true))
                    {
                        Console.WriteLine("Copying Directory Failed, Please Contact Sales");
                        Environment.Exit(-3);
                    }
                    else
                    {
                        File.WriteAllText(serverdir + "\\DONOTMODIFY", version);
                        Console.WriteLine("Give ServerDir Control Permission to the User, it may take few moument");
                        UserControl.Commons.GiveUserPermission(serverdir, user);
                        Console.WriteLine("Permission All Given");
                    }
                }
                if (!nodll)
                {
                    Console.WriteLine("Copying Plugin DLL");
                    if (File.Exists(dll))
                    {
                        File.Copy(dll, serverdir + "\\Plugin.dll", true);
                    }
                    else
                    {
                        Console.WriteLine("Cannot Find DLL, Please Contact Sale. It will run by Non-dll mode!");
                    }
                }
                Console.WriteLine("Configuring Server Properties");
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
                }
                if (!portset)
                {
                    lines = lines.Append("server-port=" + port).ToList().ToArray();
                    //Console.WriteLine("Port to " + port);
                }
                if (!port6set)
                {
                    lines = lines.Append("server-portv6=" + (int.Parse(port) + 1).ToString()).ToList().ToArray();
                    //Console.WriteLine("v6 to " + int.Parse(port) + 1);
                }
                File.WriteAllLines(serverdir + "server.properties", lines);

                Console.WriteLine("Try to Start BDS at " + serverdir);
                Process process = new System.Diagnostics.Process();
                if (!injecter) process.StartInfo.FileName = serverdir + "bedrock_server.exe";
                else
                {
                    Console.WriteLine("Using Third Party Injecter!");
                    process.StartInfo.FileName = jarpath + "\\MCDllInject.exe";
                    process.StartInfo.Arguments = serverdir + "bedrock_server.exe" + " " + serverdir;
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = serverdir;
                process.StartInfo.RedirectStandardInput = true;  // 重定向输入
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UserName = user; //指定用户名
                process.EnableRaisingEvents = true;                      // 启用Exited事件
                process.Exited += new EventHandler(Process_Exit);   // 注册进程结束事件
                process.OutputDataReceived += new DataReceivedEventHandler(processOutputDataReceived);
                process.OutputDataReceived += new DataReceivedEventHandler(processErrorDataReceived);
                SecureString password = new SecureString();   //SecureString，安全字符，必须是char类型
                foreach (char c in passwd.ToCharArray())
                {
                    password.AppendChar(c);
                }
                process.StartInfo.Password = password; //指定明码，必须是安全字符串
                _process = process;
                process.Start();
                if (!nodll && !injecter)
                {
                    Console.WriteLine("Using Original Hooker");
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
                            process.Kill();
                            Console.WriteLine("Force Killed BDS Server");
                        }
                        else
                        {
                            Console.WriteLine("Send stop Again to force stop");
                            forcestop = true;
                        }
                    }
                    else
                    {
                        process.StandardInput.WriteLine(input);
                    }
                }
                ///*             Uncomment in  Release Version
            }
            catch (Exception e)
            {
                Console.WriteLine("KWRunner Looks Like A Crash");
            }
            //*/
        }

        private static void Process_Exit(object sender, EventArgs e)
        {
            Console.WriteLine("BDS Exit with code " + _process.ExitCode);
            Environment.Exit(_process.ExitCode);
        }

        private static void processOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Contains("rhymc"))
            {
                Console.WriteLine("BDSJSRunner Successfully Loaded!");
            }
            else if (e.Data.Contains("can't start server"))
            {
                Console.WriteLine("Cannot start BDS Server!");
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
            //Console.WriteLine("[ERROR] "+e.Data);
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

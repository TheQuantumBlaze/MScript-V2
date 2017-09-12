using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace Mscript
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Mscript Compiler v2");
            string dir = "";
            bool isDll = false;
            bool debug = false;
            bool unSafe = false;
            List<string> dlls = new List<string>();
            while (true)
            {
                string command = Console.ReadLine();
                string[] splitCommand = command.Split(' ');
                if (splitCommand[0] == "cd" && splitCommand.Length > 1)
                {
                    dir = splitCommand[1];
                    Console.WriteLine("Dir changed to " + splitCommand[1]);
                }
                else if (splitCommand[0] == "cd" && splitCommand.Length <= 1)
                {
                    Console.WriteLine("No Directory specified");
                }
                if (splitCommand[0] == "exit")
                {
                    break;
                }
                if (splitCommand[0] == "dlls" && splitCommand.Length > 1)
                {
                    if (splitCommand[1] == "list")
                    {
                        for (int i = 0; i < dlls.Count; i++)
                        {
                            Console.WriteLine(i + ") " + dlls[i]);
                        }
                    }
                    if (splitCommand[1] == "add" && splitCommand.Length > 2)
                    {
                        dlls.Add(splitCommand[2]);
                    }
                    if (splitCommand[1] == "remove" && splitCommand.Length > 2)
                    {
                        int a = int.Parse(splitCommand[2]);
                        if (a < dlls.Count)
                        {
                            dlls.RemoveAt(a);
                        }
                    }
                }
                if (splitCommand[0] == "debug")
                {
                    debug = !debug;
                    Console.WriteLine("Debug: " + debug.ToString());
                }
                if (splitCommand[0] == "unsafe")
                {
                    unSafe = !unSafe;
                    Console.WriteLine("Unsafe: " + unSafe.ToString());
                }
                if (splitCommand[0] == "isdll")
                {
                    isDll = !isDll;
                    Console.WriteLine("isDll: " + isDll.ToString());
                }
                if (splitCommand[0] == "compile")
                {
                    if (dir != "")
                    {
                        List<string> passedDlls = new List<string>();
                        if (dlls.Count > 0)
                        {
                            passedDlls.AddRange(dlls);
                        }
                        List<string> code = new List<string>();

                        foreach (string f in Directory.EnumerateFiles(dir))
                        {
                            if (f.EndsWith(".ms"))
                            {
                                string csharpCode;
                                new MscriptCompiler.Compiler(f, out csharpCode, out List<string> outd);
                                if(outd.Count > 0)
                                {
                                    passedDlls.AddRange(outd);
                                }
                                code.Add(csharpCode);
                            }
                        }

                        if (!Directory.Exists(dir + "/Bin"))
                            Directory.CreateDirectory(dir + "/Bin");

                        if (debug)
                        {
                            for (int i = 0; i < code.Count; i++)
                            {
                                var s = code[i];
                                using (TextWriter tw = new StreamWriter(new FileStream(dir + "/Bin/" + $"file{i}.cs", FileMode.Create)))
                                {
                                    tw.Write(s);
                                    tw.Flush();
                                }
                            }
                        }

                        List<string> compileDlls = new List<string>();
                        compileDlls.AddRange(dlls);

                        string name = "App";
                        if (splitCommand.Length > 1)
                        {
                            name = splitCommand[1];
                        }

                        if (!isDll)
                        {
                            var paramsters = new CompilerParameters(passedDlls.ToArray(), dir + "/Bin/" + name + ".exe");
                            paramsters.GenerateExecutable = true;
                            if (unSafe)
                            {
                                paramsters.CompilerOptions = "/unsafe";
                            }
                            using (var provider = new CSharpCodeProvider())
                            {
                                CompilerResults cr = provider.CompileAssemblyFromSource(paramsters, code.ToArray());
                                if (cr.Errors.Count > 0)
                                {
                                    foreach (var a in cr.Errors)
                                    {
                                        Console.WriteLine(a.ToString());
                                    }
                                }
                            }
                        }
                        else
                        {
                            var paramsters = new CompilerParameters(passedDlls.ToArray(), dir + "/Bin/" + name + ".dll");
                            paramsters.GenerateExecutable = false;
                            if (unSafe)
                            {
                                paramsters.CompilerOptions = "/unsafe";
                            }
                            using (var provider = new CSharpCodeProvider())
                            {
                                CompilerResults cr = provider.CompileAssemblyFromSource(paramsters, code.ToArray());
                                if (cr.Errors.Count > 0)
                                {
                                    foreach (var a in cr.Errors)
                                    {
                                        Console.WriteLine(a.ToString());
                                    }
                                }
                            }
                        }

                        foreach (string s in passedDlls)
                        {
                            File.Copy(s, dir + "/Bin/" + Path.GetFileName(s), true);
                        }


                        Console.WriteLine("Finished Compilation");
                    }
                    else
                    {
                        Console.WriteLine("No Directory Specified Use \"dir location\"");
                    }
                }
                if (splitCommand[0] == "run")
                {
                    if (Directory.Exists(dir + "/Bin"))
                    {
                        foreach (string s in Directory.EnumerateFiles(dir + "/Bin"))
                        {
                            if (s.EndsWith(".exe"))
                            {
                                System.Diagnostics.Process.Start(s);
                                break;
                            }
                        }
                    }
                }
                if(splitCommand[0] == "compiler")
                {
                    if (dir != "")
                    {
                        List<string> passedDlls = new List<string>();
                        if (dlls.Count > 0)
                        {
                            passedDlls.AddRange(dlls);
                        }
                        List<string> code = new List<string>();

                        foreach (string f in Directory.EnumerateFiles(dir))
                        {
                            if (f.EndsWith(".ms"))
                            {
                                string csharpCode;
                                new MscriptCompiler.Compiler(f, out csharpCode, out List<string> outd);
                                if(outd.Count > 0)
                                {
                                    passedDlls.AddRange(outd);
                                }
                                code.Add(csharpCode);
                            }
                        }

                        if (!Directory.Exists(dir + "/Bin"))
                            Directory.CreateDirectory(dir + "/Bin");

                        if (debug)
                        {
                            for (int i = 0; i < code.Count; i++)
                            {
                                var s = code[i];
                                using (TextWriter tw = new StreamWriter(new FileStream(dir + "/Bin/" + $"file{i}.cs", FileMode.Create)))
                                {
                                    tw.Write(s);
                                    tw.Flush();
                                }
                            }
                        }

                        List<string> compileDlls = new List<string>();
                        compileDlls.AddRange(dlls);

                        string name = "App";
                        if (splitCommand.Length > 1)
                        {
                            name = splitCommand[1];
                        }

                        if (!isDll)
                        {
                            var paramsters = new CompilerParameters(passedDlls.ToArray(), dir + "/Bin/" + name + ".exe");
                            paramsters.GenerateExecutable = true;
                            if (unSafe)
                            {
                                paramsters.CompilerOptions = "/unsafe";
                            }
                            using (var provider = new CSharpCodeProvider())
                            {
                                CompilerResults cr = provider.CompileAssemblyFromSource(paramsters, code.ToArray());
                                if (cr.Errors.Count > 0)
                                {
                                    foreach (var a in cr.Errors)
                                    {
                                        Console.WriteLine(a.ToString());
                                    }
                                }
                            }
                        }
                        else
                        {
                            var paramsters = new CompilerParameters(passedDlls.ToArray(), dir + "/Bin/" + name + ".dll");
                            paramsters.GenerateExecutable = false;
                            if (unSafe)
                            {
                                paramsters.CompilerOptions = "/unsafe";
                            }
                            using (var provider = new CSharpCodeProvider())
                            {
                                CompilerResults cr = provider.CompileAssemblyFromSource(paramsters, code.ToArray());
                                if (cr.Errors.Count > 0)
                                {
                                    foreach (var a in cr.Errors)
                                    {
                                        Console.WriteLine(a.ToString());
                                    }
                                }
                            }
                        }

                        foreach (string s in passedDlls)
                        {
                            File.Copy(s, dir + "/Bin/" + Path.GetFileName(s), true);
                        }


                        Console.WriteLine("Finished Compilation");
                    }
                    else
                    {
                        Console.WriteLine("No Directory Specified Use \"dir location\"");
                    }
                    if (Directory.Exists(dir + "/Bin"))
                    {
                        foreach (string s in Directory.EnumerateFiles(dir + "/Bin"))
                        {
                            if (s.EndsWith(".exe"))
                            {
                                System.Diagnostics.Process.Start(s);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}

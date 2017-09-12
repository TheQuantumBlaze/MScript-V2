using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MscriptCompiler
{
    public class Compiler
    {
        public string fileInformation = "";
        public int dataCounter = 0;
        public List<string> fileTokens;

        public string globalclass = "MSGlobal";
        public string set;
        public Stack<StackHolder> scope = new Stack<StackHolder>();
        public Dictionary<string, Group> groups;
        public Dictionary<string, Using> usingStatements;

        public Compiler(string dir, out string compiled, out List<string> dlls)
        {
            set = globalclass;
            dlls = new List<string>();
            string outputFile = "";
            var converter = new Converter();
            groups = new Dictionary<string, Group>();
            usingStatements = new Dictionary<string, Using>();
            groups.Add(globalclass, new Group(globalclass));
            groups[globalclass].functions.Add(globalclass, new Function(globalclass, "", ""));
            clearStack();

            using (TextReader tr = new StreamReader(dir))
            {
                fileInformation = tr.ReadToEnd();
            }


            if (fileInformation != "")
            {
                A: string[] array = fileInformation.Split(';');
                foreach (string a in array)
                {
                    var s = a.Replace("\r\n", "").ToLower().StartsWith("#include");
                    if (s)
                    {
                        string file = a.Replace("\r\n", "").ToLower().Replace("#include ", "");
                        if (File.Exists($"./code/{file}"))
                        {
                            using (TextReader tr = new StreamReader($"./code/{file}"))
                            {
                                string data = tr.ReadToEnd();
                                fileInformation = fileInformation.Replace(a+";", data);
                            }
                            goto A;
                        }
                    }

                    s = a.Replace("\r\n", "").ToLower().StartsWith("#import");
                    if (s)
                    {
                        string file = a.Replace("\r\n", "").ToLower().Replace("#import ", "");
                        if (File.Exists($"{file}"))
                        {
                            dlls.Add(file);
                            fileInformation = fileInformation.Replace(a+";", "");
                            goto A;
                        }
                    }
                }

                fileTokens = converter.Convert(fileInformation);

                //using statements and groups
                for (int i = 0; i < fileTokens.Count; i++)
                {
                    var token = getElementAtID(i);
                    if (token == "#")
                    {
                        string location = "";
                        for (int a = i + 1; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ";")
                            {
                                location += d;
                            }
                            else
                            {
                                if (!usingStatements.ContainsKey(location))
                                {
                                    usingStatements.Add(location, new Using(location));
                                }
                                i = a;
                                break;
                            }
                        }
                    }
                    else if (token == "group")
                    {
                        if (i != 0)
                        {
                            if (getElementAtID(i - 1) == "end")
                            {
                                continue;
                            }
                        }
                        string location = "";
                        for (int a = i + 1; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ";")
                            {
                                location += d;
                            }
                            else
                            {
                                groups.Add(location, new Group(location));
                                groups[location].functions.Add(location, new Function(location, "", ""));
                                i = a + 1;
                                break;
                            }
                        }
                    }
                }

                int currrentScopeHolder = 0;
                string standardCode = "";
                for (int i = 0; i < fileTokens.Count; i++)
                {
                    var token = getElementAtID(i);

                    if (token == "#")
                    {
                        string location = "";
                        for (int a = i + 1; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ";")
                            {
                                location += d;
                            }
                            else
                            {
                                i = a;
                                break;
                            }
                        }
                    }
                    else if (token == "group")
                    {
                        string location = "";
                        for (int a = i + 1; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ";")
                            {
                                location += d;
                            }
                            else
                            {
                                scope.Push(new StackHolder("group", location));
                                groups[location].set = set;
                                i = a;
                                break;
                            }
                        }
                    }
                    else if (token == "set")
                    {
                        string location = "";
                        for (int a = i + 1; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ";")
                            {
                                location += d;
                            }
                            else
                            {
                                set = location;
                                groups[globalclass].set = set;
                                i = a;
                                break;
                            }
                        }
                    }
                    else if (token == "end")
                    {
                        if (getElementAtID(i + 1) == "group")
                        {
                            if (scope.Count > 0)
                            {
                                scope.Pop();
                            }
                            for (int a = i + 2; a < int.MaxValue; a++)
                            {
                                string d = getElementAtID(a);
                                if (d == null || d == ";")
                                {
                                    i = a;
                                    break;
                                }
                            }
                        }
                    }
                    else if (token == "var")
                    {
                        string name = getElementAtID(i + 1);
                        bool stat = false;
                        if (i - 1 > 0)
                        {
                            if (getElementAtID(i - 1) == "static")
                            {
                                stat = true;
                                standardCode = "";
                            }
                        }
                        if (name != null)
                        {
                            string colonOrOther = getElementAtID(i + 2);
                            string dataType = null;

                            if (colonOrOther != null)
                            {
                                if (colonOrOther == ":")
                                {
                                    dataType = "";

                                    for (int a = i + 3; a < int.MaxValue; a++)
                                    {
                                        string d = getElementAtID(a);
                                        if (d != null && d != ";" && d != "=")
                                        {
                                            dataType += d;
                                        }
                                        else
                                        {
                                            i = a;
                                            break;
                                        }
                                    }

                                    colonOrOther = getElementAtID(i);
                                    if (colonOrOther != null)
                                    {
                                        if (colonOrOther == ";")
                                        {
                                            if (scope.Count > 0)
                                            {
                                                var scopePosition = scope.Peek();
                                                if (scopePosition.type == "group" && (currrentScopeHolder == scopePosition.hold || (scopePosition.hold == null && currrentScopeHolder == 0)))
                                                {
                                                    groups[scopePosition.name].variables.Add(name, new Variable(name, dataType, null));
                                                    groups[scopePosition.name].variables[name].isStatic = stat;
                                                }
                                                else if (scopePosition.type == "function")
                                                {
                                                    string group = "";
                                                    for (int x = 0; x < scope.Count; x++)
                                                    {
                                                        var t = scope.ElementAt(x);
                                                        if (t.type == "group")
                                                        {
                                                            group = t.name;
                                                            break;
                                                        }
                                                    }

                                                    if (group == "")
                                                    {
                                                        group = globalclass;
                                                    }

                                                    groups[group].functions[scopePosition.name].code.Add($"{dataType} {name}");
                                                    groups[group].functions[scopePosition.name].code.Add($";");
                                                }
                                                else
                                                {
                                                    scopePosition = scope.Peek();
                                                    if (scopePosition.type == "group")
                                                    {
                                                        groups[scopePosition.name].functions[scopePosition.name].code.Add($"{dataType} {name}");
                                                        groups[scopePosition.name].functions[scopePosition.name].code.Add($";");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (currrentScopeHolder == 0)
                                                {
                                                    groups[globalclass].variables.Add(name, new Variable(name, dataType, null));
                                                    groups[globalclass].variables[name].isStatic = stat;
                                                }
                                                else
                                                {
                                                    groups[globalclass].functions[globalclass].code.Add($"{dataType} {name}");
                                                    groups[globalclass].functions[globalclass].code.Add($";");
                                                }
                                            }
                                            continue;
                                        }
                                        else if (colonOrOther == "=")
                                        {
                                            string equals = "";
                                            bool isNew = false;
                                            bool canNew = true;
                                            for (int a = i + 1; a < int.MaxValue; a++)
                                            {
                                                string d = getElementAtID(a);
                                                if (d == "(" && canNew)
                                                {
                                                    isNew = true;
                                                }
                                                if (d != null && d != ";")
                                                {
                                                    canNew = false;
                                                    if (d == "ref")
                                                    {
                                                        equals += "ref ";
                                                    }
                                                    else
                                                    {
                                                        equals += d;
                                                    }
                                                }
                                                else
                                                {
                                                    if (scope.Count > 0)
                                                    {
                                                        var scopePosition = scope.Peek();
                                                        if (scopePosition.type == "group" && (currrentScopeHolder == scopePosition.hold || (scopePosition.hold == null && currrentScopeHolder == 0)))
                                                        {
                                                            groups[scopePosition.name].variables.Add(name, new Variable(name, dataType, equals, isNew));
                                                            groups[scopePosition.name].variables[name].isStatic = stat;
                                                        }
                                                        else if (scopePosition.type == "function")
                                                        {
                                                            string group = "";
                                                            for (int x = 0; x < scope.Count; x++)
                                                            {
                                                                var t = scope.ElementAt(x);
                                                                if (t.type == "group")
                                                                {
                                                                    group = t.name;
                                                                    break;
                                                                }
                                                            }

                                                            if (group == "")
                                                            {
                                                                group = globalclass;
                                                            }
                                                            if (isNew)
                                                            {
                                                                groups[group].functions[scopePosition.name].code.Add($"{dataType} {name} = new {dataType}{equals}");
                                                                groups[group].functions[scopePosition.name].code.Add($";");
                                                            }
                                                            else
                                                            {
                                                                groups[group].functions[scopePosition.name].code.Add($"{dataType} {name} = {equals}");
                                                                groups[group].functions[scopePosition.name].code.Add($";");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            scopePosition = scope.Peek();
                                                            if (scopePosition.type == "group")
                                                            {
                                                                if (isNew)
                                                                {
                                                                    groups[scopePosition.name].functions[scopePosition.name].code.Add($"{dataType} {name} = new {dataType}{equals}");
                                                                    groups[scopePosition.name].functions[scopePosition.name].code.Add($";");
                                                                }
                                                                else
                                                                {
                                                                    groups[scopePosition.name].functions[scopePosition.name].code.Add($"{dataType} {name} = {equals}");
                                                                    groups[scopePosition.name].functions[scopePosition.name].code.Add($";");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (currrentScopeHolder == 0)
                                                        {
                                                            groups[globalclass].variables.Add(name, new Variable(name, dataType, equals, isNew));
                                                            groups[globalclass].variables[name].isStatic = stat;
                                                        }
                                                        else
                                                        {
                                                            if (isNew)
                                                            {
                                                                groups[globalclass].functions[globalclass].code.Add($"{dataType} {name} = new {dataType}{equals}");
                                                                groups[globalclass].functions[globalclass].code.Add($";");
                                                            }
                                                            else
                                                            {
                                                                groups[globalclass].functions[globalclass].code.Add($"{dataType} {name} = {equals}");
                                                                groups[globalclass].functions[globalclass].code.Add($";");
                                                            }
                                                        }
                                                    }
                                                    i = a;
                                                    break;
                                                }
                                            }
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (token == "function")
                    {
                        string name = getElementAtID(i + 1);
                        bool stat = false;
                        if (i - 1 > 0)
                        {
                            if (getElementAtID(i - 1) == "static")
                            {
                                stat = true;
                                standardCode = "";
                            }
                        }
                        if (name != null)
                        {
                            string arguments = "";
                            for (int a = i + 3; a < int.MaxValue; a++)
                            {
                                string d = getElementAtID(a);
                                
                                if (d != null && d != ")")
                                {
                                    arguments += d;
                                    if (d == "ref")
                                    {
                                        arguments += " ";
                                    }
                                }
                                else
                                {
                                    i = a;
                                    break;
                                }
                            }

                            string colon = getElementAtID(i + 1);
                            string returnType = "void";
                            if (colon == ":")
                            {
                                returnType = "";
                                for (int a = i + 2; a < int.MaxValue; a++)
                                {
                                    string d = getElementAtID(a);
                                    if (d != null && d != "{" && d != ";")
                                    {
                                        returnType += d;
                                    }
                                    else
                                    {
                                        i = a-1;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                returnType = "void";
                            }

                            if (scope.Count > 0)
                            {
                                var scopePosition = scope.Peek();
                                if (scopePosition.type == "group")
                                {
                                    groups[scopePosition.name].functions.Add(name, new Function(name, returnType, arguments));
                                    groups[scopePosition.name].functions[name].isStatic = stat;
                                }
                                else
                                {
                                    string group = "";
                                    for (int x = 0; x < scope.Count; x++)
                                    {
                                        var t = scope.ElementAt(i);
                                        if (t.type == "group")
                                        {
                                            group = t.name;
                                            break;
                                        }
                                    }

                                    if (group == "")
                                    {
                                        group = globalclass;
                                    }

                                    groups[group].functions.Add(name, new Function(name, returnType, arguments));
                                    groups[group].functions[name].isStatic = stat;
                                }
                            }
                            else
                            {
                                groups[globalclass].functions.Add(name, new Function(name, returnType, arguments));
                                groups[globalclass].functions[name].isStatic = stat;
                            }

                            scope.Push(new StackHolder("function", name, currrentScopeHolder + 1));
                            continue;
                        }
                    }
                    else if (token == "main")
                    {
                        if (scope.Count > 0)
                        {
                            var scopePosition = scope.Peek();
                            if (scopePosition.type == "group")
                            {
                                groups[scopePosition.name].main = true;
                            }
                            else
                            {
                                string group = "";
                                for (int x = 0; x < scope.Count; x++)
                                {
                                    var t = scope.ElementAt(i);
                                    if (t.type == "group")
                                    {
                                        group = t.name;
                                        break;
                                    }
                                }

                                if (group == "")
                                {
                                    group = globalclass;
                                }

                                groups[group].main = true;
                            }
                        }
                        else
                        {
                            groups[globalclass].main = true;
                        }

                        if (getElementAtID(i + 1) == ";")
                        {
                            i += 1;
                        }
                    }
                    else if (token == ";")
                    {
                        if (standardCode != "")
                        {
                            if (scope.Count > 0)
                            {
                                var scopePosition = scope.Peek();
                                if (scopePosition.type == "group")
                                {
                                    groups[scopePosition.name].functions[scopePosition.name].code.Add(standardCode);
                                    groups[scopePosition.name].functions[scopePosition.name].code.Add(";");
                                }
                                else if (scopePosition.type == "function")
                                {
                                    string group = "";
                                    for (int x = 0; x < scope.Count; x++)
                                    {
                                        var t = scope.ElementAt(x);
                                        if (t.type == "group")
                                        {
                                            group = t.name;
                                            break;
                                        }
                                    }

                                    if (group == "")
                                    {
                                        group = globalclass;
                                    }

                                    groups[group].functions[scopePosition.name].code.Add(standardCode);
                                    groups[group].functions[scopePosition.name].code.Add($";");
                                }
                            }
                            else
                            {
                                groups[globalclass].functions[globalclass].code.Add(standardCode);
                                groups[globalclass].functions[globalclass].code.Add(";");
                            }
                        }
                        standardCode = "";
                    }
                    else if (token == "{")
                    {
                        currrentScopeHolder += 1;
                        if (scope.Peek().hold != currrentScopeHolder)
                        {
                            standardCode += "{\n";
                        }

                        if (standardCode != "")
                        {
                            if (scope.Count > 0)
                            {
                                var scopePosition = scope.Peek();
                                if (scopePosition.type == "group")
                                {
                                    groups[scopePosition.name].functions[scopePosition.name].code.Add(standardCode);
                                }
                                else if (scopePosition.type == "function")
                                {
                                    string group = "";
                                    for (int x = 0; x < scope.Count; x++)
                                    {
                                        var t = scope.ElementAt(x);
                                        if (t.type == "group")
                                        {
                                            group = t.name;
                                            break;
                                        }
                                    }

                                    if (group == "")
                                    {
                                        group = globalclass;
                                    }

                                    groups[group].functions[scopePosition.name].code.Add(standardCode);
                                }
                            }
                            else
                            {
                                groups[globalclass].functions[globalclass].code.Add(standardCode);
                            }
                        }
                        standardCode = "";
                    }
                    else if (token == "}")
                    {
                        var t = scope.Peek();
                        currrentScopeHolder--;
                        if (t.hold != null && currrentScopeHolder < t.hold)
                        {
                            scope.Pop();
                        }
                        else
                        {
                            standardCode += "}\n";
                        }

                        if (standardCode != "")
                        {
                            if (scope.Count > 0)
                            {
                                var scopePosition = scope.Peek();
                                if (scopePosition.type == "group")
                                {
                                    groups[scopePosition.name].functions[scopePosition.name].code.Add(standardCode);
                                }
                                else if (scopePosition.type == "function")
                                {
                                    string group = "";
                                    for (int x = 0; x < scope.Count; x++)
                                    {
                                        var Y = scope.ElementAt(x);
                                        if (Y.type == "group")
                                        {
                                            group = Y.name;
                                            break;
                                        }
                                    }

                                    if (group == "")
                                    {
                                        group = globalclass;
                                    }

                                    groups[group].functions[scopePosition.name].code.Add(standardCode);
                                }
                            }
                            else
                            {
                                groups[globalclass].functions[globalclass].code.Add(standardCode);
                            }
                        }
                        standardCode = "";
                    }
                    else
                    {
                        bool complete = false;
                        foreach (var t in converter.tokensThatDontgetASpace)
                        {
                            if (token == t)
                            {
                                standardCode += token;
                                complete = true;
                                break;
                            }
                        }

                        if (!complete)
                        {
                            standardCode += token += " ";
                        }

                    }

                }

                for (int g = 0; g < groups.Count; g++)
                {
                    for (int f = 0; f < groups.ElementAt(g).Value.functions.Count; f++)
                    {
                        Function fun = groups.ElementAt(g).Value.functions.ElementAt(f).Value;
                        string argument = "";
                        if (fun.arguments.Length > 0)
                        {
                            string[] args = fun.arguments.Split(',');
                            string name = "";
                            for (int x = 0; x < args.Length; x++)
                            {
                                var s = args[x];
                                string[] v = s.Split(':');
                                if (!s.StartsWith("ref"))
                                {
                                    if (v.Length > 1)
                                    {
                                        if (!v[1].Contains("<"))
                                        {
                                            argument += $"{v[1]} {v[0]}{((x == args.Length - 1) ? "" : ",")}";
                                        }
                                        else
                                        {
                                            argument += $"{v[1]}";
                                            name = v[0];
                                        }
                                    }
                                    else
                                    {
                                        if (x == args.Length - 1)
                                        {
                                            argument += $",{v[0]} {name}";
                                        }
                                        else if (x + 1 < args.Length)
                                        {
                                            var y = args[x+1];
                                            string[] yy = y.Split(':');
                                            if(yy.Length > 1)
                                            {
                                                argument += $",{v[0]} {name}";
                                            }
                                            else
                                            {
                                                argument += $",{v[0]}";
                                            }
                                        }
                                        
                                    }
                                }
                                else
                                {
                                    s = args[x];
                                    v = s.Substring(s.IndexOf("ref") + 3).Split(':');
                                    

                                    if (v.Length > 1)
                                    {
                                        if (!v[1].Contains("<"))
                                        {
                                            argument += $"ref {v[1]} {v[0]}{((x == args.Length - 1) ? "" : ",")}";
                                        }
                                        else
                                        {
                                            argument += $"ref {v[1]}";
                                            name = v[0];
                                        }
                                    }
                                    else
                                    {
                                        if (x == args.Length - 1)
                                        {
                                            argument += $",{v[0]} {name}";
                                        }
                                        else if (x + 1 < args.Length)
                                        {
                                            var y = args[x + 1];
                                            string[] yy = y.Split(':');
                                            if (yy.Length > 1)
                                            {
                                                argument += $",{v[0]} {name}";
                                            }
                                            else
                                            {
                                                argument += $",{v[0]}";
                                            }
                                        }

                                    }
                                }
                                
                            }
                        }
                        groups[groups.ElementAt(g).Key].functions[fun.name].arguments = argument;
                    }
                }

                outputFile = "";

                foreach (KeyValuePair<string, Using> u in usingStatements)
                {
                    outputFile += $"using {u.Value.location};\n";
                }
                foreach (KeyValuePair<string, Group> n in groups)
                {
                    outputFile += $"namespace {((n.Value.set == null) ? globalclass : n.Value.set)}{{\n";
                    outputFile += $"public partial class {n.Value.name}{{\n";
                    foreach (KeyValuePair<string, Variable> v in n.Value.variables)
                    {
                        outputFile += $"public {((v.Value.isStatic) ? "static" : "")} {v.Value.datatype} {v.Value.name} {((v.Value.equals == null) ? "" : $" = {((v.Value.isNew) ? $"new {v.Value.datatype}" : "")}{v.Value.equals}")};\n";
                    }

                    if (n.Value.main == true)
                    {
                        outputFile += $"public static void Main(string[] args){{ new {n.Value.name}(); }}";
                    }

                    foreach (KeyValuePair<string, Function> f in n.Value.functions)
                    {
                        if (f.Value.name != n.Value.name || (f.Value.name == n.Value.name && f.Value.code.Count != 0))
                        {
                            if (f.Value.name == n.Value.name)
                            {
                                outputFile += $"public {f.Value.name} ({f.Value.arguments}) {{\n";
                            }
                            else
                            {
                                outputFile += $"public {((f.Value.isStatic) ? "static" : "")} {f.Value.returnType} {f.Value.name} ({f.Value.arguments}) {{\n";
                            }
                            foreach (string l in f.Value.code)
                            {
                                outputFile += $"{l}";
                            }
                            outputFile += "\n}";
                        }
                    }
                    outputFile += "\n}";
                    outputFile += "\n}";
                }

                //public struct checker
                /*for (int i = 0; i < fileTokens.Count; i++)
                {
                    var token = getElementAtID(i);
                    if (token == "group")
                    {
                        string location = "";
                        for (int a = i + 1; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ";")
                            {
                                location += d;
                            }
                            else
                            {
                                i = a + 1;
                                break;
                            }
                        }
                        scope.Push(new StackHolder("group", location));
                    }
                    if (token == "data")
                    {
                        string name = getElementAtID(++i);
                        if (name == "(")
                        {
                            name = $"data{dataCounter++}";
                            fileTokens.Insert(i++, name);
                            break;
                        }
                        else
                        {
                            i++;
                        }

                        string arguments = "";
                        for (int a = i; a < int.MaxValue; a++)
                        {
                            string d = getElementAtID(a);
                            if (d != null && d != ")")
                            {
                                arguments += d;
                            }
                            else
                            {
                                i = a;
                                break;
                            }
                        }
                        StackHolder s = null;
                        foreach(StackHolder g in scope)
                        {
                            if(g.type == "group")
                            {
                                s = g;
                                break;
                            }
                        }
                        groups[s.name].structs.Add(name, new Structs(name));
                    }
                }*/
            }
            compiled = outputFile;
        }

        private void clearStack()
        {
            scope = new Stack<StackHolder>();
            scope.Push(new StackHolder("group", globalclass));
        }

        public string getElementAtID(int id)
        {
            if (fileTokens.Count > id)
            {
                return fileTokens[id];
            }
            return null;
        }
    }

    public class Variable
    {
        public string name, datatype, equals;
        public bool isStatic = false;
        public bool isNew = false;

        public Variable(string name, string datatype, string equals)
        {
            this.name = name;
            this.datatype = datatype;
            this.equals = equals;
            isNew = false;
        }

        public Variable(string name, string datatype, string equals, bool isNew)
        {
            this.name = name;
            this.datatype = datatype;
            this.equals = equals;
            this.isNew = isNew;
        }
    }

    public class Structs
    {
        public string name;
        public List<Variable> types;

        public Structs(string name)
        {
            this.name = name;
            types = new List<Variable>();
        }
    }

    public class Using
    {
        public string location;

        public Using(string location)
        {
            this.location = location;
        }
    }

    public class Function
    {
        public string name, returnType, arguments;
        public bool isStatic = false;
        public List<string> code;

        public Function(string name, string returnType, string arguments)
        {
            this.name = name;
            this.returnType = returnType;
            this.arguments = arguments;
            code = new List<string>();
        }
    }

    public class Group
    {
        public string name;
        public string set = null;
        public bool main;
        public Dictionary<string, Variable> variables;
        public Dictionary<string, Function> functions;
        public Dictionary<string, Structs> structs;


        public Group(string name)
        {
            this.name = name;
            variables = new Dictionary<string, Variable>();
            functions = new Dictionary<string, Function>();
            structs = new Dictionary<string, Structs>();
            main = false;
        }
    }

    public class StackHolder
    {
        public string type;
        public string name;
        public int? hold;

        public StackHolder(string type, string name)
        {
            this.type = type;
            this.name = name;
            hold = null;
        }

        public StackHolder(string type, string name, int hold)
        {
            this.type = type;
            this.name = name;
            this.hold = hold;
        }
    }
}

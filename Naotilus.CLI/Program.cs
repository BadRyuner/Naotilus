using System.Text;
using AsmResolver.PE.File;

namespace Naotilus.CLI;

internal class Program
{
    static void Main(string[] args)
    {
#if false
        var path = "D:\\Work\\sandbox\\ConsoleApp1\\bin\\Release\\net8.0\\win-x64\\native\\ConsoleApp.exe";
#else
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: .\\Naotilus.CLI.exe \"path/to/native_compiled.exe\"");
            return;
        }

        var path = args[0];
#endif
        var pe = PEFile.FromFile(path);
        var naot = NaotAssembly.FromPeFile(pe);
        SaveAsPythonScript(naot, path.Replace(".exe", ".py"));
    }

    static void SaveAsPythonScript(NaotAssembly ass, string savePath)
    {
        using var file = File.Open(savePath, FileMode.Create);
        using var writer = new StreamWriter(file);
        writer.WriteLine("from ghidra.app.util.cparser.C import CParserUtils");
        writer.WriteLine("from ghidra.app.util.cparser.C import CParser");
        writer.WriteLine("from ghidra.app.cmd.function import ApplyFunctionSignatureCmd\n");
        writer.WriteLine("functionManager = currentProgram.getFunctionManager()");
        writer.WriteLine("baseAddress = currentProgram.getImageBase()");
        writer.WriteLine("USER_DEFINED = ghidra.program.model.symbol.SourceType.USER_DEFINED\n");
        writer.WriteLine("ST_DEFAULT = ghidra.program.model.symbol.SourceType.DEFAULT");
        writer.WriteLine("def at_rva(addr):\n\treturn baseAddress.add(addr)\n");
        writer.WriteLine("def make_function(start):\n\tfunc = getFunctionAt(start)\n\tif func is None:\n\t\tfunc = createFunction(start, None)\n\treturn func\n");

        foreach (var method in ass.IterateAllMethods())
        {
            if (method.EntryRVA == 0) continue;
            writer.WriteLine($"make_function(at_rva({method.EntryRVA})).setName(\"{method.Name}\", ST_DEFAULT)");
        }

        writer.WriteLine("data_type_manager = currentProgram.getDataTypeManager()");
        writer.WriteLine("parser = CParser(data_type_manager)");

        writer.WriteLine(""""
                         methodTableStruct = parser.parse("""
                         struct methodTable {
                            unsigned int m_uFlags;
                            unsigned int m_uBaseSize;
                            unsigned int m_RelatedType;
                            unsigned short m_usNumVtableSlots;
                            unsigned short m_usNumInterfaces;
                            unsigned int m_uHashCode;
                            void* m_VTable[0x0];
                         };""")
                         """");

        foreach (var tuple in ass.MethodTablesByRva)
        {
            var rva = tuple.Key;
            var table = tuple.Value;
            writer.WriteLine($"createData(at_rva({rva}), methodTableStruct)");
            writer.WriteLine($"createLabel(at_rva({rva}), \"\"\"{table.MyTypeDef.Name}\"\"\", True, USER_DEFINED)");
        }

        foreach (var tuple in ass.StringTable)
        {
            writer.WriteLine($"createLabel(at_rva({tuple.Key}), \"\"\"STRING_{Sanitize(tuple.Value)}\"\"\", True, USER_DEFINED)");
        }
    }

    static unsafe string Sanitize(string str)
    {
        fixed(char* c = str)
        {
            char* p = c;;
            while (*p != '\0')
            {
                if (*p < '0' || *p > '~'  || *p == '\\')
                    *p = '?';
                p++;
            }
        }

        return str;
    }
}

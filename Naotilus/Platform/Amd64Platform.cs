using Iced.Intel;
using Naotilus.Utils;
using Naotilus.Lift;

namespace Naotilus.Platform;
internal sealed unsafe class Amd64Platform : BasePlatform
{
    public Amd64Platform(NaotAssembly ass) : base(ass) { }

    internal override uint GetRTRHeader()
    {
        var entry = Assembly.PeFile.OptionalHeader.AddressOfEntryPoint;
        var entry_jmp = entry + 0xD;
        var code = new IcedWrapper(Assembly.PeFile.CreateReaderAtRva(entry_jmp));
        var disassembler = Decoder.Create(64, code, entry_jmp);
        Instruction instruction;
        disassembler.Decode(out instruction);

        var __scrt_common_main_seh = instruction.NearBranchTarget;

        code.Reader = Assembly.PeFile.CreateReaderAtRva((uint)__scrt_common_main_seh + 0x107);
        disassembler.IP = __scrt_common_main_seh + 0x107;
        disassembler.Decode(out instruction);

        var wmain = instruction.NearBranchTarget;

        code.Reader = Assembly.PeFile.CreateReaderAtRva((uint)wmain + 0x7E);
        disassembler.IP = wmain + 0x7E;
        disassembler.Decode(out instruction);

        var __modules_a = instruction.MemoryDisplacement64;

        var reader = Assembly.PeFile.CreateReaderAtRva((uint)__modules_a);
        ulong RTRHeader = 0;
        while (RTRHeader == 0)
            RTRHeader = reader.ReadUInt64();

        uint RTRHeaderRVA = (uint)(RTRHeader - Assembly.PeFile.OptionalHeader.ImageBase);

        reader = Assembly.PeFile.CreateReaderAtRva(RTRHeaderRVA);

        code.Reader = Assembly.PeFile.CreateReaderAtRva((uint)wmain + 0xAB);
        disassembler.IP = wmain + 0xAB;
        disassembler.Decode(out instruction);

        var startupCodeMain = instruction.NearBranchTarget;

        code.Reader = Assembly.PeFile.CreateReaderAtRva((uint)startupCodeMain);
        disassembler.IP = startupCodeMain;
        disassembler.Decode(out instruction);

        while (instruction.Mnemonic != Mnemonic.Jmp)
        {
            disassembler.Decode(out instruction);
            if (instruction.Mnemonic == Mnemonic.Call)
            {
                var saved = disassembler.IP;
                disassembler.Decode(out instruction);
                if (instruction.Mnemonic == Mnemonic.Mov && instruction.Op0Register == Register.RCX && instruction.Op1Register == Register.RAX)
                {
                    disassembler.Decode(out instruction);
                    if (instruction.Mnemonic == Mnemonic.Call)
                    {
                        EntryPoint = (uint)instruction.NearBranchTarget;
                        break;
                    }
                }
                else
                {
                    disassembler.IP = saved;
                    code.Reader.Rva = (uint)saved;
                }
            }
        }

        if (reader.ReadAsciiString() == "RTR")
            return RTRHeaderRVA;

        throw new Exception("Bad Image");
    }

    internal override LiftedFunction LiftFunction(uint at)
    {
        LiftedFunction func = new() { RVA = at };
        var code = new IcedWrapper(Assembly.PeFile.CreateReaderAtRva(at));
        var disassembler = Decoder.Create(64, code, at);

        //var tr = new Echo.Platforms.Iced.X86StateTransitioner(_architecture);
        //var res = new Echo.Platforms.Iced.X86StaticSuccessorResolver();
        //var a = new Echo.Platforms.Iced.X86DecoderInstructionProvider();

        //var asta = _architecture.ToAst();

        return func;
    }
}

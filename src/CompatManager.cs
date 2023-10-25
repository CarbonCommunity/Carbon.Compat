using System.Diagnostics;
using System.Runtime.CompilerServices;
using API.Abstracts;
using API.Assembly;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using Carbon.Compat.Converters;
using Carbon.Components;
using Carbon.Extensions;
using Network;
using Defines = Carbon.Core.Defines;

[assembly: InternalsVisibleTo("Carbon.Bootstrap")]

namespace Carbon.Compat;

#pragma warning disable
public class CompatManager : CarbonBehaviour, ICompatManager
{
	private BaseConverter oxideConverter = new OxideConverter();

	private BaseConverter harmonyConverter = new HarmonyConverter();

	private ModuleReaderParameters readerArgs = new ModuleReaderParameters(EmptyErrorListener.Instance);

    public static AssemblyReference SDK = new AssemblyReference("Carbon.SDK", new Version(0, 0, 0, 0));

    public static AssemblyReference Common = new AssemblyReference("Carbon.Common", new Version(0, 0, 0, 0));

    public static AssemblyReference Newtonsoft = new AssemblyReference("Newtonsoft.Json", new Version(0, 0, 0, 0));

    public static AssemblyReference protobuf = new AssemblyReference("protobuf-net", new Version(0, 0, 0, 0));

    public static AssemblyReference protobufCore = new AssemblyReference("protobuf-net.Core", new Version(0, 0, 0, 0));

	#pragma warning disable

	private bool ConvertAssembly(ModuleDefinition md, BaseConverter converter, ref byte[] buffer)
    {
        using (TimeMeasure.New($"{converter.Name} assembly conversion for '{md.Name}'", 1))
        {
	        try
	        {
		        buffer = converter.Convert(md); //, out BaseConverter.GenInfo info);
	        }
	        catch (Exception ex)
	        {
		        Logger.Error($"Failed to convert assembly {md.Name}", ex);
		        buffer = null;
		        return false;
	        }
        }

        string dir = Path.Combine(Defines.GetTempFolder(), "compat_debug_gen");
	    Directory.CreateDirectory(dir);
	    OsEx.File.Create(Path.Combine(dir, md.Name + ".dll"), buffer);

	    return true;
    }

    ConversionResult ICompatManager.AttemptOxideConvert(ref byte[] data)
    {
	    ModuleDefinition asm = ModuleDefinition.FromBytes(data, readerArgs);
	    if (!asm.AssemblyReferences.Any(Helpers.IsOxideASM)) return ConversionResult.Skip;
	    return ConvertAssembly(asm, oxideConverter, ref data) ? ConversionResult.Success : ConversionResult.Fail;
    }

    bool ICompatManager.ConvertHarmonyMod(ref byte[] data)
    {
	    ModuleDefinition asm = ModuleDefinition.FromBytes(data, readerArgs);
	    return ConvertAssembly(asm, harmonyConverter, ref data);
    }
}

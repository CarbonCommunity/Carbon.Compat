using System.Diagnostics;
using System.Runtime.CompilerServices;
using API.Abstracts;
using API.Assembly;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using Carbon.Compat.Converters;
using Carbon.Extensions;
using Facepunch;
using Defines = Carbon.Core.Defines;

[assembly: InternalsVisibleTo("Carbon.Bootstrap")]

namespace Carbon.Compat;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class CompatManager : CarbonBehaviour, ICompatManager
{
	private BaseConverter oxideConverter = new OxideConverter();

	private BaseConverter harmonyConverter = new HarmonyConverter();

	private ModuleReaderParameters readerArgs = new ModuleReaderParameters(EmptyErrorListener.Instance);

	private static Version zeroVersion = new Version(0,0,0,0);

    public static AssemblyReference SDK = new AssemblyReference("Carbon.SDK", zeroVersion);

    public static AssemblyReference Common = new AssemblyReference("Carbon.Common", zeroVersion);

    public static AssemblyReference Newtonsoft = new AssemblyReference("Newtonsoft.Json", zeroVersion);

    public static AssemblyReference protobuf = new AssemblyReference("protobuf-net", zeroVersion);

    public static AssemblyReference protobufCore = new AssemblyReference("protobuf-net.Core", zeroVersion);

    public static AssemblyReference wsSharp = new AssemblyReference("websocket-sharp", zeroVersion);

    private bool ConvertAssembly(ModuleDefinition md, BaseConverter converter, ref byte[] buffer)
    {
	    Stopwatch stopwatch = Pool.Get<Stopwatch>();
	    stopwatch.Start();

	    md.DebugData.Clear();

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

	    Logger.Log($" {converter.Name} assembly conversion for '{md.Name}' took {stopwatch.ElapsedMilliseconds:0}ms");

	    stopwatch.Reset();
	    Pool.Free(ref stopwatch);

#if DEBUG
	    string dir = Path.Combine(Defines.GetTempFolder(), "compat_debug_gen");
	    Directory.CreateDirectory(dir);
	    OsEx.File.Create(Path.Combine(dir, md.Name + ".dll"), buffer);
#endif
	    return true;
    }

    ConversionResult ICompatManager.AttemptOxideConvert(ref byte[] data)
    {
	    ModuleDefinition asm = ModuleDefinition.FromBytes(data, readerArgs);

	    if (!asm.AssemblyReferences.Any(Helpers.IsOxideASM))
	    {
		    return ConversionResult.Skip;
	    }

	    return ConvertAssembly(asm, oxideConverter, ref data) ? ConversionResult.Success : ConversionResult.Fail;
    }

    bool ICompatManager.ConvertHarmonyMod(ref byte[] data)
    {
	    return ConvertAssembly(ModuleDefinition.FromBytes(data, readerArgs), harmonyConverter, ref data);
    }
}

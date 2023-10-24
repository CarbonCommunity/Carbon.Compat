using System.Diagnostics;
using System.Runtime.CompilerServices;
using API.Abstracts;
using API.Assembly;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using Carbon.Compat.Converters;
using Carbon.Extensions;
using Network;
using Defines = Carbon.Core.Defines;

[assembly: InternalsVisibleTo("Carbon.Bootstrap")]

namespace Carbon.Compat;

#pragma warning disable
internal sealed class CompatManager : CarbonBehaviour, ICompatManager
{
	public static CompatManager Manager;
	public const string BuildConfiguration =
        #if DEBUG
            "Debug"
        #else
            "Release"
		#endif
		;

	public CompatManager()
	{
		Manager = this;
	}

	private BaseConverter oxideConverter = new OxideConverter();

	private BaseConverter harmonyConverter = new HarmonyConverter();

	private ModuleReaderParameters readerArgs = new ModuleReaderParameters(EmptyErrorListener.Instance);

    public AssemblyReference SDK = new AssemblyReference("Carbon.SDK", new Version(0, 0, 0, 0));

    public AssemblyReference Common = new AssemblyReference("Carbon.Common", new Version(0, 0, 0, 0));

    public AssemblyReference Newtonsoft = new AssemblyReference("Newtonsoft.Json", new Version(0, 0, 0, 0));

    public AssemblyReference protobuf = new AssemblyReference("protobuf-net", new Version(0, 0, 0, 0));

    public AssemblyReference protobufCore = new AssemblyReference("protobuf-net.Core", new Version(0, 0, 0, 0));

	#pragma warning disable
	private bool ConvertAssembly(ModuleDefinition md, string asmName, BaseConverter converter, ref byte[] op)
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
	        op = converter.Convert(md); //, out BaseConverter.GenInfo info);
        }
        catch (Exception ex)
        {
	        Logger.Error($"Failed to convert assembly {asmName}", ex);
	        op = null;
	        return false;
        }

        //VerificationResult[] convertedResults = ILVerifier.VerifyAssembly(data, true);
        //ILVerifier.ProcessResults(originalResults, convertedResults, md, info.mappings);
        // TODO: reference assembly generation
        /*if (CCLEntrypoint.InitialConfig?.Development?.ReferenceAssemblies != null &&
            CCLEntrypoint.InitialConfig.Development.DevMode &&
            CCLEntrypoint.InitialConfig.Development.ReferenceAssemblies.Contains(md.Assembly?.Name))
        {
            Logger.Info($"Generating reference assembly for {name}");
            try
            {
                File.WriteAllBytes(Path.Combine(RootDir, "ReferenceAssemblies", fileName), ReferenceAssemblyGenerator.ConvertToReferenceAssembly(data));
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to generate reference assembly for {name}: \n{e}");
            }
        }*/
        sw.Stop();
        Logger.Debug($"Converted {converter.Name} assembly {asmName} in {sw.Elapsed.TotalMilliseconds:n0}ms", 1);
    #if DEBUG
	    if (Debug)
	    {
		    string dir = Path.Combine(Defines.GetTempFolder(), "compat_debug_gen");
		    Directory.CreateDirectory(dir);
		    OsEx.File.Create(Path.Combine(dir, asmName + ".dll"), op);
	    }
    #endif
	    return true;
    }

    public void Initialize(string selfName)
    {
	    //ILVerifier.Init();

        /*if (CCLEntrypoint.InitialConfig?.Development?.ReferenceAssemblies != null && CCLEntrypoint.InitialConfig.Development.DevMode && CCLEntrypoint.InitialConfig.Development.ReferenceAssemblies.Count > 0)
        {
            Directory.CreateDirectory(Path.Combine(RootDir, "ReferenceAssemblies"));
        }*/
        /*foreach (Type type in GetTypesWithoutError(Assembly.GetExecutingAssembly()).Where(x => typeof(BaseConverter).IsAssignableFrom(x) && !x.IsAbstract))
        {
            BaseConverter cv = (BaseConverter)Activator.CreateInstance(type);
            if (Converters.TryGetValue(cv.Path, out BaseConverter dup))
            {
                Logger.Error($"Duplicate converter {type.FullName} > {dup.GetType().FullName}");
                continue;
            }

            cv.FullPath = Path.Combine(RootDir, cv.Path);
            Logger.Info($"Adding converter {cv.Path} : {cv.FullPath} > {type.FullName}");
            Directory.CreateDirectory(cv.FullPath);
            Converters.Add(cv.Path, cv);
        }*/
    }

    public bool Debug { get; set; }

    OxideResult ICompatManager.AttemptOxideConvert(ref byte[] data, string asmName)
    {
	    ModuleDefinition asm = ModuleDefinition.FromBytes(data, readerArgs);
	    if (!asm.AssemblyReferences.Any(Helpers.IsOxideASM)) return OxideResult.Skip;
	    return ConvertAssembly(asm, asmName, oxideConverter, ref data) ? OxideResult.Success : OxideResult.Fail;
    }

    bool ICompatManager.ConvertHarmonyMod(ref byte[] data, string asmName)
    {
	    ModuleDefinition asm = ModuleDefinition.FromBytes(data, readerArgs);
	    return ConvertAssembly(asm, asmName, harmonyConverter, ref data);
    }
}

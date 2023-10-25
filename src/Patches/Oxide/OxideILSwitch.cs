using System.Reflection;
using Carbon.Compat.Converters;
using Carbon.Compat.Lib;
using HarmonyLib;
using Oxide.Core.Libraries;
using Oxide.Plugins;

namespace Carbon.Compat.Patches.Oxide;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class OxideILSwitch : BaseOxidePatch
{
	public static CompatManager Singleton => Community.Runtime.Compat as CompatManager;

    private static MethodInfo pluginLoaderMethod = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.RegisterPluginLoader));
    private static MethodInfo consoleCommand1 = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.AddConsoleCommand1));
    private static MethodInfo chatCommand1 = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.AddChatCommand1));
    private static MethodInfo getExtensionDirectory = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.GetExtensionDirectory));
    private static MethodInfo timerOnce = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.TimerOnce));
    private static MethodInfo timerRepeat = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.TimerRepeat));

    private static MethodInfo onAddedToManagerCompat = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.OnAddedToManagerCompat));
    private static MethodInfo onRemovedFromManagerCompat = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.OnRemovedFromManagerCompat));

    private static FieldInfo rustPluginTimer = AccessTools.Field(typeof(RustPlugin), "timer");

    private static readonly MethodInfo carbonLangGetMessage;
    private static int carbonLangGetMessageArgLength;

    static OxideILSwitch()
    {
        carbonLangGetMessage = typeof(Lang).GetMethods().First(x => x.Name == "GetMessage" && x.ReturnType == typeof(string));
        carbonLangGetMessageArgLength = carbonLangGetMessage.GetParameters().Length;
    }
    public override void Apply(ModuleDefinition assembly, ReferenceImporter importer, BaseConverter.Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            var classIsRustPlugin = type.IsBaseType(x => x.Name == "RustPlugin" && x.DefinitionAssembly().Name == "Carbon.Common");
            var classOxideExtension = type.IsBaseType(x => x.FullName == "Oxide.Core.Extensions.Extension" && x.DefinitionAssembly().Name == "Carbon.Common");

            foreach (var method in type.Methods)
            {
                var isRustPluginInstance = !method.IsStatic && classIsRustPlugin;

                if (method.MethodBody is not CilMethodBody body)
                {
	                continue;
                }

                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    var cil = body.Instructions[index];
                    // IL Patches

                    // plugin loader
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference aref &&
                        aref.Name == "RegisterPluginLoader" &&
                        aref.Parent is TypeReference atw &&
                        atw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        cil.OpCode = CilOpCodes.Call;
                        cil.Operand = importer.ImportMethod(pluginLoaderMethod);
                        body.Instructions.Insert(index++, new CilInstruction(!method.IsStatic && classOxideExtension ? CilOpCodes.Ldarg_0 : CilOpCodes.Ldnull));
                        continue;
                    }

                    // add console command
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference bref &&
                        bref.Name == "AddConsoleCommand" &&
                        bref.Parent is TypeReference btw &&
                        btw.FullName == "Oxide.Game.Rust.Libraries.Command" &&
                        bref.Signature is MethodSignature asig &&
                        asig.ParameterTypes.Count == 3 &&
                        asig.ParameterTypes[0].ElementType == ElementType.String &&
                        asig.ParameterTypes[1].FullName == "Oxide.Core.Plugins.Plugin" &&
                        asig.ParameterTypes[2].FullName == "System.Func`2<ConsoleSystem+Arg, System.Boolean>" &&
                        btw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        cil.OpCode = CilOpCodes.Call;
                        cil.Operand = importer.ImportMethod(consoleCommand1);
                        continue;
                    }

                    // add chat command
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference cref &&
                        cref.Name == "AddChatCommand" &&
                        cref.Parent is TypeReference ctw &&
                        ctw.FullName == "Oxide.Game.Rust.Libraries.Command" &&
                        cref.Signature is MethodSignature bsig &&
                        bsig.ParameterTypes.Count == 3 &&
                        bsig.ParameterTypes[0].ElementType == ElementType.String &&
                        bsig.ParameterTypes[1].FullName == "Oxide.Core.Plugins.Plugin" &&
                        ctw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        switch (bsig.ParameterTypes[2].FullName)
                        {
                            case "System.Action`3<BasePlayer, System.String, System.String[]>":
                                cil.Operand = importer.ImportMethod(chatCommand1);
                                goto cend;
                            default:
                                continue;
                        }
                        cend:
                        cil.OpCode = CilOpCodes.Call;
                        continue;
                    }

                    // remove RegisterLibrary calls
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference dref &&
                        dref.Name == "RegisterLibrary" &&
                        dref.Parent is TypeReference dtw &&
                        dtw.FullName == "Oxide.Core.Extensions.ExtensionManager" &&
                        dtw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        cil.OpCode = CilOpCodes.Pop;
                        cil.Operand = null;
                        body.Instructions.InsertRange(index, new CilInstruction[]
                        {
                            new CilInstruction(CilOpCodes.Pop),
                            new CilInstruction(CilOpCodes.Pop)
                        });
                        index+=2;
                        continue;
                    }


                    // extension paths
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference eref &&
                        eref.Name == "get_ExtensionDirectory" &&
                        eref.Parent is TypeReference etw &&
                        etw.FullName == "Oxide.Core.OxideMod" &&
                        etw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        cil.OpCode = CilOpCodes.Call;
                        cil.Operand = importer.ImportMethod(getExtensionDirectory);
                        continue;
                    }

                    // timer call fix
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference fref &&
                        fref.Signature is MethodSignature fsig &&
                        fref.Parent is TypeReference ftw &&
                        ftw.FullName == "Oxide.Plugins.Timers" &&
                        fsig.ParameterTypes[^1].FullName == "Oxide.Core.Plugins.Plugin" &&
                        ftw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        switch (fref.Name.ToString())
                        {
                            case "Once":
                                cil.Operand = importer.ImportMethod(timerOnce);
                                goto cend;
                            case "Repeat":
                                cil.Operand = importer.ImportMethod(timerRepeat);
                                goto cend;
                            default:
                                continue;
                        }
                        cend:
                        cil.OpCode = CilOpCodes.Call;
                        continue;
                    }

                    // change GetLibrary<Oxide.Plugins.Timers> to this.timer
                    if (isRustPluginInstance && cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MethodSpecification gspec &&
                        gspec.Method is MemberReference gref &&
                        gref.Parent is TypeReference gtw &&
                        gref.Name == "GetLibrary" &&
                        gtw.FullName == "Oxide.Core.OxideMod" &&
                        gspec.Signature.TypeArguments.Count == 1 &&
                        gspec.Signature.TypeArguments[0].FullName == "Oxide.Plugins.Timers" &&
                        gtw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        cil.OpCode = CilOpCodes.Pop;
                        cil.Operand = null;
                        body.Instructions.InsertRange(++index, new CilInstruction[]
                        {
                            new CilInstruction(CilOpCodes.Pop),
                            new CilInstruction(CilOpCodes.Ldarg_0),
                            new CilInstruction(CilOpCodes.Ldfld, importer.ImportField(rustPluginTimer))
                        });
                        continue;
                    }

                    // PluginManagerEvent fix
                    if (cil.OpCode == CilOpCodes.Ldfld &&
                        cil.Operand is MemberReference href &&
                        href.Signature is FieldSignature hsig &&
                        href.Parent is TypeReference htw &&
                        htw.FullName == "Oxide.Core.Plugins.Plugin" &&
                        hsig.FieldType.FullName == "Oxide.Core.Plugins.PluginManagerEvent" &&
                        htw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        switch (href.Name.ToString())
                        {
                            case "OnAddedToManager":
                                cil.Operand = importer.ImportMethod(onAddedToManagerCompat);
                                goto cend;
                            case "OnRemovedFromManager":
                                cil.Operand = importer.ImportMethod(onRemovedFromManagerCompat);
                                goto cend;
                            default:
                                continue;
                        }
                        cend:
                        cil.OpCode = CilOpCodes.Call;
                        continue;
                    }

                    // lang.GetMessage fix #blame raul https://github.com/CarbonCommunity/Carbon.Core/commit/ef572b68b989df687c3d764e31968ca3b239ab9e
                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference iref &&
                        iref.Signature is MethodSignature isig &&
                        iref.Parent is TypeReference itw &&
                        itw.FullName == "Oxide.Core.Libraries.Lang" &&
                        iref.Name == "GetMessage" &&
                        carbonLangGetMessageArgLength != 3 &&
                        isig.ParameterTypes.Count == 3 &&
                        isig.ParameterTypes[0].ElementType == ElementType.String &&
                        isig.ParameterTypes[1].FullName == "Carbon.Base.BaseHookable" &&
                        isig.ParameterTypes[2].ElementType == ElementType.String &&
                        itw.DefinitionAssembly().Name == CompatManager.Common.Name)
                    {
                        cil.Operand = importer.ImportMethod(carbonLangGetMessage);
                        for (int idx = 0; idx < carbonLangGetMessageArgLength-isig.ParameterTypes.Count; idx++)
                        {
                            body.Instructions.Insert(index++, new CilInstruction(CilOpCodes.Ldnull));

                        }
                    }
                }
            }
        }
    }
}

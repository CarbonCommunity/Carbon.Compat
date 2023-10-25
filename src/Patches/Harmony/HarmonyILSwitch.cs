using Carbon.Compat.Converters;
using Carbon.Compat.Lib;
using HarmonyLib;

namespace Carbon.Compat.Patches.Harmony;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class HarmonyILSwitch : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.Context context)
    {
        var PatchProcessorCompatRef = importer.ImportMethod(AccessTools.Method(typeof(HarmonyCompat), nameof(HarmonyCompat.PatchProcessorCompat)));

        foreach (var type in asm.GetAllTypes())
        {
            foreach (var method in type.Methods)
            {
	            if (method.MethodBody is not CilMethodBody body)
	            {
		            continue;
	            }

                for (int i = 0; i < body.Instructions.Count; i++)
                {
                    var cil = body.Instructions[i];

                    // IL Patches
                    if (cil.OpCode == CilOpCodes.Call && cil.Operand is MemberReference { FullName: $"{Harmony2NS}.{HarmonyStr} {Harmony2NS}.{HarmonyStr}::Create(System.String)" })
                    {
                        cil.OpCode = CilOpCodes.Newobj;
                        cil.Operand = importer.ImportMethod(AccessTools.Constructor(typeof(HarmonyLib.Harmony), new Type[]{typeof(string)}));
                    }

                    if ((cil.OpCode == CilOpCodes.Newobj) && cil.Operand is MemberReference bref &&
                        bref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        bref.DeclaringType.Name == "PatchProcessor" &&
                        bref.Name == ".ctor")
                    {
                        cil.OpCode = CilOpCodes.Call;
                        cil.Operand = PatchProcessorCompatRef;
                        continue;
                    }

                    if (cil.OpCode == CilOpCodes.Callvirt && cil.Operand is MemberReference cref &&
                        cref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        cref.DeclaringType.Name == "PatchProcessor" &&
                        cref.Name == "Patch")
                    {
                        if (i != 0)
                        {
                            var ccall = body.Instructions[i - 1];

                            if (ccall.OpCode == CilOpCodes.Call && ccall.Operand == PatchProcessorCompatRef)
                            {
                                body.Instructions.RemoveAt(i);

                                var pop = body.Instructions[i];

                                if (pop.OpCode == CilOpCodes.Pop)
                                {
                                    body.Instructions.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

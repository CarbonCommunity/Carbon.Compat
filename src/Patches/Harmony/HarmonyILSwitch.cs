using Carbon.Compat.Converters;
using Carbon.Compat.Lib;
using HarmonyLib;

namespace Carbon.Compat.Patches.Harmony;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023-2024 Patrette
 * All rights reserved.
 *
 */

public class HarmonyILSwitch : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, ref BaseConverter.Context context)
    {
	    if (HarmonyConverter.IsV2Harmony(asm))
	    {
		    return;
	    }

        IMethodDescriptor PatchProcessorCompatRef = importer.ImportMethod(AccessTools.Method(typeof(HarmonyCompat), nameof(HarmonyCompat.PatchProcessorCompat)));

        foreach (TypeDefinition type in asm.GetAllTypes())
        {
            foreach (MethodDefinition method in type.Methods)
            {
	            if (method.MethodBody is not CilMethodBody body)
	            {
		            continue;
	            }

                for (int i = 0; i < body.Instructions.Count; i++)
                {
                    CilInstruction CIL = body.Instructions[i];

                    // IL Patches
                    if (CIL.OpCode == CilOpCodes.Call && CIL.Operand is MemberReference { FullName: $"{Harmony2NS}.{HarmonyStr} {Harmony2NS}.{HarmonyStr}::Create(System.String)" })
                    {
                        CIL.OpCode = CilOpCodes.Newobj;
                        CIL.Operand = importer.ImportMethod(AccessTools.Constructor(typeof(HarmonyLib.Harmony), new Type[]{typeof(string)}));
                    }

                    if ((CIL.OpCode == CilOpCodes.Newobj) && CIL.Operand is MemberReference bref &&
                        bref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        bref.DeclaringType.Name == "PatchProcessor" &&
                        bref.Name == ".ctor")
                    {
                        CIL.OpCode = CilOpCodes.Call;
                        CIL.Operand = PatchProcessorCompatRef;
                        continue;
                    }

                    if (CIL.OpCode == CilOpCodes.Callvirt && CIL.Operand is MemberReference cref &&
                        cref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        cref.DeclaringType.Name == "PatchProcessor" &&
                        cref.Name == "Patch")
                    {
                        if (i != 0)
                        {
                            CilInstruction ccall = body.Instructions[i - 1];

                            if (ccall.OpCode == CilOpCodes.Call && ccall.Operand == PatchProcessorCompatRef)
                            {
                                body.Instructions.RemoveAt(i);

                                CilInstruction pop = body.Instructions[i];

                                if (pop.OpCode == CilOpCodes.Pop)
                                {
                                    body.Instructions.RemoveAt(i);
                                }
                            }
                        }
                    }


                    if ((CIL.OpCode == CilOpCodes.Callvirt || CIL.OpCode ==  CilOpCodes.Call) && CIL.Operand is MemberReference dref &&
                        dref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        dref.Name == "Patch")
                    {
	                    CIL.Operand = importer.ImportMethod(AccessTools.Method(typeof(HarmonyCompat),
		                    nameof(HarmonyCompat.InstancePatchCompat)));
	                    CIL.OpCode = CilOpCodes.Call;

                    }

					// not tested
                    /*if (CIL.OpCode == CilOpCodes.Call && i > 0 && CIL.Operand is MemberReference eref &&
                        eref.DeclaringType.DefinitionAssembly().IsCorLib &&
                        eref.DeclaringType.Name == "RuntimeFeature" &&
                        eref.Name == "IsSupported")
                    {
	                    CilInstruction prev = body.Instructions[i - 1];
	                    if (prev.OpCode == CilOpCodes.Ldstr && prev.Operand is string op && op.Equals("carbon", StringComparison.OrdinalIgnoreCase))
	                    {
		                    CIL.OpCode = CilOpCodes.Ldc_I4_1;
		                    CIL.Operand = null;
		                    body.Instructions.RemoveAt(i-1);
		                    i++;
	                    }
                    }*/
                }
            }
        }
    }
}

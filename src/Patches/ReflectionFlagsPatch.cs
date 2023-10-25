using System.Reflection;
using Carbon.Compat.Converters;

namespace Carbon.Compat.Patches;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class ReflectionFlagsPatch : IAssemblyPatch
{
    public static List<string> ReflectionTypeMethods = new List<string>()
    {
        "GetMethod",
        "GetField",
        "GetProperty",
        "GetMember"
    };

    public void Apply(ModuleDefinition assembly, ReferenceImporter importer, BaseConverter.Context context)
    {
        foreach (var type in assembly.GetAllTypes())
        {
            foreach (var method in type.Methods)
            {
	            if (method.MethodBody is not CilMethodBody body)
	            {
		            continue;
	            }

                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    var cil = body.Instructions[index];

                    if (cil.OpCode == CilOpCodes.Callvirt &&
                        cil.Operand is MemberReference mref &&
                        mref.Signature is MethodSignature msig &&
                        mref.DeclaringType is TypeReference tref &&
                        tref.Scope is AssemblyReference aref &&
                        aref.IsCorLib &&
                        tref.Name == "Type" &&
                        ReflectionTypeMethods.Contains(mref.Name) &&
                        msig.ParameterTypes.Any(x=>x.Scope is AssemblyReference { IsCorLib: true } && x.Name == "BindingFlags")
                       )
                    {
                        for (int li = index - 1; li >= Math.Max(index-5, 0); li--)
                        {
                            var xil = body.Instructions[li];

                            if (!xil.IsLdcI4())
                            {
                                continue;
                            }

                            var flags = (BindingFlags)xil.GetLdcI4Constant() | BindingFlags.Public | BindingFlags.NonPublic;

                            xil.Operand = (object)(int)flags;
                            xil.OpCode = CilOpCodes.Ldc_I4;

                            goto exit;
                        }
                        Logger.Error($"Failed to find binding flags for {method.FullName} at #IL_{cil.Offset:X}:{index} in {assembly.Name}");
                    }

                    exit: ;
                }
            }
        }
    }
}

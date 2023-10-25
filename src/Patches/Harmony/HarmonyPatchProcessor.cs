using AsmResolver;
using AsmResolver.DotNet.Serialized;
using Carbon.Compat.Converters;
using Facepunch;

namespace Carbon.Compat.Patches.Harmony;

/*
 *
 * Copyright (c) 2023 Carbon Community
 * Copyright (c) 2023 Patrette
 * All rights reserved.
 *
 */

public class HarmonyPatchProcessor : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.Context context)
    {
        foreach (var type in asm.GetAllTypes())
        {
            var invalid = false;

            var patches = Pool.GetList<CustomAttribute>();

            foreach (var attr in type.CustomAttributes)
            {
                var declaringType = attr.Constructor?.DeclaringType;

                if (declaringType == null)
                {
	                continue;
                }

                var sig = attr.Signature;

                if (sig == null)
                {
	                continue;
                }

                if (declaringType.Name == "HarmonyPatch" && declaringType.Scope is SerializedAssemblyReference aref && aref.Name == HarmonyASM)
                {
                    if (sig.FixedArguments.Count > 1 && sig.FixedArguments[0].Element is TypeDefOrRefSignature tr && sig.FixedArguments[1].Element is Utf8String ats)
                    {
                        RegisterPatch(tr.DefinitionAssembly().Name, ats, tr.FullName, $"{asm.Assembly.Name} - {type.FullName}");

                        if (!PatchWhitelist.IsPatchAllowed(tr, ats))
                        {
                            invalid = true;
                            patches.Add(attr);
                            break;
                        }
                    }
                    else
                    {
                        if (!PatchWhitelist.IsPatchAllowed(type))
                        {
                            invalid = true;
                            patches.Add(attr);
                            break;
                        }
                    }
                }
            }

            if (invalid)
            {
                type.CustomAttributes.Add(new CustomAttribute(asm.CorLibTypeFactory.CorLibScope.CreateTypeReference("System", "ObsoleteAttribute").CreateMemberReference(".ctor", MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void)).ImportWith(importer)));

                foreach (CustomAttribute attr in patches)
                {
                    type.CustomAttributes.Remove(attr);
                }
            }
        }
    }

    public static void RegisterPatch(string assemblyName, string methodName, string typeName, string reason)
    {
        CurrentPatches.Add(new PatchInfoEntry(assemblyName, methodName, typeName, reason));
    }

    public class PatchInfoEntry
    {
        public string AssemblyName;
        public string TypeName;
        public string MethodName;
        public string Reason;

        public PatchInfoEntry(string assemblyName, string methodName, string typeName, string reason)
        {
            this.AssemblyName = assemblyName;
            this.MethodName = methodName;
            this.TypeName = typeName;
            this.Reason = reason;
        }
    }

    public static List<PatchInfoEntry> CurrentPatches = new();

    public static class PatchWhitelist
    {
        public static bool IsPatchAllowed(TypeDefOrRefSignature type, Utf8String method)
        {
            if (type.FullName == "ServerMgr" && type.Scope is SerializedAssemblyReference aref &&
                aref.Name == "Assembly-CSharp" && method == "UpdateServerInformation")
            {
                return false;
            }

            return true;
        }

        public static List<string> string_blacklist = new List<string>()
        {
            "Oxide.Core.OxideMod",
            "Oxide.Core"
        };

        public static bool IsPatchAllowed(TypeDefinition type)
        {
            var target = type.Methods.FirstOrDefault(x => x.CustomAttributes.Any(y =>
                y.Constructor.DeclaringType.Name == "HarmonyTargetMethods" &&
                y.Constructor.DeclaringType.Scope is SerializedAssemblyReference asmref &&
                asmref.Name == HarmonyASM));
            if (target?.MethodBody is not CilMethodBody body) goto End;

            for (int index = 0; index < body.Instructions.Count; index++)
            {
                var cil = body.Instructions[index];

                if (cil.OpCode == CilOpCodes.Ldstr && cil.Operand is string str)
                {
                    if (string_blacklist.Contains(str))
                    {
	                    return false;
                    }
                }
            }

            End:
            return true;
        }
    }
}

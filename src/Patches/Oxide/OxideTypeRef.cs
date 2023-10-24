using Carbon.Base;
using Carbon.Compat.Converters;
using Carbon.Compat.Lib;

namespace Carbon.Compat.Patches.Oxide;

public class OxideTypeRef : BaseOxidePatch
{
    public static List<string> PluginToBaseHookable = new List<string>()
    {
        "System.Void Oxide.Core.Libraries.Permission::RegisterPermission(System.String, Oxide.Core.Plugins.Plugin)",
        "System.Void Oxide.Core.Libraries.Lang::RegisterMessages(System.Collections.Generic.Dictionary`2<System.String, System.String>, Oxide.Core.Plugins.Plugin, System.String)",
        "System.String Oxide.Core.Libraries.Lang::GetMessage(System.String, Oxide.Core.Plugins.Plugin, System.String)",
        "System.Void Oxide.Game.Rust.Libraries.Command::RemoveConsoleCommand(System.String, Oxide.Core.Plugins.Plugin)",
    };

    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (MemberReference mref in asm.GetImportedMemberReferences())
        {
            AssemblyReference aref = mref.DeclaringType.DefinitionAssembly();
            if (mref.Signature is MethodSignature methodSig)
            {
                if (Helpers.IsOxideASM(aref))
                {
                    string fullName = mref.FullName;
                    if (PluginToBaseHookable.Contains(mref.FullName))
                    {
                        for (int index = 0; index < methodSig.ParameterTypes.Count; index++)
                        {
                            TypeSignature typeSig = methodSig.ParameterTypes[index];
                            if (typeSig.FullName == "Oxide.Core.Plugins.Plugin" &&
                                Helpers.IsOxideASM(typeSig.DefinitionAssembly()))
                            {
                                methodSig.ParameterTypes[index] = importer.ImportTypeSignature(typeof(BaseHookable));
                            }
                        }
                        continue;
                    }

                    if (methodSig.GenericParameterCount == 1 && fullName == "!!0 Oxide.Core.Interface::Call<?>(System.String, System.Object[])")
                    {
                        mref.Parent = importer.ImportType(typeof(OxideCompat));
                        mref.Name = nameof(OxideCompat.OxideCallHookGeneric);
                    }
                }
            }
        }

        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            ProcessTypeRef(tw, importer);
        }

        ProcessAttrList(asm.CustomAttributes);
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            ProcessAttrList(td.CustomAttributes);

            foreach (FieldDefinition field in td.Fields)
            {
                ProcessAttrList(field.CustomAttributes);
            }

            foreach (MethodDefinition method in td.Methods)
            {
                ProcessAttrList(method.CustomAttributes);
            }

            foreach (PropertyDefinition prop in td.Properties)
            {
                ProcessAttrList(prop.CustomAttributes);
            }
        }

        void ProcessAttrList(IList<CustomAttribute> list)
        {
            for (int x = 0; x < list.Count; x++)
            {
                CustomAttribute attr = list[x];
                for (int y = 0; y < attr.Signature?.FixedArguments.Count; y++)
                {
                    CustomAttributeArgument arg = attr.Signature.FixedArguments[y];
                    if (arg.Element is TypeDefOrRefSignature sig)
                    {
                        ProcessTypeRef(sig.Type as TypeReference, importer);
                    }
                }
            }
        }
    }

    public static void ProcessTypeRef(TypeReference tw, ReferenceImporter importer)
    {
	    if (tw == null)
	    {
		    return;
	    }

        if (tw.Scope is TypeReference parent)
        {
            if (parent.FullName is "Oxide.Plugins.Timers" or "Oxide.Plugins.Timer" && tw.Name == "TimerInstance")
            {
                tw.Name = "Timer";
                tw.Namespace = "Oxide.Plugins";
                tw.Scope = CompatManager.Common.ImportWith(importer);
                return;
            }
        }

        if (tw.Scope is AssemblyReference aref && Helpers.IsOxideASM(aref))
        {
            if (tw.FullName == "Oxide.Core.Event" || tw.FullName.StartsWith("Oxide.Core.Event`"))
            {
                tw.Scope = (IResolutionScope)importer.ImportType(typeof(OxideCompat));
                return;
            }

            if (tw.Namespace.StartsWith("Newtonsoft.Json"))
            {
                tw.Scope = CompatManager.Newtonsoft.ImportWith(importer);
                return;
            }

            if (tw.Namespace.StartsWith("ProtoBuf"))
            {
                if (tw.Namespace == "ProtoBuf" && tw.Name == "Serializer")
                    tw.Scope = CompatManager.protobuf.ImportWith(importer);
                else
                    tw.Scope = CompatManager.protobufCore.ImportWith(importer);
                return;
            }

            if (tw.Name == "VersionNumber")
            {
                goto sdk;
            }

            if (tw.Namespace == "Oxide.Plugins" && tw.Name.EndsWith("Attribute"))
            {
                tw.Namespace = string.Empty;
                goto sdk;
            }

            if (tw.FullName == "Oxide.Plugins.Hash`2")
            {
                tw.Namespace = string.Empty;
                goto common;
            }

            if (tw.FullName is "Oxide.Core.Libraries.Timer")
            {
                tw.Name = "Timers";
                tw.Namespace = "Oxide.Plugins";
                goto common;
            }

            if (tw.FullName == "Oxide.Core.Plugins.HookMethodAttribute")
            {
                tw.Namespace = string.Empty;
                goto sdk;
            }

            if (tw.FullName is "Oxide.Plugins.CSharpPlugin" or "Oxide.Core.Plugins.CSPlugin")
            {
                tw.Name = "RustPlugin";
                tw.Namespace = "Oxide.Plugins";
                goto common;
            }

            if (tw.FullName == "Oxide.Core.Plugins.PluginManager")
            {
                tw.Namespace = string.Empty;
            }

            common:
            tw.Scope = CompatManager.Common.ImportWith(importer);
            return;

            sdk:
            tw.Scope = CompatManager.SDK.ImportWith(importer);
        }
    }
}

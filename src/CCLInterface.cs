namespace Carbon.Compat;

/*public class CCLInterface : CarbonModule<CCLConfig, EmptyModuleData>
{
    public static CCLInterface Singleton { get; private set; }
    public override Type Type => typeof(CCLInterface);

    public override bool EnabledByDefault => true;

    public CCLInterface()
    {
        Singleton = this;
    }

    internal static void AttemptModuleInit()
    {
        try
        {
            InitModule();
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to init module: {e}");
        }
    }
    internal static void InitModule()
    {
        if (Singleton != null) return;
        Community.Runtime.ModuleProcessor.Build(typeof(CCLInterface));
    }
    public override string Name => "CCL";

}*/

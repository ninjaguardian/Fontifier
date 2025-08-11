using MelonLoader;

[assembly: MelonInfo(typeof(Fontifier.Fontifier), "Fontifier", "1.0.0", "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 222, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(0, 7, 0, true)]

namespace Fontifier
{
    /// <summary>
    /// Lets you change the font for other mods.
    /// </summary>
    public class Fontifier : MelonMod
    {

    }
}

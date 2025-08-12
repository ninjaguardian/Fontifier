using Fontifier;
using Il2CppTMPro;
using MelonLoader;
using RumbleModUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using System.Drawing.Text;

[assembly: MelonInfo(typeof(Fontifier.Fontifier), "Fontifier", FontifierModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 222, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(0, 7, 0, true)]

namespace Fontifier
{
    /// <summary>
    /// Contains mod version.
    /// </summary>
    public static class FontifierModInfo
    {
        /// <summary>
        /// Mod version.
        /// </summary>
        public const string ModVer = "1.0.0";
    }
    /// <summary>
    /// Lets you change the font for other mods.
    /// </summary>
    public class Fontifier : MelonMod
    {
        readonly Mod Mod = new();

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            UI.instance.UI_Initialized += OnUIInitialized;
        }

        private void OnUIInitialized()
        {
            Mod.ModName = "Fontifier";
            Mod.ModVersion = FontifierModInfo.ModVer;
            Mod.SetFolder("Fontifier");
            Mod.AddDescription("Description", "", "Lets you change the font for other mods.", new Tags { IsSummary = true });
            List<TMP_FontAsset> fonts = new();

            foreach (string fontPath in
                Directory.EnumerateFiles(@"UserData\Fontifier\fonts", "*.*", SearchOption.TopDirectoryOnly)
                .Where(
                    f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                try
                {
                    var loadedFont = TMP_FontAsset.CreateFontAsset(
                        fontPath,
                        0,
                        90,
                        5,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024
                    );
                    loadedFont.hideFlags = HideFlags.HideAndDontSave;

                    try
                    {
                        using var fontCollection = new PrivateFontCollection();
                        fontCollection.AddFontFile(fontPath);
                        if (fontCollection.Families.Length > 0)
                            loadedFont.name = fontCollection.Families[0].Name;
                    }
                    catch (Exception ex)
                    {
                        loadedFont.name = Path.GetFileNameWithoutExtension(fontPath);
                        MelonLogger.Warning($"Could not read internal font name for {fontPath}, using filename instead.", ex);
                    }

                    fonts.Add(loadedFont);
                    MelonLogger.Msg($"Loaded font: {loadedFont.name} from {fontPath}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to load font from {fontPath}", ex);
                }
            }
            Mod.AddDescription("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true });

            Mod.GetFromFile();
            UI.instance.AddMod(Mod);
            MelonLogger.Msg("Fontifier added to ModUI.");
        }
    }
}

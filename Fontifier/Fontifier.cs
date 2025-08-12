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
            Mod.AddDescription("Description", "Description", "Lets you change the font for other mods.", new Tags { IsSummary = true });
            List<TMP_FontAsset> fonts = new();
            foreach (string fontPath in Directory.GetFiles(@"UserData\Fontifier\fonts"))
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
                    fonts.Add(loadedFont);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to load font from {fontPath}", ex);
                }
            }
            Mod.AddDescription("Fonts list", "Fonts list", string.Join("\n", fonts.Select(f => f.name)), new Tags { IsSummary = true });

            Mod.GetFromFile();
            UI.instance.AddMod(Mod);
            MelonLogger.Msg("Fontifier added to ModUI.");
        }
    }
}

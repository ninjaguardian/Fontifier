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

// TODO: Make ModUI show the fonts

[assembly: MelonInfo(typeof(Fontifier.Fontifier), "Fontifier", FontifierModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 222, 230)]
[assembly: MelonAuthorColor(255, 0, 160, 230)]

[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: VerifyLoaderVersion(0, 7, 0, true)]

//[assembly: MelonPriority(5)]

[assembly: MelonOptionalDependencies("HealthDisplayWithFont")]

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
    /// Makes sure it's a valid font name.
    /// </summary>
    public class FontNameValidator : ValidationParameters
    {
        private readonly Fontifier fontifier;
        /// <summary>
        /// Takes in a Fontifier MelonMod.
        /// </summary>
        public FontNameValidator(Fontifier fontifier)
        {
            this.fontifier = fontifier;
        }

        /// <inheritdoc/>
        public override bool DoValidation(string Input)
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                return true; // default font
            }
            foreach (TMP_FontAsset font in fontifier.fonts)
            {
                if (font.name.Equals(Input, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Lets you change the font for other mods.
    /// </summary>
    public class Fontifier : MelonMod
    {
        readonly Mod Mod = new();
        /// <summary>
        /// All font names.
        /// </summary>
        public readonly List<TMP_FontAsset> fonts = new();

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

            HashSet<string> existingNames = new(StringComparer.OrdinalIgnoreCase);
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
                    TMP_FontAsset loadedFont = TMP_FontAsset.CreateFontAsset(
                        fontPath,
                        0,
                        90,
                        5,
                        GlyphRenderMode.SDFAA,
                        1024,
                        1024
                    );
                    loadedFont.hideFlags = HideFlags.HideAndDontSave;

                    string baseName;
                    try
                    {
                        using PrivateFontCollection fontCollection = new();
                        fontCollection.AddFontFile(fontPath);
                        if (fontCollection.Families.Length == 0 || string.IsNullOrWhiteSpace(fontCollection.Families[0].Name))
                            throw new Exception("Font has no internal family name.");
                        baseName = fontCollection.Families[0].Name;
                    }
                    catch (Exception ex)
                    {
                        baseName = Path.GetFileNameWithoutExtension(fontPath);
                        MelonLogger.Warning($"Could not read internal font name for {fontPath}, using filename instead.", ex);
                    }

                    if (string.IsNullOrWhiteSpace(baseName))
                    {
                        throw new Exception("Font has an invalid name/filename.");
                    }

                    string candidate = baseName;
                    int suffix = 0;
                    while (existingNames.Contains(candidate))
                    {
                        suffix++;
                        candidate = $"{baseName} ({suffix})";
                    }

                    loadedFont.name = candidate;
                    existingNames.Add(loadedFont.name);
                    fonts.Add(loadedFont);
                    MelonLogger.Msg($"Loaded font: {loadedFont.name} from {fontPath}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to load font from {fontPath}", ex);
                }
            }
            Mod.AddDescription("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true });

            FontNameValidator validator = new(this);
            foreach (MelonMod mod in RegisteredMelons)
            {
                if (mod.Info.Name.Equals("HealthDisplayWithFont", StringComparison.OrdinalIgnoreCase))
                {
                    MelonLogger.Msg("HealthDisplayWithFont was found.");
                    Mod.AddToList("HealthDisplayWithFont", "", "Enter a font from the Font List or leave it empty to use the default font.", new Tags());
                    Mod.AddValidation("HealthDisplayWithFont", validator);
                }
            }

            Mod.GetFromFile();
            UI.instance.AddMod(Mod);
            MelonLogger.Msg("Fontifier added to ModUI.");
        }
    }
}

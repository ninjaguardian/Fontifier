using Fontifier;
using Il2CppTMPro;
using MelonLoader;
using RumbleModUI;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// TODO: Make ModUI show the fonts

[assembly: MelonInfo(typeof(Fontifier.Fontifier), "Fontifier", FontifierModInfo.ModVer, "ninjaguardian", "https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

[assembly: MelonColor(255, 0, 160, 230)]
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
    static class FontifierModInfo
    {
        /// <summary>
        /// Mod version.
        /// </summary>
        public const string ModVer = "1.0.0";
    }

    /// <summary>
    /// Makes sure it's a valid font name.
    /// </summary>
    class FontNameValidator : ValidationParameters
    {
        /// <inheritdoc/>
        public override bool DoValidation(string Input)
        {
            Fontifier.Logger.Warning(Input);
            if (string.IsNullOrWhiteSpace(Input))
            {
                Fontifier.Logger.Warning("whitespace");
                return true; // default font
            }
            foreach (TMP_FontAsset font in Fontifier.fonts)
            {
                if (font.name.Equals(Input, StringComparison.OrdinalIgnoreCase))
                {
                    Fontifier.Logger.Warning(font.name);
                    return true;
                }
            }
            Fontifier.Logger.Warning("none");
            return false;
        }
    }

    /// <summary>
    /// Lets you change the font for other mods.
    /// </summary>
    class Fontifier : MelonMod
    {
        /// <summary>
        /// The logger.
        /// </summary>
        public static readonly MelonLogger.Instance Logger = new("Fontifier", System.Drawing.Color.FromArgb(255, 0, 160, 230));
        private readonly static Mod Mod = new();
        /// <summary>
        /// All font names.
        /// </summary>
        public readonly static List<TMP_FontAsset> fonts = new();

        /// <inheritdoc/>
        public override void OnLateInitializeMelon()
        {
            UI.instance.UI_Initialized += OnUIInitialized;
        }

        private static void OnUIInitialized()
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
                        Logger.Warning($"Could not read internal font name for {fontPath}, using filename instead.", ex);
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
                    Logger.Msg($"Loaded font: {loadedFont.name} from {fontPath}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load font from {fontPath}", ex);
                }
            }
            Mod.AddDescription("Fonts List", "", "The following fonts are loaded:\n" + string.Join("\n", fonts.Select(f => f.name)), new Tags { IsEmpty = true });

            FontNameValidator validator = new();
            foreach (MelonMod mod in RegisteredMelons)
            {
                if (mod.Info.Name.Equals("HealthDisplayWithFont", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Msg("HealthDisplayWithFont was found.");
                    Mod.AddToList("HealthDisplayWithFont", "", "Enter a font from the Font List or leave it empty to use the default font.", new Tags()).SavedValueChanged += HealthDisplayWithFontChanged;
                    Mod.AddValidation("HealthDisplayWithFont", validator);
                }
            }

            Mod.GetFromFile();
            UI.instance.AddMod(Mod);
            Logger.Msg("Fontifier added to ModUI.");
        }

        private static void HealthDisplayWithFontChanged(object sender, EventArgs args)
        {
            MelonMod healthMod = RegisteredMelons.FirstOrDefault(m => m.Info.Name == "HealthDisplayWithFont");
            if (healthMod == null)
            {
                Logger.Warning("HealthDisplayWithFont mod not found.");
                return;
            }

            Type modType = healthMod.GetType();

            FieldInfo fontAssetField = modType.GetField("fontAsset", BindingFlags.Static | BindingFlags.NonPublic);
            if (fontAssetField != null)
            {
                foreach (TMP_FontAsset font in fonts)
                {
                    if (font.name.Equals(((ValueChange<string>)args).Value, StringComparison.OrdinalIgnoreCase))
                    {
                        fontAssetField.SetValue(null, font);
                        return;
                    }
                }
            }
            else
            {
                Logger.Warning("Field 'fontAsset' not found.");
            }
        }
    }
}

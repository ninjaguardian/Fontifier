# Fontifier
![Photo](https://raw.githubusercontent.com/ninjaguardian/Fontifier/master/Fontifier.png)

[![Github](https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3.2.0/assets/cozy/available/github_vector.svg)](https://github.com/ninjaguardian/Fontifier)
[![Changelog](https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3.2.0/assets/cozy/documentation/changelog_vector.svg)](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier/changelog)
[![Thunderstore](https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3.2.0/assets/cozy/documentation/website_vector.svg)](https://thunderstore.io/c/rumble/p/ninjaguardian/Fontifier)

## What is this?
This mod lets you change the font of other mods' text. It also allows people to use this in their own mods (see 'For Devs').

## Instructions
1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader)
2. Run RUMBLE without mods
3. Drop Mods from .zip into RUMBLE's installation folder
4. Drop UserData from .zip into RUMBLE's installation folder
5. Drop UserLibs from .zip into RUMBLE's installation folder
6. Install dependencies (If you want custom fonts)
    - [RumbleModUI](https://thunderstore.io/c/rumble/p/Baumritter/RumbleModUI)
    - [RumbleModUIPlus](https://thunderstore.io/c/rumble/p/ninjaguardian/RumbleModUIPlus)
7. Play RUMBLE!

## Supported mods
| [HealthDisplayWithFont](https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont) | [TournamentScoringMod](https://thunderstore.io/c/rumble/p/davisgreenwell/TournamentScoringMod) |
|--|--|
| [![Photo](https://raw.githubusercontent.com/ninjaguardian/HealthDisplayWithFont/master/icon.png)](https://thunderstore.io/c/rumble/p/ninjaguardian/HealthDisplayWithFont) | [![Photo](https://gcdn.thunderstore.io/live/repository/icons/davisgreenwell-TournamentScoringMod-1.0.1.png)](https://thunderstore.io/c/rumble/p/davisgreenwell/TournamentScoringMod) |

## Choose a font
1. Move any .ttf or .otf fonts into UserData\Fontifier\fonts
2. Press F10 to open Mod UI
3. Go to Fontifier in the dropdown
4. In the second dropdown, select 'Fonts List'
5. Find the name of the font you want
6. In the second dropdown, select any mod
7. Type in the name of the font you want (case-insensitive) and hit enter
8. If it looks good, hit save

## "I can't type in ModUI"
If you have UnityExplorer, hit F7 first. If not, ask the discord.

## "It's not saving"
Make sure to press enter and then hit save. If you are, ask the discord.

## For Devs
<details>
<summary>For Devs</summary>

If you create a TextMeshPro (or similar) in your mod and want to use Fontifier with it, here's how to do it.

First, choose if you want Fontifier to be a required dependency or optional dependency.

<details>
<summary>Required</summary>

- <details><summary>You will need the following usings:</summary>

    ```c#
    using Il2CppTMPro;
    using MelonLoader;
    using static Fontifier.Fontifier;
    // The following two are needed if ImplicitUsings are disabled
    using System;
    ```
  </details>

- <details><summary>And these dll refrences:</summary>

    - net6
        - MelonLoader.dll
    - Il2CppAssemblies
        - Unity.TextMeshPro.dll
    - Mods
        - Fontifier.dll

  </details>

- <details><summary>And this code:</summary>
    (Place it in your MelonMod class)

    ```c#
    #region Fontifier
    private static Func<TMP_FontAsset> GetFont;

    /// <inheritdoc/>
    public override void OnInitializeMelon()
    {
        GetFont = RegisterModWithReference(this.Info.Name, new EventHandler<EventArgs>(FontChanged));
    }

    private static void FontChanged(object sender, EventArgs args)
    {
        // Change your TextMeshPro.font to the new font.
        TextMeshProInstance.font = FontFromName(((dynamic)args).Value);
    }
    #endregion
    ```

    ALSO: When you create the TextMeshPro, make sure to `TextMeshProInstance.font = GetFont();`
  </details>

</details>

<details>
<summary>Optional</summary>

- <details><summary>You will need the following usings:</summary>

    ```c#
    using Il2CppTMPro;
    using MelonLoader;
    using System.Reflection;
    // The following two are needed if ImplicitUsings is disabled
    using System;
    using System.Linq;
    ```
  </details>

- <details><summary>And these dll refrences:</summary>

    - net6
        - MelonLoader.dll
    - Il2CppAssemblies
        - Unity.TextMeshPro.dll

  </details>

- <details><summary>And this code:</summary>
    (Place it in your MelonMod class)

    ```c#
    #region Fontifier
    private static Func<TMP_FontAsset> GetFont;
    private static Func<string, TMP_FontAsset> FontFromName;

    /// <inheritdoc/>
    public override void OnInitializeMelon()
    {
        if (RegisteredMelons.FirstOrDefault(m => m.Info.Name == "Fontifier")?.GetType() is Type fontifierType && fontifierType != null) (GetFont, FontFromName) = ((Func<TMP_FontAsset>, Func<string, TMP_FontAsset>))fontifierType.GetMethod("RegisterMod", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { this.Info.Name, new EventHandler<EventArgs>(FontChanged) });
    }

    private static void FontChanged(object sender, EventArgs args)
    {
        // Change your TextMeshPro.font to the new font.
        TextMeshProInstance.font = FontFromName(((dynamic)args).Value);
    }
    #endregion
    ```

    ALSO: When you create the TextMeshPro, make sure to `TextMeshProInstance.font = GetFont();`
  </details>
</details>
</details>

## Help And Other Resources
Get help and find other resources in the [Modding Discord](https://discord.gg/fsbcnZgzfa)

## Included font credit
- [SDRAWKCABMIAY's video](https://youtu.be/wp4VaVm_XpI)
- [File link](https://drive.google.com/drive/folders/1-Wr4TW4FVQ8j8EyKAMHPa-D2Srg05Fyk)

## License

**My code:** [CC0 1.0 Universal](https://github.com/ninjaguardian/Fontifier?tab=CC0-1.0-1-ov-file) (public domain)

[![CC0-1.0 License](https://img.shields.io/badge/License-CC0_1.0_Universal-green.svg)](https://github.com/ninjaguardian/Fontifier?tab=CC0-1.0-1-ov-file)

**Includes SixLabors.Fonts.dll** (© Six Labors, [Apache License 2.0](https://github.com/ninjaguardian/Fontifier?tab=Apache-2.0-2-ov-file))

[![Apache License 2.0](https://img.shields.io/badge/License-Apache_License_2.0-green.svg)](https://github.com/ninjaguardian/Fontifier?tab=Apache-2.0-2-ov-file)

# Archived: Inno Setup installer bits

These files were the previous `.exe`-based install path, used for winget releases **0.0.1.0** and **0.0.1.1**.

They are kept for reference only. They are **not** built by any workflow and **do not** produce a usable CmdPal extension.

## Why archived

PowerToys Command Palette discovers extensions exclusively via `AppExtensionCatalog.Open("com.microsoft.commandpalette")`, which only enumerates packages with AppX/MSIX identity. An Inno-installed `.exe` that only writes `HKCU\Software\Classes\CLSID\{...}\LocalServer32` is invisible to CmdPal — the installer succeeds, nothing shows up in the palette.

See:

- https://github.com/microsoft/PowerToys/issues/38273 (open: "Support Unpackaged Extensions")
- https://github.com/microsoft/PowerToys/issues/47076 (open: "The documentation for the cmdpal plugin seems to have issues")

The extension was migrated to single-project MSIX, signed by the Microsoft Store, starting in version **0.0.2.0**. See `.github/workflows/release-msix.yml` in the repo root for the current build.

## Bringing these back

If PowerToys ever adds a registry-based discovery path for unpackaged extensions, these files can be restored by moving them back to `NBAExtension/` (the `.iss` and `.ps1` files) and `.github/workflows/` (the YAML).

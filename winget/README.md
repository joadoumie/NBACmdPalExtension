# winget manifests (working copies)

These are tracked in-repo so the Store product ID and future bumps live next
to the code. The actual source of truth for published manifests is
[microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) under
`manifests/j/joadoumie/NBACommandPaletteExtension/<version>/`.

## Publishing a new version

1. Bump `Version` in `NBAExtension/Package.appxmanifest` and trigger
   `.github/workflows/release-msix.yml`.
2. Submit the resulting `.msixbundle` in Partner Center. Wait for the
   listing to update (certification usually takes a few hours).
3. Copy the previous version's folder under `winget/` to a new
   `winget/<new-version>/`, update the three YAML files, and (for the
   first MSIX-based release) fill in `MSStoreProductIdentifier` from the
   live Store listing URL.
4. Run `winget validate --manifest winget/<new-version>`.
5. Open a PR to microsoft/winget-pkgs with the three YAMLs under the
   matching `manifests/j/joadoumie/NBACommandPaletteExtension/<new-version>/`
   path. The easiest way is `wingetcreate submit` — it clones the fork and
   opens the PR for you.

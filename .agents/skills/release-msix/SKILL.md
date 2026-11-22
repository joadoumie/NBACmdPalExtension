---
name: release-msix
description: >-
  Cut a Microsoft Store MSIX release, create the GitHub Release, or locally
  sideload-test the unsigned CI bundle for the NBA Command Palette Extension.
  Use when bumping the version, triggering release-msix.yml, staging release
  artifacts, or testing an unsigned .msix locally.
---

# Microsoft Store MSIX Release Process

Build mechanics live in `.github/workflows/release-msix.yml`. Follow this runbook for the operational steps and non-obvious constraints.

## 1. Bump the Version

- Version lives **only** in `NBAExtension/Package.appxmanifest` under `<Identity Version="W.X.Y.Z" />` (4-part version, e.g. `0.0.6.0`).
- Double-check that `ToolGood.Words.Pinyin` is still referenced in `NBAExtension/NBAExtension.csproj` (required unlisted runtime dependency of `FuzzyStringMatcher`; if dropped, fuzzy search breaks in the shipped bundle).
- Update `README.md` if adding a "New in `<ver>`" note or updating the roadmap.

## 2. Branch and Open a PR

- **Direct push to `main` is blocked by policy.**
- Create a `release/<ver>` branch, commit the version bump (and any release prep), push, and open a PR for the maintainer to merge.

## 3. Trigger the CI MSIX Bundle Build

Run the workflow against the release branch (or `main` after merge):

```powershell
gh workflow run release-msix.yml --ref release/<ver> -f version=<ver>
```

- Produces an unsigned x64 + ARM64 `.msixbundle` artifact (typically ~4–5 minutes).
- The Microsoft Store re-signs the package on upload; do not sign the bundle you upload to Partner Center.

## 4. Stage Artifacts Locally

- Download the workflow artifact and write `release-notes.md` into `release-staging/<ver>/`.
- `release-staging/` is gitignored—**never commit** `.msix`/`.msixbundle` files (30 MB+ binaries).
- Upload `release-staging/<ver>/*.msixbundle` to Microsoft Partner Center under the new Store submission.
- **Ignore `winget/`**: winget is no longer part of the release process (Microsoft Store submission only, even though the `release-msix.yml` summary output still mentions updating `winget/manifests`).

## 5. Optional: Local Sideload Test of the Unsigned Bundle

Windows Developer Mode does **not** allow installing unsigned MSIX packages (fails with `0x800B010A`). To test the exact CI build locally before submitting to Partner Center:

1. Copy the x64 `.msix` from `release-staging/<ver>/` to a scratch subfolder (e.g., `release-staging/<ver>/local-test/`) so you never mutate the Store bundle.
2. Create a throwaway self-signed code-signing certificate whose Subject matches `Package.appxmanifest`'s `Publisher` **exactly**:
   ```text
   CN=2B17359D-8771-44C7-AF07-42CE3B87B52C
   ```
3. Sign the copied x64 `.msix` with that certificate, then **immediately delete the private-key certificate from `Cert:\CurrentUser\My`**.
4. Export and import the public `.cer` into `Cert:\LocalMachine\TrustedPeople` (requires elevation via `Start-Process pwsh -Verb RunAs`).
5. Install the signed copy with `Add-AppxPackage`.
6. Once verified (and/or once the Store copy is live), uninstall the test build and remove the throwaway cert from `Cert:\LocalMachine\TrustedPeople`.

## 6. Create the GitHub Release (Manual)

The CI workflow does **not** create a GitHub Release. After the PR is merged to `main`, create a notes-only release pointing to the merge commit:

```powershell
gh release create NBAExtension-v<ver> `
  --target <main-merge-sha> `
  --title "NBA Command Palette Extension v<ver> — <short hook>" `
  --notes-file release-staging/<ver>/release-notes.md `
  --latest
```

- **Do not attach the unsigned `.msixbundle`** to the GitHub Release—it is not directly installable and confuses users.

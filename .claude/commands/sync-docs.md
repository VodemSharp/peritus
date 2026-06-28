---
description: Regenerate the living docs inventory and audit the fundamental docs for drift. Run manually after an approved change.
argument-hint: "[optional: area to focus, e.g. endpoints | docs]"
allowed-tools: Bash(git status:*), Bash(git diff:*), Read, Grep, Glob, Edit, Write
---

You are running the **manual** documentation sync for Peritus. This is **never** automatic — the user
invokes it deliberately, *after* a change has been approved. Be idempotent: on a clean tree with no
drift, change nothing and report "no drift".

The docs split into two tiers (see CLAUDE.md → "Documentation System"):

- **Living** (`docs/reference/**`) — mechanically derived from code. You **regenerate** these.
- **Fundamental** (`CLAUDE.md`, `docs/fundamental/*.md`) — hand-curated principles. You **audit and report** drift;
  you do **not** silently rewrite a principle. Only fix unambiguous factual breakage (a dead path, a
  renamed type, a broken markdown link) and call out anything judgmental for the human.

Focus area (optional): `$ARGUMENTS`. If empty, do the full sync below.

## Steps

1. **See what changed.** Run `git status` and `git diff` to understand the scope of recent edits
   (new/renamed features, contracts, tests, routes). Use this to target the audit; do not rely on it as
   the source of truth — always confirm against the files themselves.

2. **Regenerate `docs/reference/endpoints.md`** from the source of truth, preserving its
   `AUTO-GENERATED` banner and header. Derive every row fresh; do not trust the old file's contents:
   - **Routes + contract DTOs:** `src/contracts/Peritus.ApiContracts.Identity/IIdentityApi.cs` (one
     Refit method per endpoint; the request/response types are the contract column).
   - **Tag + HTTP method + exact route:** each feature's static `MapEndpoint` under
     `src/modules/Peritus.Identity/Features/**/*Feature.cs` (the literal `.WithTags(...)` value; `—` if
     none is declared).
   - **Registration sanity check:** every feature must appear once in
     `IdentityExtensions.MapIdentityEndpoints()`. Flag any feature with a `MapEndpoint` that is not
     registered, or registered but missing.
   - **Test file:** the matching `{Feature}Tests.cs` under
     `tests/modules/Peritus.Identity.IntegrationTests/Scenarios/{Domain}/`. Flag any feature with no
     test file (the 1↔1 slice rule is broken).
   - Group rows by tag/domain (Auth, Accounts, Profile), update the per-group and total counts. If —
     and **only if** — you find drift (an endpoint with no `.WithTags`, or a route whose
     folder/tag/path disagree, e.g. a `Features/Accounts/` feature tagged `Auth`), append a short
     **Drift note** blockquote at the bottom listing each. When there is no drift, **omit the note
     entirely** — a "drift note: none" just restates the table. Surface real drift for a human; do not
     "fix" it in code.

3. **Audit the fundamental docs for drift** (`CLAUDE.md` and `docs/fundamental/{architecture,patterns,persistence,api,refit,testing}.md`). For each, verify against the code:
   - Every file path, type name, and method name mentioned still exists (Grep/Glob to confirm).
   - Every markdown link resolves (back-link `[← Back to CLAUDE.md]`, the `**Related:**` line, and
     cross-doc links point at real files).
   - The "File Locations Reference" rows in CLAUDE.md point at paths that exist.
   - **Fix only unambiguous breakage** (dead path, renamed symbol, broken link). For anything that
     changes the *meaning* of a convention, **report it** and let the human decide.

4. **Refresh CLAUDE.md's hub wiring** only if docs were added or removed: update the Detailed Guides
   table and any cross-links so the table still lists every `docs/fundamental/*.md` and the living reference. Do not
   restructure prose.

5. **Verify before finishing:**
   - `docs/reference/endpoints.md` row count equals the number of methods on `IIdentityApi`.
   - Every feature/test/contract path you wrote exists on disk.
   - `grep -rn "Endpoints/" CLAUDE.md docs/` surfaces no reference to the removed
     `src/apps/Peritus.Api/Endpoints/` path.

## Report

End with a concise summary:
- **Regenerated:** what changed in the living inventory (rows added/removed/retagged), or "identical".
- **Fixed:** any unambiguous factual corrections you made in fundamental docs.
- **Needs human decision:** drift you found but did not auto-fix (tag mismatches, missing tests,
  convention changes), each with the file and a one-line reason.

If nothing changed and nothing drifted, say so plainly.

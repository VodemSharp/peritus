# Writing documentation for AI (project-specific cues)

This repo includes an AI entrypoint to help agents navigate code and docs. Follow these practices when writing/maintaining docs for AI consumption.

## AI reference entrypoint
- `AGENTS.md` links to all focused docs: overview, modular architecture, strong types, endpoints/auth, shared libs, contracts, identity module, host/Aspire, service defaults, testing, and how-to guides.
  @/AGENTS.md#1-22

## What helps AI perform better
- Keep docs short, structured, and deeply linked (as done in `/docs/*`).
- Provide “where things live” pointers (projects, directories) and the call flow. Example: overview describes API → modules → results mapping and strong type flow.@/docs/overview.md#3-32
- Include practical recipes and gotchas (e.g., strong types and EF mapping).@/docs/strong-types.md#16-42
- Expose cross-cutting setup (service registration, auth, persistence, OpenAPI, logging) so the agent can jump to the right extension methods.@/docs/service-registration.md

## Writing new docs for AI
1) Start from AGENTS.md and add a link to any new topic so the AI can find it.
2) Use precise file paths and project names; include short code snippets that show the pattern (e.g., how to register a module or configure EF converters).
3) Call out defaults and extension points (e.g., interceptors, converters, middleware ordering).
4) Keep terminology consistent with code (strong IDs, FluentResult, Aspire AppHost).

## Maintenance checklist
- When adding a module or feature, add/update the relevant doc under `docs/` and link it from `AGENTS.md`.
- Prefer minimal, focused docs over long narratives—agents search by headings and links.

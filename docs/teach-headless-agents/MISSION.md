# Mission: Headless Backend Agent Loop (BarberSalon)

## Why
Amir wants to finish the .NET backend for the barbershop app using an autonomous loop — not by hand-coding every layer, but by understanding how the orchestrator script, `.agents/` folder, and a headless CLI (Cline / OpenCode / Antigravity) work together so he can run one use case end-to-end with confidence.

## Success looks like
- Can explain in plain language: Select → Enrich → Inject → Verify → Close
- Can run `-DryRun` and read what the loop would do next
- Can complete **one manual iteration** for `GetActiveSalonServices` using **one** CLI of his choice
- Knows where to wire Inject in `run-backend-cycle.ps1` for full automation later

## Constraints
- Windows + PowerShell; repo at `E:\barber\beauty-saloon-api`
- Three CLIs already installed: `cline`, `opencode`, `agy`
- Inject phase in script is still a **stub** — learning starts with manual steps, then wiring
- Persian explanations OK; commands and paths stay in English

## Out of scope
- Building all 17 use cases in this learning arc
- Choosing the “best” LLM provider (use whatever is already authenticated)
- Frontend replacement (storyconnector) — backend loop only

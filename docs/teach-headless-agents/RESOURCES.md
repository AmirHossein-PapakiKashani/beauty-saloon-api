# Headless Agent Loop — Resources

## Knowledge

- [Cline CLI Overview — Headless mode](https://docs.cline.bot/usage/cli-overview)
  Official: when headless activates, `-y` / `--auto-approve`, `--json`, piping. Use for: Cline inject wiring.
- [Cline CLI reference (GitHub)](https://github.com/cline/cline/blob/main/docs/cline-cli/cli-reference.mdx)
  Flags: `-y`, `-c`, `--timeout`, `-p` plan mode. Use for: exact command syntax.
- Project: `beauty-saloon-api/run-backend-cycle.ps1`
  The orchestrator script Amir already has. Use for: what each phase does today.
- Project: `beauty-saloon-api/.agents/skills/backend-cycle/SKILL.md`
  Full loop specification. Use for: agent-facing procedure.
- Project: `beauty-saloon-api/docs/architecture/agents-loop-architecture-v2.bmpr`
  Visual diagram (Balsamiq). Use for: big picture.

## Wisdom (Communities)

- [Cline Discord / docs support](https://docs.cline.bot/)
  Use for: CLI auth issues, headless flags changing between versions.
- User preference: learning via agent (Cursor) first; communities optional later.

## Gaps

- No single doc that unifies OpenCode + AGY + Cline inject into `run-backend-cycle.ps1` — this teach workspace fills that gap.

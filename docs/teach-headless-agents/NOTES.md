# Teaching notes

- Amir prefers Persian explanations; keep CLI paths in English.
- All three CLIs installed: cline, opencode, agy.
- Inject in run-backend-cycle.ps1 still stub — teach manual path first.
- Primary source for Cline: https://docs.cline.bot/usage/cli-overview
- Learner trap: `$p = Get-Content ...` is silent (assignment). They thought Inject "didn't answer."
- Cline CLI today: no `-y`; auto-approve is default. Prefer `cline -c <path> "$p"` or pipe. Timeout flag is `-t`, not `--timeout`.
- Side effect: running `run-backend-cycle.ps1` with stub Inject + green placeholder tests marked all 17 use cases `completed` falsely — reset queue before real work.
- Inject wired 2026-09-01: `.\run-backend-cycle.ps1 -Agent cline -Once` runs full Select→Enrich→Inject→Verify→Close. Close requires green tests + handoff report. Re-injects on red.
- Agy gotcha: `--print` consumes the next argv as prompt. Must put `--print` last (after `--dangerously-skip-permissions`). Fixed 2026-09-01; queue reset GetActiveSalonServices → pending.
- Agy live feed: tails `~/.gemini/antigravity-cli/brain/.../transcript.jsonl` for THINKING/ACTION lines (ported from front run-with-agy-fallback.ps1). Heartbeat every 25s if quiet.
- Cline gotcha: never splat args (`& cline @args`) — cline.ps1 falls back to interactive → TTY error. Use `& cline -c $ws -t $sec $prompt` direct invoke.
- Agy crash fix: no async DataReceived ScriptBlocks (PS host crash). Live feed = transcript.jsonl tail on main thread only.

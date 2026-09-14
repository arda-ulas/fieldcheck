# AI assistance log

One entry per story or block. Four fields each: what was asked, what the agent produced, what was
changed or rejected (and why), how it was verified. Tool: Claude Code (Claude Fable 5.1), directed
and reviewed by Arda Ulaş Özdemir. Honest entries only.

## Block 0 — Environment setup (2026-09-14)
- **Asked:** install commands for .NET 10 SDK, Docker Desktop, Azure CLI, SQL Server and Azurite containers on Apple Silicon; create the repo.
- **Produced:** the brew/docker commands, `gh repo create`, secret scanning + push protection enabled via the GitHub API, .NET `.gitignore`.
- **Changed/rejected:** Docker Desktop hung after the Rosetta setting change (engine reported a read-only filesystem); the agent force-restarted it and recreated both containers. The Azurite arm64 image failed with `exec format error` on this engine, so Azurite runs as `--platform linux/amd64` under emulation instead of the spec's plain `docker run`.
- **Verified:** `dotnet --list-sdks` shows 10.0.401; `docker ps` shows both containers Up; SQL Server log shows "Recovery is complete"; ports 1433 and 10000 accept connections; `gh repo view` confirms public.

## Block 1 — AGENTS.md, stories, skeleton, CI (2026-09-14)
- **Asked:** rules file, stories with Gherkin-style criteria mapped to test names, solution skeleton, GitHub Actions CI.
- **Produced:** `AGENTS.md`, `docs/stories.md` (every criterion mapped to a named test), `HANDOFF.md`, `.github/workflows/ci.yml`, `FieldCheck.slnx` with `src/FieldCheck.Api` (controllers template, sample weather code removed), `tests/FieldCheck.UnitTests`, `tests/FieldCheck.IntegrationTests` (one Testcontainers test that starts real SQL Server and runs `SELECT 1`), `Directory.Build.props` with nullable + warnings-as-errors.
- **Changed/rejected:** The agent first wrote `new MsSqlBuilder()`, which Testcontainers 4.15 marks obsolete (fails the build under warnings-as-errors). Replaced with the explicit-image constructor per the package's own message.
- **Verified:** `dotnet test` locally: 1 unit + 1 integration passed (integration test started a real SQL Server container in ~11 s). CI run on the PR: see PR #1.

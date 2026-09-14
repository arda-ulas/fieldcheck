# Rules for AI-assisted work in this repo

These rules apply to any coding agent (Claude Code, Copilot, etc.) working here and to the human
directing it.

1. **One story per branch.** Branch names: `story/s1-register-assets`, `chore/ci`, etc. `main`
   changes only through pull requests with CI green.
2. **Plan before code.** The agent proposes a short plan at each checkpoint; the human approves it
   before implementation continues.
3. **No secrets anywhere.** No passwords, connection strings, keys, or SAS tokens in prompts, code,
   commits, or chat. Local: `dotnet user-secrets`. Azure: App Service configuration. CI: GitHub
   secrets or ephemeral Testcontainers credentials.
4. **Every generated diff is read before commit.** The human reviews in git, not in the chat.
5. **Tests must pass before a PR merges.** Unit and integration tests run in CI against real SQL
   Server.
6. **Verify package APIs against documentation; don't guess.** Especially OData, Testcontainers,
   OpenAPI/Scalar, and Azure SDKs on .NET 10.
7. **Hand-written T-SQL.** The stored procedure, view, and indexes are written and explained by a
   person, not generated and pasted.
8. **Honest AI log.** `docs/ai-log.md` records, per story, what was asked, what was produced, what
   was changed or rejected, and how it was verified. "Accepted as-is after review" is a valid entry.
9. **No overstatement.** The README describes a one-day portfolio project. No claims of users,
   production readiness, or scale.

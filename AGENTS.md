# Working with coding agents in this repo

Rules for any coding agent (Claude Code, Copilot, Codex, etc.) and for the person directing it.

1. **One story per branch.** Branch names like `story/s1-register-assets` or `chore/ci`. `main`
   changes only through pull requests with CI green.
2. **Plan before code.** The agent proposes a short plan; the person approves it before
   implementation continues.
3. **No secrets anywhere.** No passwords, connection strings, keys, or SAS tokens in prompts, code,
   commits, or chat. Local: `dotnet user-secrets`. Azure: App Service configuration. CI: GitHub
   secrets or ephemeral Testcontainers credentials.
4. **Every generated diff is read before commit.** Review happens in git, not in the chat.
5. **Tests must pass before a PR merges.** Unit and integration tests run in CI against a real SQL
   Server.
6. **Verify package APIs against documentation; don't guess.** Especially OData, Testcontainers,
   OpenAPI/Scalar, and the Azure SDKs on .NET 10.
7. **T-SQL is written and explained by a person.** The stored procedure, view, and indexes are not
   generated and pasted.
8. **Keep the review log honest.** `docs/ai-log.md` records, per story, what was asked, what was
   produced, what was changed or rejected, and how it was verified. "Accepted as-is after review"
   is a valid entry; invented corrections are not.
9. **No overstatement in docs.** Describe what the code does and what it does not do. No claims of
   users, production readiness, or scale.

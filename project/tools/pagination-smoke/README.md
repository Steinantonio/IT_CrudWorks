# Pagination smoke

Reproducible 10,000-row measurement for ADR-0006 (keyset task pagination).

From the repository root:

```powershell
dotnet run --project project/tools/pagination-smoke
```

The tool creates a temporary SQLite database, applies the real migrations, seeds
one user with 10,000 tasks, then measures the first and cursor pages through
`TaskRepository.ListPageAsync`. It prints the p95 for both pages and the SQLite
query plan.

Reference run (2026-11-25):

```text
rows=10000 first_p95_ms=2.00 cursor_p95_ms=4.33
plan=SEARCH Tasks USING INDEX IX_Tasks_UserId_CreatedAtUtc_Id (UserId=? AND CreatedAtUtc<?)
```

The target is p95 ≤ 100 ms on local SQLite (ADR-0006 §5). Timings vary with the
machine; the command is the evidence, not the exact number.

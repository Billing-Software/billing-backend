# Database migrations

Apply SQL files in lexical order through the release pipeline, using a least-privileged deployment principal. `004_IndustrialHardening.sql` (checksum `v2`) is idempotent and records its application in `dbo.__SchemaMigrations`.

The hardened constraints use the same names as the Entity Framework model (`FK_*_Tenant`, `UQ_*`, `CK_*`, `IX_*`), so databases created fresh via `EnsureCreated` and databases upgraded via `004` converge on the same schema. Verify with:

```
dotnet run --project BillingBackend.Migrator -- --print-ddl
```

and diff the output against the target database (constraint/index names should match; only data and migration-history rows should differ).

Do not run the `BillingBackend.Migrator` reset workflow (`--reset-database`, development-only) against production. Back up first; if validation in the hardening migration fails (`THROW 51000–51010`), correct the identified data before retrying. Schedule `dbo.PurgeCompletedWebhookPayloads` after the approved retention period (minimum 30 days).

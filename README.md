# rrc-production-lookup

A searchable view of Texas oil and gas production data for mineral and royalty owners, built from the Railroad Commission of Texas (RRC) public data sets, which are distributed in EBCDIC mainframe format.

## Problem

Royalty owners verify their payments against the production reported for their lease or well. The RRC publishes this data free of charge, but key production ledgers are EBCDIC files with fixed record layouts, and conversion is left to the user. Most owners cannot use the raw files.

## What it does

- Downloads the RRC production ledger files each month.
- Converts EBCDIC records into a relational database using the record layouts in the RRC user manuals.
- Provides search by lease, well, operator, or county, with monthly oil and gas volumes over time.
- Optional: an owner enters their decimal interest and reported volumes from a royalty statement, and the app flags months where reported production and payment are inconsistent.

## Data sources

- [RRC Data Sets Available for Download](https://www.rrc.texas.gov/resource-center/research/data-sets-available-for-download/) — oil and gas ledgers by district, refreshed monthly, each with a PDF manual.
- [Oil and Gas Historical Ledger user's guide](https://www.rrc.texas.gov/media/ocwhbdp2/oilandgashistoricalledger_lad001.pdf)

## Engineering problems

- **Mainframe record conversion:** EBCDIC decoding, fixed-layout records, and numeric field formats defined in the RRC layouts.
- **Schema and indexing:** decades of monthly production per lease, indexed for interactive lookup.
- **Incremental loads:** monthly refreshes that apply only new and corrected records.
- **ETL robustness:** download, validate, load, and reconcile each month; malformed records are logged and quarantined, not silently dropped.

## Correctness checks

- Golden-file tests: sample records with known decoded values.
- Reconciliation: totals for selected leases match the RRC's online production query exactly, run as an automated test.

## Planned stack

- .NET 10: EBCDIC parsing library, ingestion job, ASP.NET Core web app
- SQL Server / Azure SQL
- EF Core for application data access
- Azure Functions or WebJobs for the monthly load; Azure App Service for hosting
- xUnit and Moq
- GitHub Actions CI

## Repository layout

- `src/Rrc.Ebcdic` — EBCDIC text (code page 037), packed decimal (COMP-3), and zoned decimal decoding. No dependencies.
- `src/Rrc.Production.Data` — EF Core model and `ProductionDbContext` for SQL Server.
- `src/Rrc.Production.Ingestion` — run-once host that loads ledger files into the database.
- `src/Rrc.Production.Web` — ASP.NET Core Razor Pages lookup site.
- `tests/Rrc.Ebcdic.Tests` — decoder tests.
- `tests/Rrc.Production.Ingestion.Tests` — ingestion tests.

## Getting started

Requires the .NET 10 SDK and SQL Server (LocalDB by default; see `ConnectionStrings:Production` in each `appsettings.json`).

```
dotnet build
dotnet test
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/Rrc.Production.Data --startup-project src/Rrc.Production.Web
dotnet ef database update --project src/Rrc.Production.Data --startup-project src/Rrc.Production.Web
dotnet run --project src/Rrc.Production.Ingestion
dotnet run --project src/Rrc.Production.Web
```

The ingestion host reads files from `LedgerImport:InputDirectory` (default `ledgers`, relative to the working directory).

## Milestones

- [ ] Parse one district ledger file per its PDF layout, with golden-file tests
- [ ] Database schema and initial bulk load
- [ ] Lookup UI by lease, well, operator, county
- [ ] Scheduled monthly download and incremental load on Azure
- [ ] Automated reconciliation against the RRC online query
- [ ] Royalty statement consistency check

# rrc-production-lookup

A searchable view of Texas oil and gas production data for mineral and royalty owners, built from the Railroad Commission of Texas (RRC) public data sets, which are distributed in EBCDIC mainframe format.

## Problem

Royalty owners verify their payments against the production reported for their lease or well. The RRC publishes this data free of charge, but key production ledgers are EBCDIC files with fixed record layouts, and conversion is left to the user. Most owners cannot use the raw files.

## What it does

- Converts the RRC Historical Ledger EBCDIC records into a relational database using the record layouts in the RRC user manual.
- Planned: downloads the ledger files each month and applies only new and corrected records.
- Planned: search by lease, well, operator, or county, with monthly oil and gas volumes over time.
- Planned: an owner enters their decimal interest and reported volumes from a royalty statement, and the app flags months where reported production and payment are inconsistent.

## Status

- The statewide oil and gas Historical Ledger files (November 2026 publication) parse with no rejected records and load completely into SQL Server.
- The database holds 159,737 oil leases (29,926,594 lease months) and 220,152 gas wells (43,473,864 well months), January 1993 through October 2024.
- Lease names, operator names, and counties are not in the ledger and are not loaded yet; leases carry RRC district, lease number or gas well ID, and a per-month operator number.
- The web app is still the project template; no lookup pages exist yet.

## Data sources

- [RRC Data Sets Available for Download](https://www.rrc.texas.gov/resource-center/research/data-sets-available-for-download/) — lists the ledger data sets, each with a PDF manual.
- [Oil and Gas Historical Ledger user's guide](https://www.rrc.texas.gov/media/ocwhbdp2/oilandgashistoricalledger_lad001.pdf) (LAD001) — record layouts for dataset `T.LDF900`.
- Historical Ledger – Statewide Oil (`S.LDF900OL.D.ebc.gz`) and Historical Ledger – Statewide Gas (`S.LDF900GS.D.ebc.gz`), refreshed monthly (available by the 27th) through the RRC's GoAnywhere file portal, linked from the data sets page. Coverage starts January 1993 and ends about 25 months before the publication date.

### Observed file characteristics

Measured on the November 2026 publication:

| File | Compressed | Records | Leases / wells | Months |
|---|---|---|---|---|
| `S.LDF900OL.D.ebc.gz` (oil) | 466 MB | 61,274,989 | 159,737 | 29,926,594 |
| `S.LDF900GS.D.ebc.gz` (gas) | 921 MB | 107,310,084 | 220,152 | 43,473,864 |

- Records are 160 bytes with no delimiters and no tape labels.
- Each file ends with an undocumented trailer record: record id `01` followed by 30 EBCDIC zeros. The parser returns it as `LedgerTrailer`.
- Within each lease, cycle records are in descending month order (ascending cycle key).
- Neither file has malformed records or duplicate months within a lease.
- District 8B does not appear in either file; district 6E appears only in the oil file, as the manual states.
- Gas well numbers are left-padded with spaces; the parser trims them.

## Engineering problems

- **Mainframe record conversion:** EBCDIC decoding, fixed-layout records, and numeric field formats defined in the RRC layouts.
- **Schema and indexing:** decades of monthly production per lease, indexed for interactive lookup.
- **Incremental loads:** monthly refreshes that apply only new and corrected records.
- **ETL robustness:** download, validate, load, and reconcile each month; malformed records are logged and quarantined, not silently dropped.

## Correctness checks

- Golden-file tests: real records from each statewide file, with expected values decoded independently of the parser (Python `cp037` codec and manual field positions).
- Full-file loads: database row counts match a parse-only scan of each file, and the golden-file leases match their database rows.
- Planned: reconciliation of selected leases against the RRC's online production query, run as an automated test.

## Stack

- .NET 10: EBCDIC parsing library, ingestion host, ASP.NET Core web app
- SQL Server (LocalDB for development; Azure SQL planned)
- EF Core for data access and migrations
- xUnit and Moq; SQLite in-memory for importer tests
- GitHub Actions CI
- Planned: Azure Functions or WebJobs for the monthly load; Azure App Service for hosting

## Repository layout

- `src/Rrc.Ebcdic` — EBCDIC text (code page 037), packed decimal (COMP-3), and zoned decimal decoding. No dependencies.
- `src/Rrc.Ledger` — Historical Ledger record parser: 160-byte records, segment types 01–08, cycle-key and district-code mapping per the LAD001 user's guide. `LedgerFileReader` returns malformed records with their raw bytes; `LedgerLeaseGrouper` groups records into leases and rejects records that cannot be placed under the correct parent.
- `src/Rrc.Production.Data` — EF Core model, `ProductionDbContext`, and migrations.
- `src/Rrc.Production.Ingestion` — run-once host that loads ledger files into the database. Each root record becomes a `Lease` (oil lease, or gas well keyed by RRC ID) and each cycle record a `LeaseProductionMonth`; reloading a lease updates existing months and adds new ones.
- `src/Rrc.Production.Web` — ASP.NET Core Razor Pages site (template only so far).
- `tests/Rrc.Ebcdic.Tests` — decoder tests.
- `tests/Rrc.Ledger.Tests` — record layout and grouping tests, plus golden files `Fixtures/oil-lease-sample.ebc` (oil lease 01-000737, ten records) and `Fixtures/gas-well-sample.ebc` (gas well 02-127175, fourteen records).
- `tests/Rrc.Production.Ingestion.Tests` — importer, quarantine, and host tests.

## Getting started

Requires the .NET 10 SDK and SQL Server. The default connection string (`ConnectionStrings:Production` in each `appsettings.json`) targets the LocalDB instance `MSSQLLocalDB`, which is installed with the LocalDB option of SQL Server Express.

```
dotnet build
dotnet test
dotnet tool restore
dotnet ef database update --project src/Rrc.Production.Data --startup-project src/Rrc.Production.Web
```

### Loading ledger files

1. Download `S.LDF900OL.D.ebc.gz` and `S.LDF900GS.D.ebc.gz` from the Historical Ledger – Statewide Oil and Statewide Gas links on the RRC data sets page, and place them in `ledgers/` (ignored by git). The files can stay compressed.
2. Run the ingestion host from its build output directory so it finds its `appsettings.json`:

```
dotnet build -c Release src/Rrc.Production.Ingestion
cd src/Rrc.Production.Ingestion/bin/Release/net10.0
dotnet Rrc.Production.Ingestion.dll --LedgerImport:InputDirectory=<repo>/ledgers --Logging:LogLevel:Microsoft.EntityFrameworkCore=Warning
```

Settings under `LedgerImport`:

- `InputDirectory` (default `ledgers`) and `FilePattern` (default `*`) select the files; files are processed in name order, and `.gz` files are decompressed while reading.
- `BatchSize` (default 500) is the number of leases saved per database round.
- `QuarantineDirectory` (default `quarantine`) receives `<file>.rejected` (raw 160-byte records) and `<file>.rejected.log` (record number and reason) for malformed, orphaned, or duplicate-month records. No files are created when nothing is rejected.

Measured on LocalDB: the full oil file loads in 31 minutes and the full gas file in 40 minutes (about 16,000–18,000 month rows per second). A parse-only pass takes about one minute per file.

## Milestones

- [x] Parse the Historical Ledger per its PDF layout, with golden-file tests from real statewide files
- [x] Database schema and initial bulk load
- [ ] Lease names, operator names, and counties from the related RRC data sets
- [ ] Lookup UI by lease, well, operator, county
- [ ] Set-based bulk load for faster monthly refreshes
- [ ] Scheduled monthly download and incremental load on Azure
- [ ] Automated reconciliation against the RRC online query
- [ ] Royalty statement consistency check

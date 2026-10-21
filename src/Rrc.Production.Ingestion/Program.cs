using Rrc.Production.Data;
using Rrc.Production.Ingestion;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<LedgerImportOptions>(builder.Configuration.GetSection(LedgerImportOptions.SectionName));
builder.Services.AddProductionDatabase(builder.Configuration);
builder.Services.AddHostedService<LedgerImportWorker>();

var host = builder.Build();
host.Run();

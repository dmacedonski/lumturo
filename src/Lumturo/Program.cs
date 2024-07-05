using System.Globalization;

using Lumturo;
using Lumturo.Docker.Registry;
using Lumturo.Scanner;

CultureInfo cultureInfo = new("en-US");
Thread.CurrentThread.CurrentCulture = cultureInfo;
Thread.CurrentThread.CurrentUICulture = cultureInfo;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<LumturoConfig>(builder.Configuration.GetSection("Lumturo"));
builder.Services.AddSingleton<IScannerProvider, ScannerProvider>();
builder.Services.AddSingleton<IDockerRegistryProvider, DockerRegistryProvider>();
builder.Services.AddHostedService<ScannerWorker>();

var host = builder.Build();
host.Run();

using System.Globalization;

using Lumturo;
using Lumturo.Docker.Registry;

CultureInfo cultureInfo = new("en-US");
Thread.CurrentThread.CurrentCulture = cultureInfo;
Thread.CurrentThread.CurrentUICulture = cultureInfo;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<LumturoConfig>(builder.Configuration.GetSection("Lumturo"));
builder.Services.AddSingleton<IDockerRegistryProvider, DockerRegistryProvider>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

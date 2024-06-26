using Lumturo;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<LumturoConfig>(builder.Configuration.GetSection("Lumturo"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

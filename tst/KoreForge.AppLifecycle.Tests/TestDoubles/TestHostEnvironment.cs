using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace KoreForge.AppLifecycle.Tests.TestDoubles;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string ApplicationName { get; set; } = "TestApp";
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

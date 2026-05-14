using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tool.Service;

// UseWindowsService() 使程序既能作为 Windows Service 运行（sc start），
// 也能直接双击 exe 在控制台调试，两种模式自动切换。
IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        // 服务在"服务"管理器中显示的名称。
        options.ServiceName = "ProcessLimitService";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<ProcessLimitWorker>();
    })
    .Build();

await host.RunAsync();

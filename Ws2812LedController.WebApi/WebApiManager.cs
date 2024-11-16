using System.Reflection;
using System.Text.Json.Serialization;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.WebApi.Converters;

namespace Ws2812LedController.WebApi;

public class WebApiManager
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _task;
    private readonly Ref<LedManager> _manager;
    
    public WebApiManager(Ref<LedManager> manager)
    {
        _manager = manager;
        _task = Task.Run(Service, _cancellationTokenSource.Token);
    }
    
    public async Task Terminate(int timeout = 5000)
    {
        _cancellationTokenSource.Cancel();
        await _task.WaitAsync(TimeSpan.FromMilliseconds(timeout));
    }

    private async void Service()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:5000");

        // Add services to the container.
        var assembly = Assembly.Load($"Ws2812LedController.WebApi");
        builder.Services.AddMvc().AddApplicationPart(assembly).AddControllersAsServices();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new ColorConverter());
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton(_ => _manager);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        //if (_app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        await app.RunAsync(_cancellationTokenSource.Token);
    }
}
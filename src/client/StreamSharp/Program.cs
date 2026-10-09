using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StreamSharp;
using MudBlazor.Services;
using StreamSharp.Services.Abstract;
using StreamSharp.Services.Concrete;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<IRenderAwakenerService, RenderAwakenerService>();
builder.Services.AddMudServices();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient("StreamSharpApi", (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    string apiBaseUrl = configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

    client.BaseAddress = new Uri(apiBaseUrl);
});
await builder.Build().RunAsync();

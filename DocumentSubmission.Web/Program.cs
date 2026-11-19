using DocumentSubmission.Web;
using DocumentSubmission.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API serves this app under /ui, so the API itself is at the root of the same origin.
var apiBaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), "/");
builder.Services.AddSingleton(new HttpClient { BaseAddress = apiBaseAddress });
builder.Services.AddSingleton<UserSession>();
builder.Services.AddSingleton<ApiClient>();

await builder.Build().RunAsync();

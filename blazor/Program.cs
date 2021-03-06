﻿using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using GameLogic;
using gmtk2020_blazor.Helpers;

namespace gmtk2020_blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<MyApp>("myApp");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddTransient<BlazorTimer>();

            builder.Services.AddTransient<ScenarioPackageDownloader>();


            await builder.Build().RunAsync();
        }
    }
}

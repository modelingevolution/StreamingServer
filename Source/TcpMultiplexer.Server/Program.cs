using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;
using TcpMultiplexer.Server.Data;
using TcpMultiplexer.Server.Pages;
using ILoggerFactory = TcpMultiplexer.Server.Data.ILoggerFactory;


namespace TcpMultiplexer.Server
{
    public class Program
    {
        
        public static async Task Main(string[] args)
        {
            if (args.ContainsAnyOfArgs("--help", "/?", "-h"))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine();
                Console.WriteLine("TcpMultiplexer.Server [--listen <listen-address>] [--port <listen-port>] --autostart");
                Console.WriteLine();
                Console.WriteLine("listen-address - bind address such as 0.0.0.0, default is 127.0.0.1");
                Console.WriteLine("listen-port    - listing port, default is 2301");
                Console.WriteLine("autostart      - flag that would load configuration and start listening when app startups up.");
                Console.WriteLine();
                return;
            }

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<VideoMultiplexerServer>((sp) => new VideoMultiplexerServer( args.GetStringArg("--listen") ?? "localhost",args.GetIntArg("--port") ?? 2301, sp.GetRequiredService<ILoggerFactory>()));
            builder.Services.AddScoped<ServerVm>();
            builder.Services.AddMudServices();
            

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
           

            if (args.ContainsAnyOfArgs("--autostart"))
            {
                var srv = app.Services.GetRequiredService<VideoMultiplexerServer>();
                await srv.LoadConfig();
                srv.Start();
            }
           

            await app.RunAsync();
        }
    }
}
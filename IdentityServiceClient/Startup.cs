using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using IdentityServiceClient.Middleware.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace IdentityServiceClient
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            #region Setup Serilog for Logging

            // We use the global, statically accessible logger from Serilog.
            // It must be set up in the main entrpoint and does not require a DI container

            // Create a logger with configured sinks, enrichers, and minimum level
            // Serilog's global, statically accessible logger, is set via Log.Logger and can be invoked using the static methods on the Log class.

            // File Sink is commented out and can be replaced with Serilogs vast library of available sinks

            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code) //<-- This will give us output to our Kestrel console
            //.WriteTo.File("_logs/log-.txt", rollingInterval: RollingInterval.Day) //<-- Write our logs to a local text file with rolling interval configuration
            .CreateLogger();

            Log.Information("The global/static Serilog logger has been configured.");

            #endregion

            #region Initialize our IApplicationSettings object

            IApplicationSettings applicationSettings = new ApplicationSettings(Configuration);

            #endregion

            #region Register our dependencies with the DI

            // Configuration
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IApplicationSettings>(applicationSettings);

            #endregion 

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            // Use our authentication middleware
            app.UseAuthenticationMiddleware();

            app.UseMvc();
        }
    }
}

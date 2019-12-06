﻿using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DynamicBundle
{
    public class Startup
    {
        readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<DynamicSourceInvalidator>();

            services.AddBundling()
                .UseDefaults(_env)
                .UseWebMarkupMin()
                .AddLess();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, DynamicSourceInvalidator invalidator)
        {
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseBundling(
                // as we rebased the static files to /static (see UseStaticFiles()),
                // we need to pass this information to the bundling middleware as well,
                // otherwise urls would be rewrited incorrectly
                new BundlingOptions
                {
                    StaticFilesRequestPath = "/static"
                },
                bundles =>
                {
                    bundles.AddCss("/site.css")
                        .Include("/css/*.css");

                    // first we create the dynamic bundle content source (with dependencies resolved from the IoC container)
                    var dynamicSource = ActivatorUtilities.CreateInstance<DynamicSource>(app.ApplicationServices, bundles.Bundles.SourceFileProvider);

                    // then we add a less bundle whose input is generated dynamically based on the query string
                    bundles.AddLess("/dynamic.css")
                        .AddDynamicSource(dynamicSource.ProvideItems, invalidator.CreateChangeToken)
                        .DependsOnParams()
                        .UseCacheOptions(new BundleCacheOptions { SlidingExpiration = TimeSpan.FromSeconds(30) });
                });

            app.UseStaticFiles(new StaticFileOptions { RequestPath = "/static" });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

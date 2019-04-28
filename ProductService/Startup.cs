﻿using System;
using Common.Consul;
using Consul;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Common.RabbitMQ;
using Common.Dispatcher;
using Common.Jaeger;
using App.Metrics.Registry;
//using ProductService.Metrics;
using System.Linq;
using Common.Metrics;
using Common.Web;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductService
{
    public class Startup
    {
        public IContainer Container { get; private set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddTransient<IMetricRegistry, MetricRegistry>();
            services.AddConsul();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
              //  .AddControllersAsServices();
            services.AddJaeger();
            services.AddOpenTracing();

            
            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly())
                .AsImplementedInterfaces();
            //builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            //   .AsImplementedInterfaces();
           
          

            //property injection in controller
            //var controllersTypesInAssembly = typeof(Startup).Assembly.GetExportedTypes()
            //.Where(type => typeof(ActionFilterAttribute).IsAssignableFrom(type)).ToArray();
            //builder.RegisterTypes(controllersTypesInAssembly).PropertiesAutowired();
            //builder.RegisterType<AppMetricCountAttribute>().PropertiesAutowired();
    //        builder.Register(x => new MetricRegistry())
    //.As<IMetricRegistry>()
    //.InstancePerHttpRequest();
           

            builder.AddRabbitMq();
            builder.AddDispatcher();
            Container = builder.Build();

            return new AutofacServiceProvider(Container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            IApplicationLifetime applicationLifetime,  IConsulClient client)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRabbitMq();
           

            app.UseMvc(
                routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(
                    name: "api",
                    template: "api/{controller}/{action=Index}/{id?}");
            }
            );
            var consulServiceId = app.UseConsul();
            applicationLifetime.ApplicationStopped.Register(() =>
            {
                client.Agent.ServiceDeregister(consulServiceId);
               // Container.Dispose();
            }); ;
        }
    }
}
using System.Threading.Tasks;
using BurgerJoint.Events.AzureEventHubs;
using BurgerJoint.Events.Kafka;
using BurgerJoint.StoreFront.Data;
using BurgerJoint.StoreFront.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BurgerJoint.StoreFront
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration) => _configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().WithRazorPagesRoot("/Features");

            services.AddDbContext<BurgerDbContext>(
                    options => options.UseSqlServer(_configuration.GetConnectionString("BurgerJointStoreFront")))
                .AddHostedService<OutboxHostedService>()
                .AddSingleton<OutboxMessagePublisher>()
                .AddSingleton<OutboxMessageListener>()
                .AddSingleton<IOutboxMessageListener>(s => s.GetRequiredService<OutboxMessageListener>())
                .AddHostedService<OutboxHostedService>();


            if (_configuration.GetValue<bool>("UseAzure"))
            {
                services.AddAzureEventHubsPublisher(
                    _configuration.GetSection(nameof(EventHubsOrderEventPublisherSettings))
                        .Get<EventHubsOrderEventPublisherSettings>());
            }
            else
            {
                services.AddKafkaEventPublisher();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapFallback(ctx =>
                {
                    ctx.Response.Redirect("/Orders/Create");
                    return Task.CompletedTask;
                });
            });
        }
    }
}
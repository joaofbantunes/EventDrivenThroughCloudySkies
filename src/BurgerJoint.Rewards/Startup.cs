using System.Linq;
using System.Threading.Tasks;
using BurgerJoint.Events.AzureEventHubs;
using BurgerJoint.Events.Kafka;
using BurgerJoint.Rewards.Data;
using BurgerJoint.Rewards.Domain;
using BurgerJoint.Rewards.Features.Dashboard;
using BurgerJoint.Rewards.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BurgerJoint.Rewards
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().WithRazorPagesRoot("/Features");
            services.AddSignalR();

            services
                .AddDbContext<RewardsDbContext>(
                    options => options.UseSqlServer(_configuration.GetConnectionString("BurgerJointRewards")))
                .AddHostedService<EventListenerHostedService>()
                .AddScoped<IOrderEventHandler, RewardsProgramEventHandler>()
                .Decorate<IOrderEventHandler, OrderEventHandlerIdempotenceDecorator>();

            if (_configuration.GetValue<bool>("UseAzure"))
            {
                services.AddAzureEventHubsConsumer(
                    _configuration
                        .GetSection(nameof(EventHubsOrderEventConsumerSettings))
                        .Get<EventHubsOrderEventConsumerSettings>());
            }
            else
            {
                services.AddKafkaEventConsumer(new KafkaOrderEventConsumerSettings
                {
                    ConsumerGroup = "rewards"
                });
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
                endpoints.MapHub<DashboardHub>("/dashboard/hub");
                endpoints.MapRazorPages();
                endpoints.MapFallback(ctx =>
                {
                    ctx.Response.Redirect("/dashboard");
                    return Task.CompletedTask;
                });
            });
        }
    }
}
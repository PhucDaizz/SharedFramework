using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Configuration;
using SharedLibrary.Interfaces;
using SharedLibrary.Services;

namespace SharedLibrary.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedRabbitMQ(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));

            services.AddSingleton<IMessagePublisher>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RabbitMQSettings>>();
                var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQPublisher>>();
                return RabbitMQPublisher.CreateAsync(options, logger).GetAwaiter().GetResult();
            });

            services.AddSingleton<IMessageConsumer>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<RabbitMQSettings>>();
                var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQConsumer>>(); 
                return RabbitMQConsumer.CreateAsync(options, logger).GetAwaiter().GetResult();
            });

            return services;
        }
    }
}

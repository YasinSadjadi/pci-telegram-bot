using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application.Conversations;

namespace VocabularyBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, long? bootstrapAdminId = null)
    {
        services.AddSingleton(new BotOptions(bootstrapAdminId));
        services.AddScoped<BotService>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
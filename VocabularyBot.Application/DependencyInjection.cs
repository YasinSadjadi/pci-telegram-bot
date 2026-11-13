using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application.Conversations;

namespace VocabularyBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, long? bootstrapAdminId = null, string? bootstrapAdminUsername = null)
    {
        services.AddSingleton(new BotOptions(bootstrapAdminId, bootstrapAdminUsername));
        services.AddScoped<BotService>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Application.Conversations;
using VocabularyBot.Application.Reminders;

namespace VocabularyBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, long bootstrapAdminId)
    {
        services.AddSingleton(new BotOptions(bootstrapAdminId));
        services.AddScoped<BotService>();
        services.AddScoped<ReminderService>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
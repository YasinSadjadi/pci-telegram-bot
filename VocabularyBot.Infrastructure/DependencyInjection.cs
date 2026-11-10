using Microsoft.Extensions.DependencyInjection;
using VocabularyBot.Infrastructure.Persistence.Repositories;

namespace VocabularyBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository, GenericRepository>();
        return services;
    }
}
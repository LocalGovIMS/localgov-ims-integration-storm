using Microsoft.Extensions.DependencyInjection;
using Application.Data;
using Infrastructure.Data;
using Application.Clients.CybersourceRestApiClient.Interfaces;
using Infrastructure.Clients;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddTransient<ICybersourceRestApiClient, CybersourceRestApiClient>();
            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

            return services;
        }
    }
}

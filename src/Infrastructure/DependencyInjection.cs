using Application.Clients.LocalGovImsPaymentApi;
using Infrastructure.Clients.LocalGovImsPaymentApi;
using Microsoft.Extensions.DependencyInjection;
using Application.Data;
using Infrastructure.Data;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddTransient<ILocalGovImsPaymentApiClient, LocalGovImsPaymentApiClient>();
            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));

            return services;
        }
    }
}

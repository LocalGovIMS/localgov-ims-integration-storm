using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Entities;

namespace Application.Clients.CybersourceRestApiClient.Interfaces
{
    public interface ICybersourceRestApiClient
    {
        Task<List<Payment>> SearchPayments(string clientReference, int daysAgo);

    }
}
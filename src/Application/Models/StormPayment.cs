using System.Collections.Generic;
using System.Linq;

namespace Application.Models
{
    public class StormPayment
    {
        public decimal Amount { get; set; }
        public string InternalReference { get; set; }
        public bool Success { get; set; } = false;

    }
}

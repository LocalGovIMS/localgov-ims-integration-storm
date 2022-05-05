using System.Collections.Generic;
using System.Linq;

namespace Application.Models
{
    public class Payment
    {
        public decimal Amount { get; set; }
        public string InternalReference { get; set; }
    }
}

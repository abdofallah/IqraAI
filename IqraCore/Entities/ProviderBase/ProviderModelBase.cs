using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IqraCore.Entities.ProviderBase
{
    public class ProviderModelBase
    {
        public string Id { get; set; } = "";
        public DateTime? DisabledAt { get; set; } = null;
    }
}

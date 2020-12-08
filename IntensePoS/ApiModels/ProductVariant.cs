using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class ProductVariant
    {
        public List<string> colors { get; set; } = null;
        public List<string> sizes { get; set; } = null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstantDealingApp
{
    public class Item
    {
        public string? GoldRate { get; set; }
        public string? labAmount { get; set; }
        public string? labToCash { get; set; }
        public string? cashAmount { get; set; }
        public string? cashToLab { get; set; }
        public bool? fix {  get; set; }
    }
}

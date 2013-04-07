using System.Collections.Generic;

namespace RestBox.ViewModels
{
    public class IntellisenseItem
    {
        public IntellisenseItem()
        {
            Values = new List<string>();
        }
        public string Key { get; set; }
        public List<string> Values { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace RestBox.Domain.Entities
{
    public class MainMenuItem
    {
        public Action ExecuteMethod { get; set; }
        public Func<bool> CanExecuteMethod { get; set; }
        public string Header { get; set; }
        public bool IsChecked { get; set; }
        public List<MainMenuItem> Children { get; private set; }

        public MainMenuItem()
        {
            Children = new List<MainMenuItem>();
        }
    }
}

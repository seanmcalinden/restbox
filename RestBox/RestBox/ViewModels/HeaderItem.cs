namespace RestBox.ViewModels
{
    public class HeaderItem
    {
        public string Key { get; set; }
        public string[] Values { get; set; }
        public bool IsContentHeader
        {
            get { return Key.ToLower().Contains("content"); }
        }
    }
}

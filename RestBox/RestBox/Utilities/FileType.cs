namespace RestBox.Utilities
{
    public class FileType
    {
        public string Extension { get; set; }
        public string FilterText { get; set; }
        public string CreateTitle { get; set; }
        public string OpenTitle { get; set; }
        public string AddExistingTitle { get; set; }
        public string SaveAsTitle { get; set; }
        public string UntitledFileName { get; set; }

        public FileType(string extension, string filterText, string createTitle, string openTitle, string addExistingTitle, string saveAsTitle)
        {
            Extension = extension;
            FilterText = filterText;
            CreateTitle = createTitle;
            OpenTitle = openTitle;
            AddExistingTitle = addExistingTitle;
            SaveAsTitle = saveAsTitle;
            UntitledFileName = "Untitled";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace RestBox.ViewModels
{
    public class RestBoxState
    {
        public List<RestBoxStateFile> RestBoxStateFiles { get; set; }

        public RestBoxState()
        {
            RestBoxStateFiles = new List<RestBoxStateFile>();
        }
    }

    public class RestBoxStateFile
    {
        public RestBoxStateFileType FileType { get; set; }
        public string FilePath { get; set; }
        public string DateSaved { get; set; }
        public string Name {get { return Path.GetFileNameWithoutExtension(FilePath); }}

        public RestBoxStateFile(RestBoxStateFileType fileType, string filePath)
        {
            FileType = fileType;
            FilePath = filePath;
            DateSaved = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }
    }

    public enum RestBoxStateFileType
    {
        Solution,
        HttpRequest
    }
}

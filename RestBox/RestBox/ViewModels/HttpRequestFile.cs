using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class HttpRequestFile : ViewModelBase<HttpRequestFile>
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { id = value; OnPropertyChanged(x => x.Id); }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(x => x.Name); }
        }

        private string relativeFilePath;
        public string RelativeFilePath
        {
            get { return relativeFilePath; }
            set { relativeFilePath = value; OnPropertyChanged(x => x.RelativeFilePath); }
        }

        private string groups;
        public string Groups
        {
            get { return groups; }
            set { groups = value; OnPropertyChanged(x => x.Groups); }
        }
    }
}

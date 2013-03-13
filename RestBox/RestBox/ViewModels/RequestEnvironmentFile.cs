using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class RequestEnvironmentFile : ViewModelBase<RequestEnvironmentFile>
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
    }
}

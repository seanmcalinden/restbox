using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class RequestExtensionViewFile : ViewModelBase<RequestExtensionViewFile>
    {
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

        private string icon;
        public string Icon
        {
            get { return icon; }
            set { icon = value; OnPropertyChanged("Icon"); }
        }
    }
}

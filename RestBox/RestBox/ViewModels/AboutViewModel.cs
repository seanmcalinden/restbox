using System.Globalization;
using System.Reflection;
using RestBox.Domain.Entities;

namespace RestBox.ViewModels
{
    public class AboutViewModel : ViewModelBase<AboutViewModel>
    {
        public AboutViewModel()
        {
            var assembly = Assembly.GetEntryAssembly();
            Copyright = "\u00a9 2013 CoderSphere";
            Version = assembly.GetName().Version.ToString();
        }

        public string Copyright { get; set; }
        public string Version { get; set; }
    }
}

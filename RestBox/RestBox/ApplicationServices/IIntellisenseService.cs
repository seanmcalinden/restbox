using System.Collections.Generic;

namespace RestBox.ApplicationServices
{
    public interface IIntellisenseService
    {
        List<string> GetSuggestions(string currentText);
        void AddEnvironmentIntellisenseItem(string key, string value);
        void AddRequestExtensionIntellisenseItem(string key);
        void RemoveRequestExtensionIntellisenseItem(string key);
        List<string> GetEnvironmentIntellisenseItems(string currentText);
        List<string> GetRequestExtesnionsIntellisenseItems(string currentText);
        bool CheckKey(string tagPrefix, string key);
    }
}
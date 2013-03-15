using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class IntellisenseService : IIntellisenseService
    {
        private readonly List<IntellisenseItem> intellisenseItems;
        private readonly List<IntellisenseItem> environmentIntellisenseItems;
        private readonly List<IntellisenseItem> requestExtensionsIntellisenseItems;

        public IntellisenseService()
        {
            var intellisenseJson = File.ReadAllText("Intellisense.json");
            intellisenseItems = JsonConvert.DeserializeObject<List<IntellisenseItem>>(intellisenseJson);
            environmentIntellisenseItems = new List<IntellisenseItem>();
            requestExtensionsIntellisenseItems = new List<IntellisenseItem>();
        }

        public void AddEnvironmentIntellisenseItem(string key, string value)
        {
            if (environmentIntellisenseItems.Any(x => x.Key == string.Format("env.{0}", key)))
            {
                return;
            }

            var environmentIntellisenseItem = new IntellisenseItem
                                       {
                                           Key = string.Format("env.{0}", key)
                                       };

            environmentIntellisenseItem.Values.Add(value);
            environmentIntellisenseItems.Add(environmentIntellisenseItem);
        }

        public void AddRequestExtensionIntellisenseItem(string key)
        {
            if (requestExtensionsIntellisenseItems.Any(x => x.Key == string.Format("ext.{0}", key)))
            {
                return;
            }

            var intellisenseItem = new IntellisenseItem
            {
                Key = string.Format("ext.{0}", key)
            };

            requestExtensionsIntellisenseItems.Add(intellisenseItem);
        }

        public void RemoveRequestExtensionIntellisenseItem(string key)
        {
            var intellisenseItem = requestExtensionsIntellisenseItems.FirstOrDefault(x => x.Key.ToLower() == string.Format("ext.{0}", key).ToLower());
            if(intellisenseItem != null)
            {
                requestExtensionsIntellisenseItems.Remove(intellisenseItem);
            }
        }

        public List<string> GetEnvironmentIntellisenseItems(string currentText)
        {
            if (string.IsNullOrWhiteSpace(currentText))
            {
                return new List<string>();
            }

            if (currentText.Contains("env."))
            {
                var currentTextParts = currentText.Trim().Split(' ');
                return environmentIntellisenseItems.Where(
                x => x.Key.ToLower().Contains(currentTextParts[0].ToLower()))
                .Select(x => x.Key.Replace("env.", string.Empty)).ToList();
            }
            return new List<string>();
        }

        public List<string> GetRequestExtesnionsIntellisenseItems(string currentText)
        {
            if (string.IsNullOrWhiteSpace(currentText))
            {
                return new List<string>();
            }

            if (currentText.Contains("ext."))
            {
                var currentTextParts = currentText.Trim().Split(' ');
                return requestExtensionsIntellisenseItems.Where(
                x => x.Key.ToLower().Contains(currentTextParts[0].ToLower()))
                .Select(x => x.Key.Replace("ext.", string.Empty)).ToList();
            }
            return new List<string>();
        }

        public List<string> GetSuggestions(string currentText)
        {
            if (string.IsNullOrWhiteSpace(currentText))
            {
                return new List<string>();
            }

            if (currentText.Contains(":"))
            {
                var splitHeader = currentText.Split(':');
                var key = splitHeader[0].ToLower();
                var value = splitHeader[1];
                var intellisenseHeaderItem = intellisenseItems.FirstOrDefault(x => x.Key.ToLower() == key);

                var values = value.Split(',').Select(x => x.Trim().ToLower());

                if (intellisenseHeaderItem != null)
                {
                    var filteredValue = values.Where(x => !intellisenseHeaderItem.Values.Contains(x)).FirstOrDefault();
                    if (filteredValue == null)
                    {
                        return new List<string>();
                    }
                    var filteredIntellisenseValuesForNoRepeat = intellisenseHeaderItem.Values.Where(x => !values.Contains(x)).ToList();
                    return filteredIntellisenseValuesForNoRepeat.Where(
                    x => x.ToLower().StartsWith(filteredValue.Trim().ToLower()) && x.ToLower() != filteredValue.Trim().ToLower())
                    .ToList();
                }
                return new List<string>();
            }
            return intellisenseItems.Where(
                x => x.Key.ToLower().StartsWith(currentText.Trim().ToLower()) && x.Key.Trim().ToLower() != currentText.ToLower())
                .Select(x => x.Key).ToList();
        }

        public bool CheckKey(string tagPrefix, string key)
        {
            if (tagPrefix == "env.")
            {
                return environmentIntellisenseItems.Any(x => x.Key.ToLower() == "env." + key.ToLower());
            }
            else
            {
                return requestExtensionsIntellisenseItems.Any(x => x.Key.ToLower() == "ext." + key.ToLower());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace RestBox.ApplicationServices
{
    public static class HttpRequestExtensionCacheService
    {
        public static List<CacheItem> cacheItems = new List<CacheItem>();

        public static void AddCacheItem(string key, string args, int duration, string value)
        {
            cacheItems.Add(new CacheItem
                               {
                                   Key = key,
                                   Args = args,
                                   Value = value,
                                   Expires = DateTime.UtcNow.AddMilliseconds(duration)
                               });
        }

        public static string GetCacheValue(string key, string args)
        {
            var cacheItem = cacheItems.FirstOrDefault(x => x.Key == key && x.Args == args);
            
            if(cacheItem != null)
            {
                if (DateTime.UtcNow > cacheItem.Expires)
                {
                    cacheItems.Remove(cacheItem);
                    return null;
                }
                return cacheItem.Value;
            }
            return null;
        }
    }

    public class CacheItem
    {
        public string Key { get; set; }
        public string Args { get; set; }
        public string Value { get; set; }
        public DateTime Expires { get; set; }
    }
}

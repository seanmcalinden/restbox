﻿using System.IO;
using AvalonDock;
using AvalonDock.Layout.Serialization;

namespace RestBox.Services
{
    public class ApplicationLayout : IApplicationLayout
    {
        private const string LayoutFileName = @".\RestBox.Layout.config";

        public void Load(DockingManager dockingManager)
        {
            var serializer = new XmlLayoutSerializer(dockingManager);
            serializer.LayoutSerializationCallback += (s, args) =>
            {
                args.Content = args.Content;
            };

            if (File.Exists(LayoutFileName))
                serializer.Deserialize(LayoutFileName);
        }

        public void Save(DockingManager dockingManager)
        {
            var layoutSerializer = new XmlLayoutSerializer(dockingManager);
            layoutSerializer.Serialize(LayoutFileName);
        }
    }
}

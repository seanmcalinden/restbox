using System;

namespace RestBox.Utilities
{
    public static class LayoutDocumentUtilities
    {
        public static Uri GetImageUri(LayoutDocumentType layoutDocumentType)
        {
            string imageName = "httprequest.png";
            switch (layoutDocumentType)
            {
                case LayoutDocumentType.StartPage:
                    imageName = "start-page-icon.png";
                    break;
                case LayoutDocumentType.HttpRequest:
                    imageName = "httprequest.png";
                    break;
                case LayoutDocumentType.Sequence:
                    imageName = "play-icon.png";
                    break;
                case LayoutDocumentType.Environment:
                    imageName = "environment-icon.png";
                    break;
                case LayoutDocumentType.Extension:
                    imageName = "requestextension-icon.png";
                    break;
            }

            return new Uri(string.Format("pack://application:,,,/RestBox;component/Images/{0}", imageName));
        }
    }
}

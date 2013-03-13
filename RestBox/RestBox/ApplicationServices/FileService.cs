using System;
using System.IO;
using System.Text;
using RestBox.Domain.Services;
using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public class FileService : IFileService
    {
        private readonly IJsonSerializer jsonSerializer;

        public FileService(IJsonSerializer jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer;
        }

        public void SaveFile(string filePath, string contents)
        {
            File.WriteAllText(filePath, contents);
        }

        public void SaveSolution()
        {
            if (Solution.Current.FilePath != null)
            {
                SaveFile(Solution.Current.FilePath, jsonSerializer.ToJsonString(Solution.Current));
            }
        }

        public T Load<T>(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var fileContent = reader.ReadToEnd();
                        return jsonSerializer.FromJsonString<T>(fileContent);
                    }
            }
        }

        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public void MoveFile(string sourceFilePath, string destinationFilePath)
        {
            if(!FileExists(sourceFilePath))
            {
                throw new Exception(string.Format("Cannot move file {0}", sourceFilePath));
            }

            File.Move(sourceFilePath, destinationFilePath);
        }

        public string GetFilePath(string solutionPath, string relativeFilePath)
        {
            var directoryParts = new Uri(solutionPath).Segments;

            var sb = new StringBuilder();
            for (var i = 1; i < directoryParts.Length - 1; i++)
            {
                sb.Append(directoryParts[i]);
            }
            sb.Append(relativeFilePath);
            var filePath = sb.ToString();
            return filePath;
        }

        public string GetRelativePath(Uri solutionPath, string fileName)
        {
            var newFilePath = new Uri(fileName);
            var pathDifference = solutionPath.MakeRelativeUri(newFilePath);
            var relativePath = pathDifference.OriginalString;
            return relativePath.Replace("%20", " ");
        }
    }
}

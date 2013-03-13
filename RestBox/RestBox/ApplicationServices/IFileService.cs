using System;

namespace RestBox.ApplicationServices
{
    public interface IFileService
    {
        void SaveFile(string filePath, string contents);
        void SaveSolution();
        T Load<T>(string filePath);
        void DeleteFile(string filePath);
        bool FileExists(string filePath);
        string GetFilePath(string solutionPath, string relativeFilePath);
        string GetRelativePath(Uri solutionPath, string fileName);
        void MoveFile(string sourceFilePath, string destinationFilePath);
    }
}

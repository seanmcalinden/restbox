using System.Linq;
using RestBox.Domain.Services;
using RestBox.ViewModels;
using System.Diagnostics;

namespace RestBox.ApplicationServices
{
    public class RestBoxStateService : IRestBoxStateService
    {
        private const string stateFileLocation = @".\RestBox.state";
        private readonly IFileService fileService;
        private readonly IJsonSerializer jsonSerializer;

        public RestBoxStateService(IFileService fileService, IJsonSerializer jsonSerializer)
        {
            this.fileService = fileService;
            this.jsonSerializer = jsonSerializer;
        }

        public void SaveState(RestBoxStateFile restBoxStateFile)
        {
            var restBoxState = new RestBoxState();

            if (fileService.FileExists(stateFileLocation))
            {
                restBoxState = fileService.Load<RestBoxState>(stateFileLocation);
            }

            for (var i = restBoxState.RestBoxStateFiles.Count - 1; i >= 0; i--)
            {
                if (restBoxState.RestBoxStateFiles[i].FilePath == restBoxStateFile.FilePath)
                {
                    restBoxState.RestBoxStateFiles.RemoveAt(i);
                }
            }

            restBoxState.RestBoxStateFiles.Insert(0, restBoxStateFile);

            restBoxState.RestBoxStateFiles = restBoxState.RestBoxStateFiles.Take(10).ToList();

            fileService.SaveFile(stateFileLocation, jsonSerializer.ToJsonString(restBoxState));
        }

        public RestBoxState GetState()
        {
            if (fileService.FileExists(stateFileLocation))
            {
                return fileService.Load<RestBoxState>(stateFileLocation);
            }
            return null;
        }

        public RestBoxState RemoveRestBoxStateFile(RestBoxStateFile restBoxStateFile)
        {
            if (!fileService.FileExists(stateFileLocation))
            {
                return null;
            }

            var restBoxState = fileService.Load<RestBoxState>(stateFileLocation);

            for (var i = restBoxState.RestBoxStateFiles.Count - 1; i >= 0; i--)
            {
                if (restBoxState.RestBoxStateFiles[i].FilePath == restBoxStateFile.FilePath)
                {
                    restBoxState.RestBoxStateFiles.RemoveAt(i);
                }
            }
            
            restBoxState.RestBoxStateFiles = restBoxState.RestBoxStateFiles.Take(10).ToList();

            fileService.SaveFile(stateFileLocation, jsonSerializer.ToJsonString(restBoxState));

            return restBoxState;
        }
    }
}
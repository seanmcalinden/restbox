using RestBox.ViewModels;

namespace RestBox.ApplicationServices
{
    public interface IRestBoxStateService
    {
        void SaveState(RestBoxStateFile restBoxStateFile);
        RestBoxState GetState();
        RestBoxState RemoveRestBoxStateFile(RestBoxStateFile restBoxStateFile);
    }
}

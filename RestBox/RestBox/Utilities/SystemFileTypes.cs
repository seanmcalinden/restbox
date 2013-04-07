namespace RestBox.Utilities
{
    public static class SystemFileTypes
    {
        public static FileType Solution = new FileType("rbsln", "REST Box Solution (*.rbsln)|*.rbsln", "Create Solution", "Open Solution", "", "");
        public static FileType HttpRequest = new FileType("rbhrq", "REST Box Http Request (*.rbhrq)|*.rbhrq", "Create Http Request", "Open Http Request", "Add Existing Http Request", "Save Http Request As..");
        public static FileType HttpSequence = new FileType("rbseq", "REST Box Sequence (*.rbseq)|*.rbseq", "Create Sequence", "Open Sequence", "Add Existing Sequence", "Save Sequence As...");
        public static FileType Interceptor = new FileType("rbicpt", "REST Box Interceptor (*.rbicpt)|*.rbicpt", "Create Interceptor", "Open Interceptor", "Add Existing Interceptor", "Save Interceptor As...");
        public static FileType Environment = new FileType("rbenv", "REST Box Environment (*.rbenv)|*.rbenv", "Create Environment", "Open Environment", "Add Existing Environment", "Save Environment As...");
        public static FileType RequestExtension = new FileType("exe", "Executable (*.exe)|*.exe", "", "", "Add Request Extension", "");

    }
}

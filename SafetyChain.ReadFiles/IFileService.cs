namespace SafetyChain.ReadFiles
{
    internal interface IFileService
    {
        void Load(string readFolderPath);
        void ProcessDocumentsUsingProducerConsumerPattern();
        void Save(string cs);
    }
}
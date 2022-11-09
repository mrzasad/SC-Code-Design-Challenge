using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyChain.ReadFiles
{
    internal class FileService : IFileService
    {
        private static BlockingCollection<string> fileNameQueue = new BlockingCollection<string>();
        private static BlockingCollection<FileData> inputQueue = new BlockingCollection<FileData>();
        private static Task[] consumerTask;
        private static Task saveTask;
        public void Load(string readFolderPath)
        {
            Console.WriteLine("Loading files..");
            var files = Directory.EnumerateFiles(readFolderPath);

            foreach (var id in files)
                fileNameQueue.Add(id);

            fileNameQueue.CompleteAdding();
            int filesCount = files.Count();
            Console.WriteLine(filesCount + " Found");
            consumerTask = Enumerable.Range(0, filesCount > 7 ? 7 : filesCount)
        .Select(_ => Task.Run(async () =>
            {
                foreach (var file in fileNameQueue.GetConsumingEnumerable())
                {
                    Console.WriteLine($"adding data from {file} to the queue");
                    string contents = await GetContentsFromFilesInsideFolder(Path.Combine(readFolderPath, file));
                    var fileJtoken = JToken.Parse(contents);
                    if (fileJtoken.Type is JTokenType.Array)
                    {
                        List<FileData> filesData = fileJtoken.ToObject<List<FileData>>();
                        foreach (var fileData in filesData)
                        {
                            Console.WriteLine("adding file data:" + fileData.Id + " to the queue");
                            inputQueue.Add(fileData);
                        }
                    }
                    else
                    {
                        FileData fileData = fileJtoken.ToObject<FileData>();
                        Console.WriteLine("adding file data:" + fileData.Id + " to the queue");
                        inputQueue.Add(fileData);
                    }
                }
            })).ToArray();
        }

        private static async Task<string> GetContentsFromFilesInsideFolder(string filePath)
        {
            byte[] result;
            using (FileStream SourceStream = File.Open(filePath, FileMode.Open))
            {
                result = new byte[SourceStream.Length];
                await SourceStream.ReadAsync(result, 0, (int)SourceStream.Length);
            }
            string contents = Encoding.ASCII.GetString(result);
            return contents;
        }

        public void Save(string connectionString)
        {
            Console.WriteLine("Saving files..");
            saveTask = Task.Run(() =>
            {
                foreach (var fileData in inputQueue.GetConsumingEnumerable())
                {
                    Console.WriteLine("Saving data id: {0}", fileData.Id);
                    string query = "INSERT INTO [dbo].[FileData]([Id],[Name],[CreatedOn],[CreatedBy])";
                    query += " VALUES (@Id, @Name, @CreatedOn, @CreatedBy)";
                    using (var conn = new SqlConnection(connectionString))
                    {
                        SqlCommand myCommand = new SqlCommand(query, conn);
                        myCommand.Parameters.AddWithValue("@Id", fileData.Id);
                        myCommand.Parameters.AddWithValue("@Name", fileData.Name);
                        myCommand.Parameters.AddWithValue("@CreatedOn", fileData.CreatedOn);
                        myCommand.Parameters.AddWithValue("@CreatedBy", fileData.CreatedBy);
                        conn.Open();
                        myCommand.ExecuteNonQuery();
                        conn.Close();
                    }
                    Console.WriteLine("Saved successfully!");
                }
                Console.WriteLine("Data Saved!");
            });
        }
        public void ProcessDocumentsUsingProducerConsumerPattern()
        {
            Task.WaitAll(consumerTask);
            inputQueue.CompleteAdding();
            saveTask.Wait();
        }
    }
}

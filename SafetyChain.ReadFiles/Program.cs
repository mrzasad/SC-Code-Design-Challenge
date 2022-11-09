using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace SafetyChain.ReadFiles
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
            .AddSingleton<IFileService, FileService>()
            .BuildServiceProvider();

            var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appSettings.json");

            var configuration = builder.Build();

            Console.WriteLine("Hello, SafetyChain! Welcome to Read Files Application");
            Console.WriteLine(String.Format("How do you want to read files?"));
            string folderPath = "";
            bool isReadFolderConfigured = false;
            while (!isReadFolderConfigured)
            {
                Console.WriteLine("1. Read from configured folder");
                Console.WriteLine("2. Enter custom folder path");
                var readKey = Console.ReadLine();
                switch (readKey)
                {
                    case "1":
                        string configurationKey = "ReadFolder";
                        try
                        {
                            folderPath = configuration[configurationKey].ToString();
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(configurationKey + "not configured");
                            folderPath = "";
                            isReadFolderConfigured = false;
                        }
                        break;
                    case "2":
                        Console.WriteLine("Please input the folder");
                        folderPath = Console.ReadLine();
                        try
                        {
                            Path.GetFullPath(folderPath);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Please input valid path");
                            folderPath = "";
                            isReadFolderConfigured = false;
                        }
                        break;
                    default:
                        Console.WriteLine("Wrong Input");
                        break;
                }
                if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
                {
                    isReadFolderConfigured = true;
                }
                else
                {
                    Console.WriteLine("Invalid Path");
                    isReadFolderConfigured = false;
                }
            }
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }
            IFileService fs = serviceProvider.GetService<IFileService>();

            fs.Load(folderPath);

            fs.Save(configuration["DbConnection"]);

            fs.ProcessDocumentsUsingProducerConsumerPattern();

            Console.ReadKey();
        }
    }
}

using System;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace NoteTakingApp
{
    class Program
    {
        private static IConfiguration Configuration;

        static void Main(string[] args)
        {
            // Load Configuration
            LoadConfiguration();

            // Default values
            string category = Configuration["DefaultCategory"];
            string filename = DateTime.Now.ToString("yyyy-MM-dd") + ".md";
            string noteText = null;

            // Parse Command-Line Arguments
            if (args.Length > 0)
            {
                if (args[0].StartsWith("-c"))
                {
                    // Category and filename are specified
                    if (args.Length > 1)
                    {
                        string[] parts = args[1].Split('/');

                        if (parts.Length > 0)
                        {
                            category = parts[0];
                        }
                        else
                        {
                            Console.WriteLine("Invalid Category format. Use -c Category/filename.md \"note text\"");
                            return;
                        }

                        if (parts.Length > 1)
                        {
                            filename = parts[1];
                            if (!filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                            {
                                filename += ".md";
                            }
                        }
                        else
                        {
                            filename = DateTime.Now.ToString("yyyy-MM-dd") + ".md";
                        }


                        // Capture all remaining arguments as note text
                        noteText = string.Join(" ", args.Skip(1));


                    }
                    else
                    {
                        Console.WriteLine("Invalid Category format. Use -c Category/filename.md \"note text\"");
                        return;
                    }

                }
                else
                {
                    // No category specified, just note text
                    noteText = string.Join(" ", args);
                }
            }

            // Construct Note Path
            string noteDirectory = Configuration["NoteDirectory"];
            if (!Path.IsPathFullyQualified(noteDirectory))
            {
                noteDirectory = Path.Combine(Environment.CurrentDirectory, noteDirectory);
            }
            string fullCategoryPath = Path.Combine(noteDirectory, category);
            string notePath = Path.Combine(fullCategoryPath, filename);
            string editor = Configuration["Editor"];
            string templatePath = Configuration["TemplateNote"];

            // Create Directory if it doesn't exist.
            if (!Directory.Exists(fullCategoryPath))
            {
                Directory.CreateDirectory(fullCategoryPath);
            }

            // Create or append note
            CreateNote(notePath, templatePath, noteText);

            // Open Note
            Console.WriteLine($"Opening note: {notePath}");
            OpenNote(notePath, editor);
        }


        static void LoadConfiguration()
        {
            // Get the directory where the executable is located
            string executablePath = AppContext.BaseDirectory;
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(executablePath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        static void CreateNote(string notePath, string templatePath, string noteText = null)
        {
            try
            {
                bool fileExists = File.Exists(notePath);
                
                if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
                {
                    string template = File.ReadAllText(templatePath);
                    string content = template
                        .Replace("{{time}}", DateTime.Now.ToString("HH:mm:ss"))
                        .Replace("{{note}}", noteText ?? ""); // Use empty string if noteText is null

                    if (fileExists)
                    {
                        File.AppendAllText(notePath, "\n" + content);
                        Console.WriteLine($"Appended text to note: {notePath}");
                    }
                    else
                    {
                        File.WriteAllText(notePath, content);
                        Console.WriteLine($"Created new note: {notePath}");
                    }
                }
                else
                {
                    if (fileExists)
                    {
                        using (StreamWriter sw = File.AppendText(notePath))
                        {
                            sw.WriteLine($"\n{DateTime.Now.ToString("HH:mm:ss")}\n{noteText}");
                        }
                        Console.WriteLine($"Appended text to note: {notePath}");
                    }
                    else
                    {
                        File.Create(notePath).Close();
                        Console.WriteLine($"Created new note: {notePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {(File.Exists(notePath) ? "appending to" : "creating")} note: {ex.Message}");
            }
        }

        static void AppendTextToNote(string notePath, string text)
        {
            try
            {
                string templatePath = Configuration["TemplateNote"];
                if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
                {
                    string template = File.ReadAllText(templatePath);
                    string content = template
                        .Replace("{{time}}", DateTime.Now.ToString("HH:mm:ss"))
                        .Replace("{{note}}", text);

                    File.AppendAllText(notePath, "\n" + content);
                }
                else
                {
                    // Fallback to original behavior if no template exists
                    using (StreamWriter sw = File.AppendText(notePath))
                    {
                        sw.WriteLine("\n" + DateTime.Now.ToString("HH:mm:ss") + "\n" + text);
                    }
                }
                Console.WriteLine($"Appended text to note: {notePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error appending text to note: {ex.Message}");
            }
        }


        static void OpenNote(string notePath, string editor)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = editor;
                psi.Arguments = $"\"{notePath}\""; // Quote the path
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening note: {ex.Message}");
            }
        }
    }
}
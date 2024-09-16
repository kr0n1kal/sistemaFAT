using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

class Program
{
    static List<FATEntry> fatTable = new List<FATEntry>();

    static void Main(string[] args)
    {
        LoadFATTable();
        MainMenu();
        SaveFATTable();
    }

    static void MainMenu()
    {
        int option;
        do
        {
            Console.Clear();
            Console.WriteLine("Sistema de Archivos FAT");
            Console.WriteLine("1. Crear un archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir un archivo");
            Console.WriteLine("4. Modificar un archivo");
            Console.WriteLine("5. Eliminar un archivo");
            Console.WriteLine("6. Recuperar un archivo");
            Console.WriteLine("7. Salir");
            Console.Write("Seleccione una opción: ");
            option = int.Parse(Console.ReadLine());

            switch (option)
            {
                case 1:
                    CreateFile();
                    break;
                case 2:
                    ListFiles();
                    break;
                case 3:
                    OpenFile();
                    break;
                case 4:
                    ModifyFile();
                    break;
                case 5:
                    DeleteFile();
                    break;
                case 6:
                    RecoverFile();
                    break;
                case 7:
                    Console.WriteLine("Saliendo del programa...");
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }

            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
        } while (option != 7);
    }

    static void CreateFile()
    {
        Console.Write("Ingrese el nombre del archivo: ");
        string fileName = Console.ReadLine();
        Console.WriteLine("Ingrese los datos del archivo (máximo 20 caracteres por segmento):");
        string data = Console.ReadLine();

        FATEntry entry = new FATEntry
        {
            FileName = fileName,
            FilePath = $"{fileName}_fat.json",
            IsDeleted = false,
            TotalCharacters = data.Length,
            CreationDate = DateTime.Now,
            ModificationDate = DateTime.Now,
        };

        SaveDataSegments(data, entry.FilePath);
        fatTable.Add(entry);
        Console.WriteLine("Archivo creado exitosamente.");
    }

    static void SaveDataSegments(string data, string basePath)
    {
        string previousPath = null;

        for (int i = 0; i < data.Length; i += 20)
        {
            string segmentData = data.Substring(i, Math.Min(20, data.Length - i));
            string segmentPath = $"{basePath}_{i / 20}.json";

            DataSegment segment = new DataSegment
            {
                Data = segmentData,
                NextFilePath = null,
                Eof = i + 20 >= data.Length
            };

            if (previousPath != null)
            {
                UpdateNextFilePath(previousPath, segmentPath);
            }

            previousPath = segmentPath;
            File.WriteAllText(segmentPath, JsonConvert.SerializeObject(segment, Formatting.Indented));
        }
    }

    static void UpdateNextFilePath(string currentPath, string nextPath)
    {
        var segment = JsonConvert.DeserializeObject<DataSegment>(File.ReadAllText(currentPath));
        segment.NextFilePath = nextPath;
        File.WriteAllText(currentPath, JsonConvert.SerializeObject(segment, Formatting.Indented));
    }

    static void ListFiles()
    {
        Console.WriteLine("Archivos disponibles:");
        int index = 1;

        foreach (var file in fatTable)
        {
            if (!file.IsDeleted)
            {
                Console.WriteLine($"{index}. {file.FileName} - {file.TotalCharacters} caracteres, Creado: {file.CreationDate}, Modificado: {file.ModificationDate}");
                index++;
            }
        }
    }

    static void OpenFile()
    {
        ListFiles();
        Console.Write("Seleccione el número del archivo que desea abrir: ");
        int selection = int.Parse(Console.ReadLine()) - 1;

        if (selection >= 0 && selection < fatTable.Count && !fatTable[selection].IsDeleted)
        {
            var file = fatTable[selection];
            Console.WriteLine($"Archivo: {file.FileName} - {file.TotalCharacters} caracteres, Creado: {file.CreationDate}, Modificado: {file.ModificationDate}");
            Console.WriteLine("Contenido:");

            string currentPath = $"{file.FilePath}_0.json";

            while (File.Exists(currentPath))
            {
                var segment = JsonConvert.DeserializeObject<DataSegment>(File.ReadAllText(currentPath));
                Console.Write(segment.Data);
                if (segment.Eof) break;
                currentPath = segment.NextFilePath;
            }

            Console.WriteLine("\nFin del archivo.");
        }
        else
        {
            Console.WriteLine("Selección inválida.");
        }
    }

    static void ModifyFile()
    {
        ListFiles();
        Console.Write("Seleccione el número del archivo que desea modificar: ");
        int selection = int.Parse(Console.ReadLine()) - 1;

        if (selection >= 0 && selection < fatTable.Count && !fatTable[selection].IsDeleted)
        {
            var file = fatTable[selection];
            Console.WriteLine("Contenido actual del archivo:");
            OpenFile();

            Console.WriteLine("Ingrese el nuevo contenido (presione ESC para finalizar):");
            string newData = "";

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) break;
                Console.Write(key.KeyChar);
                newData += key.KeyChar;
            }

            Console.WriteLine("\n¿Desea guardar los cambios? (S/N)");
            if (Console.ReadKey(true).Key == ConsoleKey.S)
            {
                DeleteSegments($"{file.FilePath}_0.json");
                SaveDataSegments(newData, file.FilePath);
                file.TotalCharacters = newData.Length;
                file.ModificationDate = DateTime.Now;
                Console.WriteLine("Archivo modificado exitosamente.");
            }
            else
            {
                Console.WriteLine("Cambios descartados.");
            }
        }
        else
        {
            Console.WriteLine("Selección inválida.");
        }
    }

    static void DeleteFile()
    {
        ListFiles();
        Console.Write("Seleccione el número del archivo que desea eliminar: ");
        int selection = int.Parse(Console.ReadLine()) - 1;

        if (selection >= 0 && selection < fatTable.Count && !fatTable[selection].IsDeleted)
        {
            var file = fatTable[selection];
            Console.WriteLine($"¿Está seguro que desea eliminar el archivo '{file.FileName}'? (S/N)");
            if (Console.ReadKey(true).Key == ConsoleKey.S)
            {
                file.IsDeleted = true;
                file.DeletionDate = DateTime.Now;
                Console.WriteLine("Archivo eliminado y enviado a la papelera.");
            }
            else
            {
                Console.WriteLine("Eliminación cancelada.");
            }
        }
        else
        {
            Console.WriteLine("Selección inválida.");
        }
    }

    static void RecoverFile()
    {
        Console.WriteLine("Archivos en la papelera:");
        int index = 1;

        foreach (var file in fatTable)
        {
            if (file.IsDeleted)
            {
                Console.WriteLine($"{index}. {file.FileName} - {file.TotalCharacters} caracteres, Eliminado: {file.DeletionDate}");
                index++;
            }
        }

        Console.Write("Seleccione el número del archivo que desea recuperar: ");
        int selection = int.Parse(Console.ReadLine()) - 1;

        if (selection >= 0 && selection < fatTable.Count && fatTable[selection].IsDeleted)
        {
            var file = fatTable[selection];
            Console.WriteLine($"¿Está seguro que desea recuperar el archivo '{file.FileName}'? (S/N)");
            if (Console.ReadKey(true).Key == ConsoleKey.S)
            {
                file.IsDeleted = false;
                file.DeletionDate = null;
                Console.WriteLine("Archivo recuperado exitosamente.");
            }
            else
            {
                Console.WriteLine("Recuperación cancelada.");
            }
        }
        else
        {
            Console.WriteLine("Selección inválida.");
        }
    }

    static void DeleteSegments(string startPath)
    {
        string currentPath = startPath;

        while (File.Exists(currentPath))
        {
            var segment = JsonConvert.DeserializeObject<DataSegment>(File.ReadAllText(currentPath));
            string nextPath = segment.NextFilePath;
            File.Delete(currentPath);
            if (segment.Eof) break;
            currentPath = nextPath;
        }
    }

    static void LoadFATTable()
    {
        if (File.Exists("fat_table.json"))
        {
            string json = File.ReadAllText("fat_table.json");
            fatTable = JsonConvert.DeserializeObject<List<FATEntry>>(json);
        }
    }

    static void SaveFATTable()
    {
        string json = JsonConvert.SerializeObject(fatTable, Formatting.Indented);
        File.WriteAllText("fat_table.json", json);
    }
}

class FATEntry
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int TotalCharacters { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime ModificationDate { get; set; }
    public DateTime? DeletionDate { get; set; }
}

class DataSegment
{
    public string Data { get; set; }
    public string NextFilePath { get; set; }
    public bool Eof { get; set; }
}
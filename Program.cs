using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

class ArchivoDatos
{
    public string Datos { get; set; }
    public string? SiguienteArchivo { get; set; }
    public bool EOF { get; set; }

    public ArchivoDatos(string datos)
    {
        Datos = datos;
        SiguienteArchivo = null;
        EOF = true;
    }
}

class FAT
{
    public string NombreArchivo { get; set; }
    public string RutaInicial { get; set; }
    public bool Papelera { get; set; }
    public int TamañoCaracteres { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public FAT(string nombreArchivo, string rutaInicial, int tamañoCaracteres)
    {
        NombreArchivo = nombreArchivo;
        RutaInicial = rutaInicial;
        Papelera = false;
        TamañoCaracteres = tamañoCaracteres;
        FechaCreacion = DateTime.Now;
        FechaModificacion = DateTime.Now;
        FechaEliminacion = null;
    }
}

class ProgramaFAT
{
    static List<FAT> tablaFAT = new List<FAT>();

    static void Main(string[] args)
    {
        string opcion = "0";
        while (opcion != "7")
        {
            Console.Clear();
            Console.WriteLine("1. Crear archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir archivo");
            Console.WriteLine("4. Modificar archivo");
            Console.WriteLine("5. Eliminar archivo");
            Console.WriteLine("6. Recuperar archivo");
            Console.WriteLine("7. Salir");
            Console.WriteLine("Elige una opción:");
            opcion = Console.ReadLine()!;

            switch (opcion)
            {
                case "1":
                    CrearArchivo();
                    break;
                case "2":
                    ListarArchivos();
                    break;
                case "3":
                    AbrirArchivo();
                    break;
                case "4":
                    ModificarArchivo();
                    break;
                case "5":
                    EliminarArchivo();
                    break;
                case "6":
                    RecuperarArchivo();
                    break;
            }
        }
    }

    static void CrearArchivo()
    {
        Console.WriteLine("Ingrese el nombre del archivo:");
        string nombre = Console.ReadLine()!;
        Console.WriteLine("Ingrese el contenido (máx. 100 caracteres):");
        string contenido = Console.ReadLine()!;

        // Crear fragmentos de datos
        List<string> fragmentos = new List<string>();
        for (int i = 0; i < contenido.Length; i += 20)
        {
            string fragmento = contenido.Substring(i, Math.Min(20, contenido.Length - i));
            fragmentos.Add(fragmento);
        }

        // Serializar fragmentos
        string rutaInicial = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + nombre + "_0.json";
        string rutaActual = rutaInicial;
        for (int i = 0; i < fragmentos.Count; i++)
        {
            ArchivoDatos archivoDatos = new ArchivoDatos(fragmentos[i]);
            if (i < fragmentos.Count - 1)
                archivoDatos.SiguienteArchivo = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + nombre + "_" + (i + 1) + ".json";

            string jsonData = JsonSerializer.Serialize(archivoDatos);
            File.WriteAllText(rutaActual, jsonData);
            rutaActual = archivoDatos.SiguienteArchivo ?? null;
        }

        // Crear y guardar entrada FAT
        FAT nuevaFAT = new FAT(nombre, rutaInicial, contenido.Length);
        tablaFAT.Add(nuevaFAT);
        Console.WriteLine("Archivo creado con éxito.");
        Console.ReadKey();
    }

    static void ListarArchivos()
    {
        int count = 1;
        foreach (var archivo in tablaFAT)
        {
            if (!archivo.Papelera)
            {
                Console.WriteLine($"{count}. Nombre: {archivo.NombreArchivo}, Tamaño: {archivo.TamañoCaracteres}, Creado: {archivo.FechaCreacion}, Modificado: {archivo.FechaModificacion}");
                count++;
            }
        }
        Console.ReadKey();
    }

    static void AbrirArchivo()
    {
        ListarArchivos();
        Console.WriteLine("Seleccione el número del archivo para abrir:");
        int index = int.Parse(Console.ReadLine()!) - 1;

        if (index >= 0 && index < tablaFAT.Count)
        {
            FAT archivoSeleccionado = tablaFAT[index];

            if (archivoSeleccionado.Papelera)
            {
                Console.WriteLine("El archivo está en la papelera.");
            }
            else
            {
                string rutaActual = archivoSeleccionado.RutaInicial;
                string contenidoCompleto = "";

                while (rutaActual != null)
                {
                    string jsonData = File.ReadAllText(rutaActual);
                    ArchivoDatos datos = JsonSerializer.Deserialize<ArchivoDatos>(jsonData)!;
                    contenidoCompleto += datos.Datos;
                    rutaActual = datos.SiguienteArchivo ?? null;
                }

                Console.WriteLine($"Contenido del archivo {archivoSeleccionado.NombreArchivo}:");
                Console.WriteLine(contenidoCompleto);
            }
        }
        Console.ReadKey();
    }

    static void ModificarArchivo()
    {
        AbrirArchivo();
        Console.WriteLine("Ingrese el nuevo contenido (máx. 100 caracteres) o presione ESC para cancelar:");

        // Capturar entrada de texto, detectando ESC
        string nuevoContenido = CapturarEntradaConEscape();
        if (nuevoContenido == null)
        {
            Console.WriteLine("Modificación cancelada.");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("¿Desea guardar los cambios? (s/n)");
        string confirmacion = Console.ReadLine()!;
        if (confirmacion.ToLower() != "s")
        {
            Console.WriteLine("Cambios descartados.");
            Console.ReadKey();
            return;
        }

        FAT archivoSeleccionado = tablaFAT.Find(archivo => archivo.NombreArchivo == archivo.NombreArchivo)!;

        // Eliminar archivos anteriores
        string rutaActual = archivoSeleccionado.RutaInicial;
        while (rutaActual != null)
        {
            string jsonData = File.ReadAllText(rutaActual);
            ArchivoDatos datos = JsonSerializer.Deserialize<ArchivoDatos>(jsonData)!;
            File.Delete(rutaActual);
            rutaActual = datos.SiguienteArchivo ?? null;
        }

        // Crear nuevos archivos
        CrearArchivoConDatos(archivoSeleccionado.NombreArchivo, nuevoContenido);
        archivoSeleccionado.FechaModificacion = DateTime.Now;

        Console.WriteLine("Archivo modificado con éxito.");
        Console.ReadKey();
    }

    static string? CapturarEntradaConEscape()
    {
        string entrada = "";
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
                return null;
            if (key.Key == ConsoleKey.Backspace && entrada.Length > 0)
                entrada = entrada.Substring(0, entrada.Length - 1);
            else
                entrada += key.KeyChar;
        } while (key.Key != ConsoleKey.Enter);
        return entrada;
    }

    static void CrearArchivoConDatos(string nombreArchivo, string contenido)
    {
        List<string> fragmentos = new List<string>();
        for (int i = 0; i < contenido.Length; i += 20)
        {
            string fragmento = contenido.Substring(i, Math.Min(20, contenido.Length - i));
            fragmentos.Add(fragmento);
        }

        string rutaInicial = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + nombreArchivo + "_0.json";
        string rutaActual = rutaInicial;
        for (int i = 0; i < fragmentos.Count; i++)
        {
            ArchivoDatos archivoDatos = new ArchivoDatos(fragmentos[i]);
            if (i < fragmentos.Count - 1)
                archivoDatos.SiguienteArchivo = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + nombreArchivo + "_" + (i + 1) + ".json";

            string jsonData = JsonSerializer.Serialize(archivoDatos);
            File.WriteAllText(rutaActual, jsonData);
            rutaActual = archivoDatos.SiguienteArchivo ?? null;
        }
        Console.WriteLine("Archivo creado con éxito.");
    }

    static void EliminarArchivo()
    {
        ListarArchivos();
        Console.WriteLine("Seleccione el número del archivo para eliminar:");
        int index = int.Parse(Console.ReadLine()!) - 1;

        if (index >= 0 && index < tablaFAT.Count)
        {
            FAT archivoSeleccionado = tablaFAT[index];
            Console.WriteLine($"¿Está seguro de que desea eliminar el archivo {archivoSeleccionado.NombreArchivo}? (s/n)");
            string confirmacion = Console.ReadLine()!;

            if (confirmacion.ToLower() == "s")
            {
                archivoSeleccionado.Papelera = true;
                archivoSeleccionado.FechaEliminacion = DateTime.Now;
                Console.WriteLine($"El archivo {archivoSeleccionado.NombreArchivo} ha sido enviado a la papelera.");
            }
            else
            {
                Console.WriteLine("Eliminación cancelada.");
            }
        }
        Console.ReadKey();
    }

    static void RecuperarArchivo()
    {
        Console.WriteLine("Archivos en la papelera:");
        for (int i = 0; i < tablaFAT.Count; i++)
        {
            if (tablaFAT[i].Papelera)
            {
                Console.WriteLine($"{i + 1}. {tablaFAT[i].NombreArchivo}, Eliminado: {tablaFAT[i].FechaEliminacion}");
            }
        }

        Console.WriteLine("Seleccione el número del archivo para recuperar:");
        int index = int.Parse(Console.ReadLine()!) - 1;

        if (index >= 0 && index < tablaFAT.Count && tablaFAT[index].Papelera)
        {
            FAT archivoSeleccionado = tablaFAT[index];
            archivoSeleccionado.Papelera = false;
            archivoSeleccionado.FechaEliminacion = null;

            Console.WriteLine($"El archivo {archivoSeleccionado.NombreArchivo} ha sido recuperado.");
        }
        Console.ReadKey();
    }
}

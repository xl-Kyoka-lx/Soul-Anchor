using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoulAnchor.Managers
{
    public static class LocalizationManager
    {
        // Usamos AppContext.BaseDirectory (carpeta del .exe/bin) en vez de una ruta relativa,
        // porque el "directorio actual" con dotnet run es la carpeta del proyecto, no el bin de salida.
        private static readonly string rutaArchivo = Path.Combine(AppContext.BaseDirectory, "Data", "Localization", "ui_strings.json");

        private static Dictionary<string, Dictionary<string, string>>? textos;
        private static bool cargado = false;

        public static void Cargar()
        {
            if (cargado) return;

            try
            {
                string json = File.ReadAllText(rutaArchivo);
                textos = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar localización ({rutaArchivo}): {ex.Message}");
                textos = new Dictionary<string, Dictionary<string, string>>();
            }
            finally
            {
                // Se marca como cargado incluso si falló, para no reintentar leer el archivo
                // en cada frame (esto causaba el spam de errores repetidos en consola).
                cargado = true;
            }
        }

        // idioma: "es" o "en". clave: la key del JSON.
        public static string Obtener(string idioma, string clave)
        {
            if (!cargado) Cargar();

            string idiomaKey = idioma.ToLowerInvariant();

            if (textos != null &&
                textos.TryGetValue(idiomaKey, out var diccionarioIdioma) &&
                diccionarioIdioma.TryGetValue(clave, out var valor))
            {
                return valor;
            }

            // Fallback: si falta la clave o el idioma, devolvemos la clave misma para notar el error visualmente
            return $"[{clave}]";
        }

        public static string ObtenerFormateado(string idioma, string clave, params object[] args)
        {
            string plantilla = Obtener(idioma, clave);
            return string.Format(plantilla, args);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoulAnchor.Managers
{
    public static class LocalizationManager
    {
        private static readonly string rutaArchivo = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "Localization", "ui_strings.json");

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
                cargado = true;
            }
        }
        public static string Obtener(string idioma, string clave)
        {
            if (!cargado) Cargar();

            string idiomaKey = idioma.ToLowerInvariant();

            if (textos != null &&
                textos.TryGetValue(idiomaKey, out var diccionarioIdioma) &&
                diccionarioIdioma.TryGetValue(clave, out var valor))
            {
                return TextoEncoding.ACP437(valor);
            }

            return $"[{clave}]";
        }

        public static string ObtenerFormateado(string idioma, string clave, params object[] args)
        {
            string plantilla = Obtener(idioma, clave);
            return string.Format(plantilla, args);
        }
    }
}
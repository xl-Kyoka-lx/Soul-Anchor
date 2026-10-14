using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoulAnchor.Datos.Mundo;

namespace SoulAnchor.Managers
{
    // Carga Data/Jsons/soul anchor hechizos Stats/*.json (stats) y
    // Data/Jsons/Localization/hechizos_comunes_localization.json (nombre/descripción por idioma).
    public static class HechizoDatabase
    {
        private static readonly string carpetaStats = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "soul anchor hechizos Stats");
        private static readonly string rutaLocalizacion = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "Localization", "hechizos_comunes_localization.json");

        private static Dictionary<string, HechizoData>? datos;
        private static Dictionary<string, Dictionary<string, (string Nombre, string Descripcion)>>? localizacion;

        public static void Cargar()
        {
            if (datos != null) return;

            datos = new Dictionary<string, HechizoData>();

            if (!Directory.Exists(carpetaStats))
            {
                Console.WriteLine($"Error: no existe la carpeta de datos de hechizos ({carpetaStats})");
            }
            else
            {
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                foreach (var archivo in Directory.GetFiles(carpetaStats, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(archivo);
                        var raiz = JsonSerializer.Deserialize<HechizoRaiz>(json, opciones);
                        if (raiz?.Hechizos == null) continue;

                        foreach (var hechizo in raiz.Hechizos)
                        {
                            datos[hechizo.Id] = hechizo;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al cargar hechizos ({archivo}): {ex.Message}");
                    }
                }
            }

            CargarLocalizacion();
        }

        private static void CargarLocalizacion()
        {
            localizacion = new Dictionary<string, Dictionary<string, (string, string)>>();

            try
            {
                string json = File.ReadAllText(rutaLocalizacion);
                using var doc = JsonDocument.Parse(json);

                foreach (var idiomaProp in doc.RootElement.EnumerateObject())
                {
                    var porIdioma = new Dictionary<string, (string, string)>();

                    foreach (var itemProp in idiomaProp.Value.EnumerateObject())
                    {
                        string nombre = itemProp.Value.TryGetProperty("name", out var n) ? (n.GetString() ?? itemProp.Name) : itemProp.Name;
                        string desc = itemProp.Value.TryGetProperty("description", out var d) ? (d.GetString() ?? "") : "";
                        porIdioma[itemProp.Name] = (nombre, desc);
                    }

                    localizacion[idiomaProp.Name] = porIdioma;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar localización de hechizos ({rutaLocalizacion}): {ex.Message}");
            }
        }

        public static HechizoData Obtener(string id)
        {
            Cargar();

            if (datos != null && datos.TryGetValue(id, out var hechizo))
            {
                return hechizo;
            }

            throw new KeyNotFoundException($"No existe ficha de hechizo con id '{id}'. ¿Falta en el JSON o está mal escrito?");
        }

        public static (string Nombre, string Descripcion) ObtenerTexto(string id, string idioma = "es")
        {
            Cargar();

            if (localizacion != null &&
                localizacion.TryGetValue(idioma, out var porIdioma) &&
                porIdioma.TryGetValue(id, out var texto))
            {
                return texto;
            }

            return (id, "");
        }

        public static bool Existe(string id)
        {
            Cargar();
            return datos != null && datos.ContainsKey(id);
        }

        private class HechizoRaiz
        {
            [JsonPropertyName("hechizos")]
            public List<HechizoData> Hechizos { get; set; } = new();
        }
    }
}

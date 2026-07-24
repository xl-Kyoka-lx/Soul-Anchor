using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoulAnchor.Datos.Items;


namespace SoulAnchor.Managers
{
    public static class ItemDatabase
    {
        private static readonly string carpetaStats = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "soul anchor items Stats");
        private static readonly string rutaLocalizacion = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "Localization", "objetos_localization.json");
        private static Dictionary<string, ItemData>? datos;
        private static Dictionary<string, Dictionary<string, (string Nombre, string Descripcion)>>? localizacion;
        public static void Cargar()
        {
            if (datos != null) return;

            datos = new Dictionary<string, ItemData>();

            if (!Directory.Exists(carpetaStats))
            {
                Console.WriteLine($"Error: no existe la carpeta de datos de items ({carpetaStats})");
            }
            else
            {
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                foreach (var archivo in Directory.GetFiles(carpetaStats, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(archivo);
                        var raiz = JsonSerializer.Deserialize<ItemRaiz>(json, opciones);
                        if (raiz?.Items == null) continue;

                        foreach (var item in raiz.Items)
                        {
                            datos[item.Id] = item;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al cargar items ({archivo}): {ex.Message}");
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
                Console.WriteLine($"Error al cargar localización de items ({rutaLocalizacion}): {ex.Message}");
            }
        }

        public static ItemData ObtenerDatos(string id)
        {
            Cargar();

            if (datos != null && datos.TryGetValue(id, out var item))
            {
                return item;
            }

            throw new KeyNotFoundException($"No existe ficha de objeto con id '{id}'. ¿Falta en el JSON o está mal escrito?");
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

            return (id, ""); // fallback visible si falta la clave, igual que LocalizationManager
        }

        public static bool Existe(string id)
        {
            Cargar();
            return datos != null && datos.ContainsKey(id);
        }
        public static Item CrearInstancia(string id, string idioma = "es")
        {
            var d = ObtenerDatos(id);
            var (nombre, descripcion) = ObtenerTexto(id, idioma);

            return d.Category switch
            {
                "consumible" => new Consumible(
                    d.Id, nombre, descripcion, d.ValorOro,
                    curaHp: d.CuraHpPct > 0,
                    curaMp: d.CuraMpPct > 0,
                    porcentaje: d.CuraHpPct > 0 ? d.CuraHpPct : d.CuraMpPct,
                    esMaterial: false),

                "material" => new Material(d.Id, nombre, descripcion, d.ValorOro, d.UsadoEnCrafteo),

                "basura" => new ObjetoVendible(d.Id, nombre, descripcion, d.ValorOro),

                _ => throw new InvalidOperationException($"Categoría de item desconocida: '{d.Category}' (id: {id})")
            };
        }

        private class ItemRaiz
        {
            [JsonPropertyName("items")]
            public List<ItemData> Items { get; set; } = new();
        }
    }
}

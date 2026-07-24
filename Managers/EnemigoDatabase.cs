using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoulAnchor.Datos.Mundo;
using System.Linq;

namespace SoulAnchor.Managers
{
    public static class EnemigoDatabase
    {
        private static readonly string carpetaBase = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "soul anchor enemigos Stats");

        private static Dictionary<string, EnemigoData>? datos;

        public static void Cargar()
        {
            if (datos != null) return;

            datos = new Dictionary<string, EnemigoData>();

            if (!Directory.Exists(carpetaBase))
            {
                Console.WriteLine($"Error: no existe la carpeta de datos de enemigos ({carpetaBase})");
                return;
            }

            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var archivo in Directory.GetFiles(carpetaBase, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(archivo);
                    var raiz = JsonSerializer.Deserialize<EnemigoRaiz>(json, opciones);

                    if (raiz?.Enemies == null) continue;

                    foreach (var enemigo in raiz.Enemies)
                    {
                        datos[enemigo.Id] = enemigo;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar enemigos ({archivo}): {ex.Message}");
                }
            }
        }

        public static EnemigoData Obtener(string id)
        {
            Cargar();

            if (datos != null && datos.TryGetValue(id, out var enemigo))
            {
                return enemigo;
            }

            throw new KeyNotFoundException($"No existe ficha de enemigo con id '{id}'. ¿Falta en el JSON o está mal escrito?");
        }

        public static bool Existe(string id)
        {
            Cargar();
            return datos != null && datos.ContainsKey(id);
        }

        //para GameManager si más adelante quiero elegir un enemigo al azar de un bioma.
        public static IReadOnlyCollection<string> ObtenerTodosLosIds()
        {
            Cargar();
            return datos?.Keys.ToArray() ?? Array.Empty<string>();
        }

        private class EnemigoRaiz
        {
            [JsonPropertyName("enemies")]
            public List<EnemigoData> Enemies { get; set; } = new();
        }
    }
}
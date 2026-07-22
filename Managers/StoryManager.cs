using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoulAnchor.Managers
{
    // Maneja cualquier secuencia de texto narrativo (prólogo, introducción, reclutamiento, etc.)
    // Todas viven como JSON en Data/Jsons/Localization/Historia/, con la misma forma: { "es": [...], "en": [...] }
    public class StoryManager
    {
        private static readonly string carpetaBase = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "Localization", "Historia");

        // Cache de archivos ya leídos de disco, para no releer el mismo JSON varias veces en la misma partida
        private Dictionary<string, Dictionary<string, List<string>>> cache = new();

        private List<string>? paginasActivas;
        private int paginaActual;

        public bool SecuenciaTerminada => paginasActivas == null || paginaActual >= paginasActivas.Count;

        public string PaginaActualTexto => paginasActivas != null && paginaActual < paginasActivas.Count
            ? TextoEncoding.ACP437(paginasActivas[paginaActual])
            : string.Empty;

        public int PaginaActualIndice => paginaActual;
        public int TotalPaginas => paginasActivas?.Count ?? 0;

        // nombreArchivo ej: "Prologo_localizacion.json", "Intro_localizacion.json", "Reclutamiento_localizacion.json"
        public void IniciarSecuencia(string nombreArchivo, string idioma)
        {
            paginaActual = 0;

            if (!cache.TryGetValue(nombreArchivo, out var porIdioma))
            {
                porIdioma = CargarArchivo(nombreArchivo);
                cache[nombreArchivo] = porIdioma;
            }

            paginasActivas = porIdioma.TryGetValue(idioma, out var paginas) ? paginas : new List<string>();
        }

        public void AvanzarPagina()
        {
            if (paginasActivas == null) return;
            if (paginaActual < paginasActivas.Count) paginaActual++;
        }

        private Dictionary<string, List<string>> CargarArchivo(string nombreArchivo)
        {
            string ruta = Path.Combine(carpetaBase, nombreArchivo);

            try
            {
                string json = File.ReadAllText(ruta);
                return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                    ?? new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar secuencia de historia ({ruta}): {ex.Message}");
                return new Dictionary<string, List<string>>();
            }
        }
    }
}
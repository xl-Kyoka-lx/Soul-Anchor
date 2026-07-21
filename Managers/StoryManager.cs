using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoulAnchor.Managers
{
    public class StoryManager
    {
        private static readonly string rutaPrologo = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "Localization", "Historia", "Prologo_localizacion.json");

        private Dictionary<string, List<string>>? paginasPrologoPorIdioma;
        private List<string>? paginasPrologoActivas;
        private int paginaActual;

        public bool PrologoTerminado => paginasPrologoActivas == null || paginaActual >= paginasPrologoActivas.Count;

        public string PaginaActualTexto => paginasPrologoActivas != null && paginaActual < paginasPrologoActivas.Count
            ? TextoEncoding.ACP437(paginasPrologoActivas[paginaActual])
            : string.Empty;

        public int PaginaActualIndice => paginaActual;
        public int TotalPaginas => paginasPrologoActivas?.Count ?? 0;

        public void IniciarPrologo(string idioma)
        {
            CargarSiHaceFalta();

            paginaActual = 0;

            if (paginasPrologoPorIdioma != null && paginasPrologoPorIdioma.TryGetValue(idioma, out var paginas))
            {
                paginasPrologoActivas = paginas;
            }
            else
            {
                paginasPrologoActivas = new List<string>();
            }
        }

        public void AvanzarPagina()
        {
            if (paginasPrologoActivas == null) return;
            if (paginaActual < paginasPrologoActivas.Count) paginaActual++;
        }

        private void CargarSiHaceFalta()
        {
            if (paginasPrologoPorIdioma != null) return;

            try
            {
                string json = File.ReadAllText(rutaPrologo);
                paginasPrologoPorIdioma = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el prólogo: {ex.Message}");
                paginasPrologoPorIdioma = new Dictionary<string, List<string>>();
            }
        }
    }
}
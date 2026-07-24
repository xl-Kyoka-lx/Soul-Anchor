using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulAnchor.Managers
{
    // Una "pista" disponible para la opción Preguntar. No contiene texto: apunta a un archivo
    // JSON en Data/Jsons/Localization/Historia/ (mismo formato que Prologo/Intro/Reclutamiento),
    // que StoryManager ya sabe leer y paginar. Así reutilizamos toda la pantalla de texto histórico
    // que ya existe en vez de construir un renderer de diálogo nuevo.
    public class ReglaPregunta
    {
        public string Id { get; set; } = "";

        // Se evalúa contra el estado actual del juego. La primera regla (por Prioridad) cuya
        // Condicion devuelva true es la que se muestra.
        public Func<GameManager, bool> Condicion { get; set; } = _ => false;

        public string ArchivoDialogo { get; set; } = "";

        // Mayor prioridad se evalúa primero. Dejá la regla de fallback en int.MinValue.
        public int Prioridad { get; set; }
    }

    // Decide qué diálogo mostrar cuando el jugador usa "Preguntar" en una ciudad (GDD 9.6).
    // Para agregar un diálogo nuevo: 1) crear el JSON en Historia/ con el texto, 2) agregar una
    // ReglaPregunta acá (o llamar AgregarRegla desde donde corresponda) con la condición que
    // determine cuándo debe aparecer.
    public class PreguntaManager
    {
        private readonly List<ReglaPregunta> reglas;

        private const string ArchivoFallback = "Preguntar_Generico_localizacion.json";

        public PreguntaManager()
        {
            reglas = new List<ReglaPregunta>
            {
                // Sumire sugiere hacer misiones del gremio antes de viajar: solo aplica mientras
                // seguimos en la Ciudad del Oeste, ya se formó el equipo, y todavía no se completó
                // ni se aceptó ninguna misión (PuntosGremio en 0, sin misión activa).
                new ReglaPregunta
                {
                    Id = "sumire_sugiere_gremio_inicio",
                    Prioridad = 100,
                    Condicion = gm =>
                        gm.UbicacionActual?.Nombre == "Ciudad del Oeste" &&
                        gm.AnuncioColocado &&
                        gm.Gremio.PuntosGremio == 0 &&
                        gm.Gremio.MisionActiva == null,
                    ArchivoDialogo = "Preguntar_Sumire_SugerenciaGremio_localizacion.json"
                },

                // Fallback: siempre cumple, pero al tener la prioridad más baja solo se usa
                // cuando ninguna regla más específica aplicó.
                new ReglaPregunta
                {
                    Id = "generico_sin_pistas",
                    Prioridad = int.MinValue,
                    Condicion = _ => true,
                    ArchivoDialogo = ArchivoFallback
                }
            };
        }

        // Permite sumar reglas desde afuera (ej. un manager de misiones o de historia que
        // desbloquea una pista nueva) sin tener que tocar esta clase cada vez.
        public void AgregarRegla(ReglaPregunta regla)
        {
            reglas.Add(regla);
        }

        public string ObtenerDialogoActivo(GameManager gm)
        {
            var candidato = reglas
                .Where(r => r.Condicion(gm))
                .OrderByDescending(r => r.Prioridad)
                .FirstOrDefault();

            return candidato?.ArchivoDialogo ?? ArchivoFallback;
        }
    }
}

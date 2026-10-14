using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SoulAnchor.Datos.Mundo;

namespace SoulAnchor.Managers
{
    // Lee Data/Jsons/soul anchor mapa/mapa.json y monstruos_*.json (mismo patrón que EnemigoDatabase).
    // El mapa es un plano cartesiano de enteros de -Limite a +Limite; cada paso mueve 1 casilla.
    public static class MapaDatabase
    {
        private static readonly string carpeta = Path.Combine(AppContext.BaseDirectory, "Data", "Jsons", "soul anchor mapa");

        private static MapaData? mapa;
        private static List<Ubicacion> ubicaciones = new List<Ubicacion>();
        private static Dictionary<string, TablaMonstruosData> tablas = new Dictionary<string, TablaMonstruosData>();

        public static int Limite { get { Cargar(); return mapa?.Limite ?? 13; } }

        public static IReadOnlyList<Ubicacion> Ubicaciones { get { Cargar(); return ubicaciones; } }

        public static void Cargar()
        {
            if (mapa != null) return;

            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            mapa = new MapaData();

            try
            {
                string json = File.ReadAllText(Path.Combine(carpeta, "mapa.json"));
                mapa = JsonSerializer.Deserialize<MapaData>(json, opciones) ?? new MapaData();
                ubicaciones = mapa.Ubicaciones.Select(u => new Ubicacion(u)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el mapa ({carpeta}): {ex.Message}");
            }

            if (!Directory.Exists(carpeta)) return;

            foreach (var archivo in Directory.GetFiles(carpeta, "monstruos_*.json"))
            {
                try
                {
                    var tabla = JsonSerializer.Deserialize<TablaMonstruosData>(File.ReadAllText(archivo), opciones);
                    if (tabla != null && tabla.Id != "") tablas[tabla.Id] = tabla;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar tabla de monstruos ({archivo}): {ex.Message}");
                }
            }
        }

        public static Ubicacion? ObtenerPorId(string id)
        {
            return Ubicaciones.FirstOrDefault(u => u.Id == id);
        }

        public static Ubicacion? UbicacionEn(int x, int y)
        {
            return Ubicaciones.FirstOrDefault(u => u.X == x && u.Y == y);
        }

        // Zona de monstruos de una casilla: la primera del JSON que la contenga, o la zona por defecto.
        public static ZonaData ObtenerZona(int x, int y)
        {
            Cargar();
            return mapa!.Zonas.FirstOrDefault(z => z.Contiene(x, y)) ?? mapa.ZonaPorDefecto;
        }

        public static TablaMonstruosData ObtenerTablaMonstruos(string id)
        {
            Cargar();

            if (tablas.TryGetValue(id, out var tabla)) return tabla;

            throw new KeyNotFoundException($"No existe la tabla de monstruos '{id}'. Falta monstruos_{id}.json en soul anchor mapa.");
        }

        // ¿Se puede pisar esta casilla? Devuelve el evento que debe mostrar la UI si no
        // ("limite", "rio", "puente_destruido"). 'tieneFlag' dice si un flag de historia está activo.
        public static (bool Permitido, string? Evento) EvaluarCasilla(int x, int y, Func<string, bool> tieneFlag)
        {
            Cargar();

            int limite = mapa!.Limite;
            if (x < -limite || x > limite || y < -limite || y > limite)
            {
                return (false, "limite");
            }

            var puente = mapa.Puentes.FirstOrDefault(p => p.X == x && p.Y == y);
            if (puente != null)
            {
                return tieneFlag(puente.FlagRequerido) ? (true, null) : (false, puente.EventoDestruido);
            }

            var obstaculo = mapa.Obstaculos.FirstOrDefault(o => o.Contiene(x, y));
            if (obstaculo != null)
            {
                return (false, obstaculo.Evento);
            }

            return (true, null);
        }

        // Evento informativo al LLEGAR a una casilla (ej. avisar que el puente está destruido al
        // acercarse). Null si no hay nada que avisar.
        public static string? AvisoAlLlegar(int x, int y, Func<string, bool> tieneFlag)
        {
            Cargar();

            var puente = mapa!.Puentes.FirstOrDefault(p => p.AvisosEn.Any(a => a.X == x && a.Y == y));
            if (puente != null && !tieneFlag(puente.FlagRequerido))
            {
                return puente.EventoDestruido;
            }

            return null;
        }
    }
}
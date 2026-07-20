using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
// no se hacer un sistema de guardado
namespace SoulAnchor.Managers
{
    public class DatosGuardado
    {
        // Posición en el mundo
        public float UbicacionX { get; set; }
        public float UbicacionY { get; set; }

        // Inventario y Economía
        public int Oro { get; set; }
        public Dictionary<string, int>? Mochila { get; set; }

        // Progresión de Ren (El Jugador)
        public int NivelJugador { get; set; }
        public int HpJugador { get; set; }
        public int MpJugador { get; set; }
        public int AtkJugador { get; set; }
        public int DefJugador { get; set; }
        public int PuntosDisponibles { get; set; }

        // (Aquí podrías agregar variables similares para Sumire y Tetsu)
    }

    public static class SaveManager
    {
        private static readonly string rutaArchivo = "soulanchor_save.json";
        public static bool GuardarPartida(DatosGuardado datos)
        {
            try
            {
                var opciones = new JsonSerializerOptions { WriteIndented = true };
                
                string jsonString = JsonSerializer.Serialize(datos, opciones);
                
                File.WriteAllText(rutaArchivo, jsonString);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false; 
            }
        }

        public static DatosGuardado? CargarPartida()
        {
            try
            {
                // Verificamos si el archivo existe antes de intentar leerlo
                if (!File.Exists(rutaArchivo))
                {
                    return null;
                }

                string jsonString = File.ReadAllText(rutaArchivo);
                
                DatosGuardado? datosCargados = JsonSerializer.Deserialize<DatosGuardado>(jsonString);
                
                return datosCargados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la partida: {ex.Message}");
                return null;
            }
        }
    }
}
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
// no se hacer un sistema de guardado
namespace SoulAnchor.Managers
{
    // ==========================================
    // 1. EL "PAQUETE" DE DATOS (DTO)
    // ==========================================
    // Esta clase solo contiene las variables que queremos recordar.
    public class DatosGuardado
    {
        // Posición en el mundo
        public float UbicacionX { get; set; }
        public float UbicacionY { get; set; }

        // Inventario y Economía
        public int Oro { get; set; }
        public Dictionary<string, int> Mochila { get; set; }

        // Progresión de Ren (El Jugador)
        public int NivelJugador { get; set; }
        public int HpJugador { get; set; }
        public int MpJugador { get; set; }
        public int AtkJugador { get; set; }
        public int DefJugador { get; set; }
        public int PuntosDisponibles { get; set; }

        // (Aquí podrías agregar variables similares para Sumire y Tetsu)
    }

    // ==========================================
    // 2. EL GESTOR DE GUARDADO
    // ==========================================
    public static class SaveManager
    {
        // El nombre del archivo que se creará en la carpeta de tu juego
        private static readonly string rutaArchivo = "soulanchor_save.json";
        public static bool GuardarPartida(DatosGuardado datos)
        {
            try
            {
                // Opciones para que el archivo JSON sea legible por humanos (WriteIndented)
                var opciones = new JsonSerializerOptions { WriteIndented = true };
                
                // Convertimos la caja de datos en texto JSON
                string jsonString = JsonSerializer.Serialize(datos, opciones);
                
                // Escribimos el texto en el archivo
                File.WriteAllText(rutaArchivo, jsonString);
                
                return true; // Guardado exitoso
            }
            catch (Exception ex)
            {
                // Si algo sale mal (ej. falta de permisos en el disco), el juego no crashea
                Console.WriteLine($"Error al guardar: {ex.Message}");
                return false; 
            }
        }

        // Método para CARGAR (Llamar desde el Menú Principal)
        public static DatosGuardado CargarPartida()
        {
            try
            {
                // Verificamos si el archivo existe antes de intentar leerlo
                if (!File.Exists(rutaArchivo))
                {
                    return null; // No hay partida guardada
                }

                // Leemos todo el texto del archivo
                string jsonString = File.ReadAllText(rutaArchivo);
                
                // Convertimos el texto JSON de vuelta a nuestra clase DatosGuardado
                DatosGuardado datosCargados = JsonSerializer.Deserialize<DatosGuardado>(jsonString);
                
                return datosCargados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la partida: {ex.Message}");
                return null; // Archivo corrupto o error de lectura
            }
        }
    }
}
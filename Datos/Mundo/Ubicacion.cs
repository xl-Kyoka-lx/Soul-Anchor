using System;
using System.Collections.Generic;

// a la mierda no voy a hacer esto yo solo, ia haz lo tuyo
namespace SoulAnchor.Datos.Mundo
{
    public class Ubicacion
    {
        // Propiedades de la ubicación (solo lectura desde afuera)
        public string Nombre { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public string Descripcion { get; private set; }

        public Ubicacion(string nombre, float x, float y, string descripcion)
        {
            Nombre = nombre;
            X = x;
            Y = y;
            Descripcion = descripcion;
        }

        // ==========================================
        // MATEMÁTICAS DEL MAPA
        // ==========================================

        // 1. Calcula la distancia exacta entre dos puntos
        public static float CalcularDistancia(float x1, float y1, float x2, float y2)
        {
            // Fórmula Euclidiana: Raíz cuadrada de ((x2-x1)^2 + (y2-y1)^2)
            double calculo = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return (float)calculo;
        }

        // 2. Calcula la nueva posición del jugador al darle a "Avanzar" (25 km)
        // Devuelve un arreglo de dos floats: [NuevaX, NuevaY]
        public static float[] AvanzarHaciaDestino(float actualX, float actualY, Ubicacion destino)
        {
            float distanciaRestante = CalcularDistancia(actualX, actualY, destino.X, destino.Y);

            // Regla de Overshoot: Si estamos a 25 km o menos, nos "enganchamos" al destino directamente
            if (distanciaRestante <= 25f)
            {
                return new float[] { destino.X, destino.Y };
            }

            // Si estamos más lejos, calculamos la dirección (vector normalizado)
            float direccionX = (destino.X - actualX) / distanciaRestante;
            float direccionY = (destino.Y - actualY) / distanciaRestante;

            // Multiplicamos la dirección por nuestro paso de 25 km y se lo sumamos a la posición actual
            float nuevaX = actualX + (direccionX * 25f);
            float nuevaY = actualY + (direccionY * 25f);

            // Validamos los límites del continente (Máximo 300 en X e Y)
            if (nuevaX > 300f) nuevaX = 300f;
            if (nuevaY > 300f) nuevaY = 300f;; // Asumiendo que el mapa puede ir hacia negativos
            if (nuevaY < -300f) nuevaY = -300f;

            return new float[] { nuevaX, nuevaY };
        }
    }

    // ==========================================
    // REGISTRO DEL MUNDO (Data estática)
    // ==========================================
    public static class RegistroMapa
    {
        // Aquí guardamos todas las ciudades para llamarlas fácilmente desde el GameManager
        public static List<Ubicacion> ObtenerTodasLasUbicaciones()
        {
            return new List<Ubicacion>
            {
                new Ubicacion("Ciudad del Oeste", 0f, 0f, "Nodo inicial del mapa."),
                new Ubicacion("Puente", 0.7f, -0.7f, "Cruce principal del río."),
                new Ubicacion("Ciudad del Sur", 0f, -3f, "Ciudad cercana al sur de Oeste."),
                new Ubicacion("Pueblo Intermedio", 0.3f, 0.4f, "Pueblo rural cercano."),
                new Ubicacion("Ciudad del Este", 275f, -10f, "Ciudad costera oriental."),
                new Ubicacion("Ciudad del Norte (Desierto)", 0f, 200f, "Ciudad en el desierto."),
                new Ubicacion("Cueva (Bosque Norte)", 170f, 170f, "Entrada a sistema de cuevas."),
                new Ubicacion("Rift", 220f, 220f, "Zona lejana del noreste.")
            };
        }
    }
}
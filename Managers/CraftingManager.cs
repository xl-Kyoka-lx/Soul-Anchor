using System;
using SoulAnchor.Datos.Entidades;

namespace SoulAnchor.Managers
{
    public static class CraftingManager
    {

        public static bool CraftearPocionVida(Jugador jugador)
        {
            if (jugador.Mochila.ContainsKey("Hierba Orderia") && jugador.Mochila["Hierba Orderia"] >= 5)
            {
                jugador.UsarObjeto("Hierba Orderia", 5);
                jugador.AgregarObjeto("Poción de Vida", 1);
                return true;
            }
            return false;
        }

        public static bool CraftearPocionMp(Jugador jugador)
        {
            if (jugador.Mochila.ContainsKey("Hierba Eteria") && jugador.Mochila["Hierba Eteria"] >= 5)
            {
                jugador.UsarObjeto("Hierba Eteria", 5);
                jugador.AgregarObjeto("Poción de MP", 1);
                return true; 
            }
            return false;
        }

        public static bool CraftearPocionMax(Jugador jugador, string nombrePocionBase)
        {
            bool tienePocion = jugador.Mochila.ContainsKey(nombrePocionBase) && jugador.Mochila[nombrePocionBase] >= 1;
            bool tieneBaya = jugador.Mochila.ContainsKey("Baya de Xylo") && jugador.Mochila["Baya de Xylo"] >= 1;

            if (tienePocion && tieneBaya)
            {
                jugador.UsarObjeto(nombrePocionBase, 1);
                jugador.UsarObjeto("Baya de Xylo", 1);
                jugador.AgregarObjeto("Poción MAX", 1);
                return true;
            }
            
            return false;
        }
        public static bool CraftearPocionDeVeneno(Jugador jugador)
        {
            const string idMaterial = "veneno";
            const int cantidadRequerida = 3;

            if (jugador.Mochila.ContainsKey(idMaterial) && jugador.Mochila[idMaterial] >= cantidadRequerida)
            {
                jugador.UsarObjeto(idMaterial, cantidadRequerida);
                jugador.AgregarObjeto("pocion_de_veneno", 1);
                return true;
            }
            return false;
        }
    }
}
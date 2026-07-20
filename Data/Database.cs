using System;
using System.Collections.Generic;
using SoulAnchor.Datos.Entidades;

namespace SoulAnchor.Datos
{
    public static class Database
    {
       
        public static Jugador CrearRen(string nombrePersonalizado = "Ren")
        {
            // Orden: Nombre, Nivel, HPMax, MPMax, ATK, DEF
            return new Jugador(nombrePersonalizado, 1, 100, 70, 10, 8);
        }

        public static Companero CrearSumire()
        {
            return new Companero("Sumire", 1, 80, 120, 12, 6);
        }

        public static Companero CrearTetsu()
        {
            return new Companero("Tetsu", 1, 140, 20, 25, 15); 
        }

        public static Enemigo CrearLobo(int nivelGenerado)
        {

            int hpBase = 40 + (nivelGenerado * 5); 
            int atkBase = 12 + (nivelGenerado * 2);
            int defBase = 4 + nivelGenerado;
            
            // Los lobos no usaran magia, así que el MP es 0
            return new Enemigo("Lobo", nivelGenerado, hpBase, 0, atkBase, defBase); 
        }

        public static Enemigo CrearGoblin(int nivelGenerado)
        {
            int hpBase = 45 + (nivelGenerado * 5);
            int atkBase = 15 + (nivelGenerado * 2);
            int defBase = 5 + nivelGenerado;

            return new Enemigo("Goblin", nivelGenerado, hpBase, 0, atkBase, defBase);
        }

        public static Enemigo CrearEcoDelRift(Jugador jugadorActual)
        {
            return new Enemigo("Eco del Rift", jugadorActual.Nivel, jugadorActual.HpMaximo, 0, jugadorActual.Atk, jugadorActual.Def, TipoIA.EcoDelRift);
        }
    }
    
}
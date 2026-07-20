using System;
using System.Collections.Generic;
using SoulAnchor.Datos.Entidades;

namespace SoulAnchor.Datos.Entidades
{
    // Actualizamos el Enum con TODOS tus enemigos especiales
    public enum TipoIA 
    { 
        Normal, 
        Humano, 
        MagoDeSangre, 
        Eco,
        AlmaHambrienta,
        EcoDelRift,
        AlmasVinculadas,
        GolemReforzado,
        Cobarde
    }

    public class Enemigo : Personaje
    {
        public TipoIA Comportamiento { get; private set; }
        public bool EsJefe { get; private set; }
        
        public int PocionesDisponibles { get; private set; } 

        public Enemigo(string nombre, int nivel, int hpMax, int mpMax, int atk, int def, TipoIA comportamiento = TipoIA.Normal, bool esJefe = false) 
            : base(nombre, nivel, hpMax, mpMax, atk, def)
        {
            Comportamiento = comportamiento;
            EsJefe = esJefe;

            if (comportamiento == TipoIA.Humano)
            {
                PocionesDisponibles = 1;
            }
            else if (comportamiento == TipoIA.MagoDeSangre)
            {
                PocionesDisponibles = 3;
            }
            else
            {
                PocionesDisponibles = 0; 
            }
        }

        public int ObtenerXpBase()
        { 
            int xp = 50 + (Nivel * 10);
            
            if (EsJefe)
            {
                xp += (xp / 2); 
            }
            
            return xp;
        }


        public Personaje? ElegirObjetivo(List<Personaje> aliadosVivos)
        {
            if (aliadosVivos.Count == 0) return null;

            Dictionary<Personaje, int> pesos = new Dictionary<Personaje, int>();
            int pesoTotal = 0;

            foreach (var aliado in aliadosVivos)
            {
                int pesoActual = 10;

                if (aliado.Nombre == "Sumire")
                {
                    pesoActual = 60;
                }
                else if (aliado.Nombre == "Tetsu")
                {
                    pesoActual = 30;
                }

                pesos.Add(aliado, pesoActual);
                pesoTotal += pesoActual;
            }

            Random rng = new Random();
            int tirada = rng.Next(0, pesoTotal);

            int acumulado = 0;
            foreach (var candidato in pesos)
            {
                acumulado += candidato.Value;
                if (tirada < acumulado)
                {
                    return candidato.Key;
                }
            }

            return aliadosVivos[0]; 
        }
    }
}
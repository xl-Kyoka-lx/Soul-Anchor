using System;
using System.Collections.Generic;

namespace SoulAnchor.Datos.Entidades
{
    public class Jugador : Personaje
    {
        public Dictionary<string, int> Mochila { get; private set; }


        public int Oro { get; private set; }


        public Jugador(string nombre, int nivel, int hpMax, int mpMax, int atk, int def) 
            : base(nombre, nivel, hpMax, mpMax, atk, def) 
        {
            Mochila = new Dictionary<string, int>();
            Oro = 0;
        }

        public void AgregarObjeto(string nombreObjeto, int cantidad)
        {
            if (Mochila.ContainsKey(nombreObjeto))
            {
                Mochila[nombreObjeto] += cantidad;
            }
            else
            {
                Mochila.Add(nombreObjeto, cantidad);
            }
        }

        public bool UsarObjeto(string nombreObjeto, int cantidad = 1)
        {
            if (Mochila.ContainsKey(nombreObjeto) && Mochila[nombreObjeto] >= cantidad)
            {
                Mochila[nombreObjeto] -= cantidad;
                
                if (Mochila[nombreObjeto] <= 0)
                {
                    Mochila.Remove(nombreObjeto);
                }
                return true; 
            }
            return false; 
        }

        public void GanarOro(int cantidad)
        {
            Oro += cantidad;
        }

        public bool GastarOro(int cantidad)
        {
            if (Oro >= cantidad)
            {
                Oro -= cantidad;
                return true;
            }
            return false;
        }
    }
}
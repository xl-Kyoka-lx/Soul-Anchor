using System;
using SoulAnchor.Datos.Entidades;

namespace SoulAnchor.Datos.Entidades
{

    public class Companero : Personaje
    {

        public bool EsSumire { get; private set; }
        public bool EsTetsu { get; private set; }

        public Companero(string nombre, int nivel, int hpMax, int mpMax, int atk, int def) 
            : base(nombre, nivel, hpMax, mpMax, atk, def)
        {
            if (nombre == "Sumire")
            {
                EsMagoDeSangre = true;
                EsTetsu = false;
            }
            else if (nombre == "Tetsu")
            {
                EsTetsu = true;
                EsMagoDeSangre = false;
            }
        }

        public int CalcularXpArma(int xpObtenida)
        {
            if (EsMagoDeSangre)
            {
                return xpObtenida / 3; 
            }
            return xpObtenida;
        }

        public int ObtenerNivelEfectivoArma(int nivelBaseArma)
        {
            if (EsTetsu)
            {
                return nivelBaseArma + 5;
            }
            return nivelBaseArma;
        }

        public int CalcularEficaciaMagica(int valorBaseHechizo)
        {
            if (EsTetsu)
            {
                return valorBaseHechizo / 2; 
            }
            return valorBaseHechizo;
        }
    }
}
using System.Collections.Generic;

namespace SoulAnchor.Datos.Entidades
{
    public abstract class Personaje
    {
        public string Nombre { get; private set; }
        public int Nivel { get; private set; }
        public bool EsMagoDeSangre { get; protected set; }
        public int HpMaximo { get; private set; }
        public int HpActual { get; private set; }
        public int MpMaximo { get; private set; }
        public int MpActual { get; private set; }
        public int Atk { get; private set; }
        public int Def { get; private set; }

        public int PuntosDisponibles { get; private set; }
        public List<string> EstadosAlterados { get; private set; }

        public Personaje(string nombre, int nivel, int hpMax, int mpMax, int atk, int def)
        {
            Nombre = nombre;
            Nivel = nivel;
            HpMaximo = hpMax;
            HpActual = hpMax;
            MpMaximo = mpMax;
            MpActual = mpMax;
            Atk = atk;
            Def = def;
            PuntosDisponibles = 0;
            EstadosAlterados = new List<string>();
        }

        public virtual void RecibirDano(int ataqueEnemigo)
        {
            float mitigacion = 100f / (100f + this.Def); 
            int danoFinal = (int)(ataqueEnemigo * mitigacion);

            if (danoFinal < 1) danoFinal = 1; 

            HpActual -= danoFinal;
            if (HpActual < 0) HpActual = 0;
        }

        public virtual void Curar(int cantidad)
        {
            HpActual += cantidad;
            if (HpActual > HpMaximo) HpActual = HpMaximo;
        }

        public virtual void ConsumirMp(int cantidad)
        {
            MpActual -= cantidad;
            if (MpActual < 0) MpActual = 0;
        }

        public virtual void SubirNivel()
        {
            Nivel++;
            PuntosDisponibles += 5;
        }

        public bool EstaVivo()
        {
            return HpActual > 0;
        }
    }
}
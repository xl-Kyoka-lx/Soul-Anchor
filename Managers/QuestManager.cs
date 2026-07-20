using System;
using System.Collections.Generic;

namespace SoulAnchor.Managers
{

    public enum RangoAventurero { E, D, C, B, A, S }

    public class Mision
    {
        public string? Titulo { get; set; }
        public RangoAventurero Rango { get; set; }
        public int RecompensaOro { get; set; }
        public string? Descripcion { get; set; }

    }

    public class QuestManager
    {
        public RangoAventurero RangoActual { get; private set; }
        public int PuntosGremio { get; private set; }
        public Mision? MisionActiva { get; private set; }

        public QuestManager()
        {
            RangoActual = RangoAventurero.E;
            PuntosGremio = 0;
            MisionActiva = null;
        }

        public bool AceptarMision(Mision nuevaMision)
        {
            if (MisionActiva != null)
            {
                return false;
            }

            int diferenciaRango = (int)nuevaMision.Rango - (int)RangoActual;

            if (diferenciaRango >= -1 && diferenciaRango <= 1)
            {
                MisionActiva = nuevaMision;
                return true;
            }

            return false;
        }

        public void CompletarMisionActiva(out int oroGanado)
        {
            oroGanado = 0;
            if (MisionActiva == null) return;

            int diferenciaRango = (int)MisionActiva.Rango - (int)RangoActual;

            if (diferenciaRango == 1) PuntosGremio += 2;
            else if (diferenciaRango == 0) PuntosGremio += 1;

            if (diferenciaRango == -1)
            {
                oroGanado = MisionActiva.RecompensaOro / 2;
            }
            else
            {
                oroGanado = MisionActiva.RecompensaOro;
            }

            MisionActiva = null;
            VerificarAscenso();
        }

        public void FallarMisionActiva()
        {
            if (MisionActiva == null) return;

            int diferenciaRango = (int)MisionActiva.Rango - (int)RangoActual;

            if (diferenciaRango == 0) PuntosGremio -= 2;
            else if (diferenciaRango == 1) PuntosGremio -= 1;
            
            if (PuntosGremio < 0) PuntosGremio = 0;

            MisionActiva = null;
        }

        private void VerificarAscenso()
        {
            if (RangoActual == RangoAventurero.E && PuntosGremio >= 5)
            {
                RangoActual = RangoAventurero.D;
            }
            else if (RangoActual == RangoAventurero.D && PuntosGremio >= 10)
            {
                RangoActual = RangoAventurero.C;
            }
            else if (RangoActual == RangoAventurero.C && PuntosGremio >= 20)
            {
                RangoActual = RangoAventurero.B;
            }
            else if (RangoActual == RangoAventurero.B && PuntosGremio >= 35)
            {
                RangoActual = RangoAventurero.A;
            }
        }
    }
}
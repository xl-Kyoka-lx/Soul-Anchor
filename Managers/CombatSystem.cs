using System;
using System.Collections.Generic;
using System.Linq;
using SoulAnchor.Datos.Entidades;

namespace SoulAnchor.Managers
{
    public class CombatSystem
    {
        public List<Personaje> Aliados { get; private set; }
        public List<Enemigo> Enemigos { get; private set; }
        public bool CombateTerminado { get; private set; }
        public bool Victoria { get; private set; }

        public CombatSystem(List<Personaje> equipo, List<Enemigo> encuentro)
        {
            Aliados = equipo;
            Enemigos = encuentro;
            CombateTerminado = false;
            Victoria = false;
        }

        public bool EjecutarAtaqueFisico(Personaje atacante, Enemigo objetivo)
        {
            if (!atacante.EstaVivo() || !objetivo.EstaVivo()) return false;

            int danoInfligido = atacante.Atk;

            objetivo.RecibirDano(danoInfligido);

            if (!objetivo.EstaVivo())
            {
                return true;
            }

            return false; 
        }

        public bool EjecutarMagia(Personaje lanzador, Enemigo objetivo, int danoMagico, int costoMp)
        {
            if (lanzador is Companero comp && comp.EsTetsu)
            {
                danoMagico /= 2;
            }

            lanzador.ConsumirMp(costoMp);
            objetivo.RecibirDano(danoMagico);

            // (Podria ajustar esto si quiero que hechizos letales también den Multi-Kill)
            return false; 
        }

        public void VerificarEstadoCombate()
        {
            bool todosEnemigosMuertos = true;
            foreach (var enemigo in Enemigos)
            {
                if (enemigo.EstaVivo()) todosEnemigosMuertos = false;
            }

            if (todosEnemigosMuertos)
            {
                CombateTerminado = true;
                Victoria = true;
                RepartirRecompensas();
                return;
            }

            bool todosAliadosMuertos = true;
            foreach (var aliado in Aliados)
            {
                if (aliado.EstaVivo()) todosAliadosMuertos = false;
            }

            if (todosAliadosMuertos)
            {
                CombateTerminado = true;
                Victoria = false;
            }
        }

        private void RepartirRecompensas()
        {
            int totalXp = 0;
            foreach (var enemigo in Enemigos)
            {
                totalXp += enemigo.ObtenerXpBase();
            }

            if (Enemigos.Count > 1)
            {
                totalXp += (int)(totalXp * 0.2f);
            }

            var vivos = Aliados.Where(a => a.EstaVivo()).ToList();
            if (vivos.Count > 0)
            {
                int xpPorCabeza = totalXp / vivos.Count;
            }
        }

        public bool IntentarHuir()
        {
            int nivelPromedioAliados = Aliados.Sum(a => a.Nivel) / Aliados.Count;
            int nivelPromedioEnemigos = Enemigos.Sum(e => e.Nivel) / Enemigos.Count;

            Random rng = new Random();
            int probEscape = 50 + ((nivelPromedioAliados - nivelPromedioEnemigos) * 10);

            if (rng.Next(0, 100) < probEscape)
            {
                CombateTerminado = true;
                Victoria = false;
                return true;
            }
            return false;
        }
    }
}
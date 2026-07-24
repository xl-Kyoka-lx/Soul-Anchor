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

        private readonly Random rng = new Random();

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

            // Tabla de enemigos: algunos tienen evasion_pct (ej. Ladrón 20%)
            if (objetivo.IntentaEvadir(rng))
            {
                return false; // el llamador (UI) debería mostrar "¡El ataque fue esquivado!"
            }

            int danoInfligido = atacante.Atk;
            objetivo.RecibirDano(danoInfligido);

            if (!objetivo.EstaVivo())
            {
                return true;
            }

            return false; 
        }

        // 'elemento' es opcional (ej. "fuego") para que la debilidad elemental del enemigo aplique
        // (ej. Esqueleto x1.5 contra fuego). Si no se pasa, no hay bonus.
        public bool EjecutarMagia(Personaje lanzador, Enemigo objetivo, int danoMagico, int costoMp, string? elemento = null)
        {
            if (lanzador is Companero comp && comp.EsTetsu)
            {
                danoMagico /= 2;
            }

            float multiplicador = objetivo.ObtenerMultiplicadorDebilidad(elemento);
            danoMagico = (int)(danoMagico * multiplicador);

            lanzador.ConsumirMp(costoMp);
            objetivo.RecibirDano(danoMagico);

            return !objetivo.EstaVivo();
        }

        // Turno del enemigo atacando a un aliado. Aplica el estado on-hit definido en su ficha JSON
        // (ej. Araña Gigante -> Veneno 3 turnos) si acierta el chance_pct.
        public void EjecutarAtaqueEnemigo(Enemigo atacante, Personaje objetivo)
        {
            if (!atacante.EstaVivo() || !objetivo.EstaVivo()) return;

            objetivo.RecibirDano(atacante.Atk);

            var estado = atacante.IntentarAplicarEstadoOnHit(rng);
            if (estado != null && objetivo.EstaVivo())
            {
                objetivo.AplicarEstado(estado.Status, estado.DuracionTurnos, estado.DanoPorTurno);
            }
        }

        // Se llama al cierre de cada ronda completa (todos actuaron) para tickear veneno/quemadura/
        // sangrado sobre aliados y enemigos por igual. Ver Personaje.ProcesarEstados().
        public void ProcesarEstadosDeRonda()
        {
            foreach (var aliado in Aliados)
            {
                if (aliado.EstaVivo()) aliado.ProcesarEstados();
            }

            foreach (var enemigo in Enemigos)
            {
                if (enemigo.EstaVivo()) enemigo.ProcesarEstados();
            }
        }

        public void VerificarEstadoCombate()
        {
            bool todosEnemigosMuertos = Enemigos.All(e => !e.EstaVivo());

            if (todosEnemigosMuertos)
            {
                CombateTerminado = true;
                Victoria = true;
                RepartirRecompensas();
                return;
            }

            bool todosAliadosMuertos = Aliados.All(a => !a.EstaVivo());

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
                // NOTA (pre-existente en tu código original): xpPorCabeza se calcula pero nunca se
                // aplica a los personajes. Falta un Personaje.GanarXp(int) conectado a tu curva de
                // XP de la sección 6 del GDD antes de poder llamar SubirNivel() automáticamente.
            }

            var jugador = Aliados.OfType<Jugador>().FirstOrDefault();
            if (jugador == null) return;

            foreach (var enemigo in Enemigos)
            {
                foreach (var drop in enemigo.GenerarDrops(rng))
                {
                    if (drop.EsOro)
                    {
                        int oroBase = 20 + (enemigo.Nivel * 5);
                        int oroFinal = oroBase + (oroBase * drop.CantidadExtraPct / 100);
                        jugador.GanarOro(oroFinal);
                    }
                    else
                    {
                        jugador.AgregarObjeto(drop.ItemId, 1);
                    }
                }
            }
        }

        public bool IntentarHuir()
        {
            int nivelPromedioAliados = Aliados.Sum(a => a.Nivel) / Aliados.Count;
            int nivelPromedioEnemigos = Enemigos.Sum(e => e.Nivel) / Enemigos.Count;

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
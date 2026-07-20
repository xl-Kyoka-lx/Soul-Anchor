using System;
using System.Collections.Generic;
using SoulAnchor.Datos.Entidades;

namespace SoulAnchor.Managers
{
    public class BloodMagicManager
    {
        private Dictionary<string, int> cooldownsActivos;

        public BloodMagicManager()
        {
            cooldownsActivos = new Dictionary<string, int>();
        }

        public void ReducirCooldowns()
        {
            List<string> llaves = new List<string>(cooldownsActivos.Keys);

            foreach (string llave in llaves)
            {
                if (cooldownsActivos[llave] > 0)
                {
                    cooldownsActivos[llave]--;
                }
            }
        }

        public int ObtenerCooldown(Personaje lanzador, string nombreHechizo)
        {
            string llave = $"{lanzador.Nombre}_{nombreHechizo}";
            if (cooldownsActivos.ContainsKey(llave))
            {
                return cooldownsActivos[llave];
            }
            return 0;
        }

        private void ActivarCooldown(Personaje lanzador, string nombreHechizo)
        {
            string llave = $"{lanzador.Nombre}_{nombreHechizo}";
            cooldownsActivos[llave] = 3;
        }

        public bool PuedeLanzarHechizoSangre(Personaje lanzador, string nombreHechizo, int costoBase)
        {
            if (ObtenerCooldown(lanzador, nombreHechizo) > 0)
            {
                return false;
            }

            [cite_start]
            if (lanzador is Companero comp && comp.EsMagoDeSangre)
            {
                return lanzador.MpActual >= costoBase; 
            }

            return lanzador.HpActual > costoBase; 
        }

        public void EjecutarBolaDeFuego(Personaje lanzador, Enemigo objetivo)
        {
            int costoBase = 92;
            int dano = 95 + (lanzador.Nivel * 8);

            if (!PuedeLanzarHechizoSangre(lanzador, "BolaDeFuego", costoBase)) return;

            CobrarCostoSangre(lanzador, costoBase);

            objetivo.RecibirDano(dano);
            ActivarCooldown(lanzador, "BolaDeFuego");

        }

        public void EjecutarExplosionVital(Personaje lanzador, List<Personaje> aliados)
        {
            int costoBase = 118;
            int curacion = 145 + (lanzador.Nivel * 12);

            if (!PuedeLanzarHechizoSangre(lanzador, "ExplosionVital", costoBase)) return;

            CobrarCostoSangre(lanzador, costoBase);

            foreach (var aliado in aliados)
            {
                if (aliado.EstaVivo())
                {
                    aliado.Curar(curacion);
                }
            }
            
            ActivarCooldown(lanzador, "ExplosionVital");
        }

        public void EjecutarRayoRojo(Personaje lanzador, Enemigo objetivo)
        {
            int costoBase = 145;
            int dano = 220 + (lanzador.Nivel * 18);

            if (!PuedeLanzarHechizoSangre(lanzador, "RayoRojo", costoBase)) return;

            CobrarCostoSangre(lanzador, costoBase);
            objetivo.RecibirDano(dano);
            ActivarCooldown(lanzador, "RayoRojo");

        }

        private void CobrarCostoSangre(Personaje lanzador, int costo)
        {
            if (lanzador is Companero comp && comp.EsMagoDeSangre)
            {
                lanzador.ConsumirMp(costo);
            }
            else
            {

                lanzador.RecibirDano(costo); 
            }
        }
    }
}
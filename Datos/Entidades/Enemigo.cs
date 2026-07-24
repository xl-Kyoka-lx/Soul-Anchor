using System;
using System.Collections.Generic;
using SoulAnchor.Datos.Mundo;

namespace SoulAnchor.Datos.Entidades
{
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
        public EnemigoData? Datos { get; private set; }

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

        public void AsignarDatos(EnemigoData datos)
        {
            Datos = datos;
        }

        public int ObtenerXpBase()
        { 
            int xp = Datos?.XpBase ?? (50 + (Nivel * 10));
            
            if (EsJefe)
            {
                xp += (xp / 2); 
            }
            
            return xp;
        }
        public bool IntentaEvadir(Random rng)
        {
            int evasion = Datos?.Traits.EvasionPct ?? 0;
            if (evasion <= 0) return false;
            return rng.Next(0, 100) < evasion;
        }
        public float ObtenerMultiplicadorDebilidad(string? elementoAtaque)
        {
            if (Datos == null || string.IsNullOrEmpty(Datos.Traits.DebilidadElemental)) return 1.0f;
            if (string.IsNullOrEmpty(elementoAtaque)) return 1.0f;

            if (string.Equals(Datos.Traits.DebilidadElemental, elementoAtaque, StringComparison.OrdinalIgnoreCase))
            {
                return Datos.Traits.DebilidadMultiplicador;
            }

            return 1.0f;
        }

        public OnHitStatus? IntentarAplicarEstadoOnHit(Random rng)
        {
            var config = Datos?.Traits.OnHitApplyStatus;
            if (config == null) return null;

            return rng.Next(0, 100) < config.ChancePct ? config : null;
        }

        public bool DeberiaUsarPocion()
        {
            bool tienePocion = PocionesDisponibles > 0;
            bool tieneTrait = Datos?.Traits.UsaPocionCuracionBarata
                ?? (Comportamiento == TipoIA.Humano || Comportamiento == TipoIA.MagoDeSangre);
            bool hpBajo = HpMaximo > 0 && ((float)HpActual / HpMaximo) < 0.3f;

            return tienePocion && tieneTrait && hpBajo;
        }

        public void UsarPocion()
        {
            if (PocionesDisponibles <= 0) return;
            PocionesDisponibles--;
            Curar((int)(HpMaximo * 0.4f)); // poción barata genérica: ~40% HP
        }

        public List<(string ItemId, bool EsOro, int CantidadExtraPct)> GenerarDrops(Random rng)
        {
            var resultado = new List<(string, bool, int)>();
            if (Datos == null) return resultado;

            foreach (var drop in Datos.DropTable)
            {
                if (rng.Next(0, 100) < drop.ChancePct)
                {
                    bool esOro = drop.ItemId == "oro";
                    resultado.Add((drop.ItemId, esOro, drop.CantidadExtraPct));
                }
            }

            return resultado;
        }

        public Personaje? ElegirObjetivo(List<Personaje> aliadosVivos)
        {
            if (aliadosVivos.Count == 0) return null;

            Dictionary<Personaje, int> pesos = new Dictionary<Personaje, int>();
            int pesoTotal = 0;

            var aggroOverride = Datos?.Traits.AggroOverride;

            foreach (var aliado in aliadosVivos)
            {
                int pesoActual = 10;

                if (aggroOverride != null)
                {
                    if (aggroOverride.TryGetValue(aliado.Nombre, out int pesoJson))
                    {
                        pesoActual = pesoJson;
                    }
                    else if (aggroOverride.TryGetValue("default", out int pesoDefault))
                    {
                        pesoActual = pesoDefault;
                    }
                }
                else
                {
                    if (aliado.Nombre == "Sumire")
                    {
                        pesoActual = 60;
                    }
                    else if (aliado.Nombre == "Tetsu")
                    {
                        pesoActual = 30;
                    }
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
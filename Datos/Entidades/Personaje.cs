using System;
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

        // Antes era List<string>. Ahora cada estado trackea su propia duración y daño por turno,
        // necesario para que veneno/quemadura/sangrado hagan algo de verdad (GDD sección 7).
        public List<EstadoAlterado> EstadosAlterados { get; private set; }

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
            EstadosAlterados = new List<EstadoAlterado>();
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

        // Contraparte de ConsumirMp: restaura MP (ej. pociones de MP, Consumible.AplicarEfecto).
        public virtual void GanarMp(int cantidad)
        {
            MpActual += cantidad;
            if (MpActual > MpMaximo) MpActual = MpMaximo;
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

        // Aplica o refresca un estado alterado. Si ya está activo, toma la duración/daño mayor
        // en vez de apilar turnos nuevos (evita estados "infinitos" por spam del mismo ataque).
        public void AplicarEstado(string tipo, int turnos, int danoPorTurno = 0)
        {
            var existente = EstadosAlterados.Find(e => e.Tipo == tipo);
            if (existente != null)
            {
                existente.TurnosRestantes = Math.Max(existente.TurnosRestantes, turnos);
                existente.DanoPorTurno = Math.Max(existente.DanoPorTurno, danoPorTurno);
            }
            else
            {
                EstadosAlterados.Add(new EstadoAlterado(tipo, turnos, danoPorTurno));
            }
        }

        public bool TieneEstado(string tipo)
        {
            return EstadosAlterados.Exists(e => e.Tipo == tipo);
        }

        public void RemoverEstado(string tipo)
        {
            EstadosAlterados.RemoveAll(e => e.Tipo == tipo);
        }

        // Se llama una vez por ronda (ver CombatSystem.ProcesarEstadosDeRonda). Aplica el daño de
        // estados tipo veneno/quemadura/sangrado y reduce turnos restantes; elimina los que expiran.
        public virtual void ProcesarEstados()
        {
            for (int i = EstadosAlterados.Count - 1; i >= 0; i--)
            {
                var estado = EstadosAlterados[i];

                if (estado.DanoPorTurno > 0 && EstaVivo())
                {
                    HpActual -= estado.DanoPorTurno;
                    if (HpActual < 0) HpActual = 0;
                }

                estado.TurnosRestantes--;
                if (estado.TurnosRestantes <= 0)
                {
                    EstadosAlterados.RemoveAt(i);
                }
            }
        }
    }
}
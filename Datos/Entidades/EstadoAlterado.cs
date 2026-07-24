namespace SoulAnchor.Datos.Entidades
{
    public class EstadoAlterado
    {
        public string Tipo { get; set; } = "";
        public int TurnosRestantes { get; set; }
        // 0 si el estado no hace daño directo por turno
        public int DanoPorTurno { get; set; }

        public EstadoAlterado(string tipo, int turnos, int danoPorTurno = 0)
        {
            Tipo = tipo;
            TurnosRestantes = turnos;
            DanoPorTurno = danoPorTurno;
        }
    }
}

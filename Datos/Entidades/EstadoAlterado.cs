namespace SoulAnchor.Datos.Entidades
{
    // Representa una instancia activa de un estado alterado (veneno, quemadura, parálisis,
    // sangrado, confusión...) sobre un Personaje. Reemplaza el antiguo List<string> de
    // Personaje.EstadosAlterados, que no permitía trackear duración ni daño por turno (GDD sección 7).
    public class EstadoAlterado
    {
        public string Tipo { get; set; } = "";
        public int TurnosRestantes { get; set; }

        // 0 si el estado no hace daño directo por turno (ej. parálisis, confusión)
        public int DanoPorTurno { get; set; }

        public EstadoAlterado(string tipo, int turnos, int danoPorTurno = 0)
        {
            Tipo = tipo;
            TurnosRestantes = turnos;
            DanoPorTurno = danoPorTurno;
        }
    }
}

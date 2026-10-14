namespace SoulAnchor.Datos.Mundo
{
    // Un punto del mapa (ciudad, pueblo, cueva, portal...). Se construye desde mapa.json vía
    // MapaDatabase; para agregar uno nuevo no hace falta tocar C#, solo una entrada en el JSON.
    public class Ubicacion
    {
        public string Id { get; private set; }
        public string Nombre { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public string Tipo { get; private set; }

        // false = el lugar existe en el mapa pero su contenido (otro mapa, pueblo...) aún no está listo.
        public bool Implementada { get; private set; }

        public bool EsCiudad => Tipo == "ciudad";

        public Ubicacion(UbicacionData datos)
        {
            Id = datos.Id;
            Nombre = datos.Nombre;
            X = datos.X;
            Y = datos.Y;
            Tipo = datos.Tipo;
            Implementada = datos.Implementada;
        }
    }
}
using System.Text;

namespace SoulAnchor.Managers
{
    // SadConsole usa el código numérico de cada char como índice de glyph en la imagen de la fuente,
    // y la fuente por defecto (IBM_8x16) está organizada según CP437 (el codepage clásico de DOS/PC).
    // En Unicode, letras como 'ñ' o 'á' tienen códigos distintos a su posición en CP437,
    // así que hay que "traducir" el texto antes de imprimirlo o se ve un glyph incorrecto (o nada).
    public static class TextoEncoding
    {
        private static bool proveedorRegistrado = false;

        // Debe llamarse una vez al inicio del programa (antes de imprimir cualquier texto especial)
        public static void Inicializar()
        {
            if (proveedorRegistrado) return;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            proveedorRegistrado = true;
        }

        // Convierte un string normal (Unicode) a uno donde cada char representa
        // el índice CP437 correcto, listo para pasar a Surface.Print(...)
        public static string ACP437(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;

            Inicializar();

            byte[] bytes = Encoding.GetEncoding(437).GetBytes(texto);
            char[] chars = new char[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i] = (char)bytes[i];
            }

            return new string(chars);
        }
    }
}

using System;
using SadConsole;
using SadRogue.Primitives; // Librería de colores y posiciones en SadConsole
using SoulAnchor.Managers;

namespace SoulAnchor.interfaz
{


    // Heredamos de 'Console' para que esta clase sea una pantalla dibujable en SadConsole
    public class ConsoleDisplay : SadConsole.Console
    {
        private GameManager gameManager;
        private bool idiomaSeleccionado = false;
        private string idioma = "ES"; // Por defecto

        public ConsoleDisplay(int width, int height, GameManager gm) : base(width, height)
        {
            gameManager = gm;
            
            UseMouse = true; 
            UseKeyboard = true;

            // Al iniciar, dibujamos la primera pantalla
            DibujarPantallaFirstBoot();
        }

        // ==========================================
        // DIBUJO DE PANTALLAS
        // ==========================================

        public void DibujarPantallaFirstBoot()
        {
            Surface.Clear(); // Limpiamos la pantalla

            // Imprimimos el texto en las coordenadas X, Y de la pantalla de la consola
            Surface.Print(20, 10, "Selecciona tu idioma / Select your language:", Color.White);
            Surface.Print(25, 12, "1. ES (Español)", Color.Cyan);
            Surface.Print(25, 13, "2. EN (English)", Color.Cyan);
        }

        public void DibujarMenuPrincipal()
        {
            Surface.Clear();
            Surface.Print(20, 5,  "S O U L   A N C H O R", Color.Red); // El título que querías más grande
            Surface.Print(25, 10, "Nueva Partida", Color.White);
            Surface.Print(25, 12, "Cargar Partida", Color.White);
            Surface.Print(25, 14, "Ajustes", Color.White);
            Surface.Print(25, 16, "Salir", Color.White);
            // lo programaremos en el método Update leyendo la posición del mouse.
        }

        public void DibujarPreguntaPrologo()
        {
            Surface.Clear();
            Surface.Print(15, 10, "¿Quieres leer el prologo?", Color.Yellow);
            Surface.Print(25, 12, "1. Si", Color.White);
            Surface.Print(25, 13, "2. No", Color.White);
        }

        // ==========================================
        // LÓGICA DE INPUT (El bucle del juego)
        // ==========================================

        // Update se ejecuta constantemente (como en Unity)
        public override void Update(TimeSpan delta)
        {
            base.Update(delta);

            // Lógica temporal de teclado para probar rápido antes de meter el Mouse
            if (!idiomaSeleccionado)
            {
                if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.D1))
                {
                    idioma = "ES";
                    idiomaSeleccionado = true;
                    DibujarMenuPrincipal();
                }
                else if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.D2))
                {
                    idioma = "EN";
                    idiomaSeleccionado = true;
                    DibujarMenuPrincipal();
                }
            }
            else if (gameManager.EstadoActual == EstadoJuego.MenuPrincipal)
            {
                // Aquí simulamos que el jugador hizo clic en "Nueva Partida" pulsando la tecla N
                // Más adelante lo cambiaremos a eventos de Mouse.LeftClicked
                if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.N))
                {
                    DibujarPreguntaPrologo();
                }
                // Si pulsa 'S' simulamos que dijo Sí al prólogo
                else if (GameHost.Instance.Keyboard.IsKeyPressed(SadConsole.Input.Keys.S))
                {
                    // Arrancamos el juego a través del GameManager
                    gameManager.IniciarNuevaPartida("Ren");

                    Surface.Clear();
                    Surface.Print(2, 2, "=== JUEGO INICIADO ===", Color.Green);
                    Surface.Print(2, 4, $"Bienvenido {gameManager.Prota!.Nombre}. Estás en {gameManager.UbicacionActual!.Nombre}.");
                }
            }
        }
    }
}
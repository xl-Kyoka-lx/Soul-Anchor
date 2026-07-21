using SadConsole;
using SadConsole.Configuration;
using SoulAnchor.Managers;
using SoulAnchor.interfaz;

namespace SoulAnchor
{
    class Program
    {
        static void Main()
        {
            Builder configuracion = new Builder()
                .SetWindowSizeInCells(80, 25)
                .OnStart(Init)
                ;
            Game.Create(configuracion);
            Game.Instance.Run();
            Game.Instance.Dispose();
        }

        static void Init(object? sender, GameHost host)
        {
            GameManager gm = new GameManager();

            ConsoleDisplay pantallaPrincipal = new ConsoleDisplay(80, 25, gm);

            Game.Instance.Screen = pantallaPrincipal;
            Game.Instance.DestroyDefaultStartingConsole();
        }
    }
}
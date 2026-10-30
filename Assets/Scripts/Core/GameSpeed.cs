namespace Drowned.Core
{
    /// <summary>
    /// Velocidades de simulação disponíveis para o jogador (SCRUM-125).
    /// O valor numérico é o multiplicador aplicado ao tempo real.
    /// </summary>
    public enum GameSpeed
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        Fastest = 3
    }

    public static class GameSpeedExtensions
    {
        /// <summary>Multiplicador de tempo correspondente à velocidade.</summary>
        public static float Multiplier(this GameSpeed speed) => (int)speed;

        /// <summary>Rótulo curto para a HUD.</summary>
        public static string Label(this GameSpeed speed) => speed switch
        {
            GameSpeed.Paused => "II",
            GameSpeed.Normal => "1x",
            GameSpeed.Fast => "2x",
            GameSpeed.Fastest => "3x",
            _ => "?"
        };
    }
}

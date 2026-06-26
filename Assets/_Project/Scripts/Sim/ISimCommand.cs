namespace Planet.Sim
{
    /// <summary>
    /// Команда симуляции. Любой приказ игрока/ИИ (move, attack, build, spawn) — это команда,
    /// применяемая на конкретном тике. По сети в lockstep передаются именно команды,
    /// а не состояние мира. Это же — основа реплеев.
    /// </summary>
    public interface ISimCommand
    {
        void Execute(SimWorld world);
    }
}

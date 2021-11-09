using System;
using System.Collections.Generic;

namespace WDE.MapRenderer.Managers
{
    public class UpdateManager
    {
        private readonly IGameContext gameContext;
        private List<Action<float>> updates = new();

        public UpdateManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
        }

        public void Register(Action<float> update)
        {
            updates.Add(update);
        }

        public void Update(float delta)
        {
            foreach (var u in updates)
                u(delta);
        }
    }
}
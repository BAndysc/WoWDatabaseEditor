namespace WDE.MapRenderer.Managers
{
    public class UpdateManager
    {
        private List<Action<float>> updates = new();
        
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
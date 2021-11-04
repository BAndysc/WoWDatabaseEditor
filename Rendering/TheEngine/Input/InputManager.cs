namespace TheEngine.Input
{
    public class InputManager
    {
        internal Mouse mouse { get; }
        public IMouse Mouse { get; }

        internal Keyboard keyboard { get; }

        public IKeyboard Keyboard { get; }

        internal InputManager(Engine engine)
        {
            Mouse = mouse = new Mouse();
            Keyboard = keyboard = new Keyboard();
        }

        internal void Update()
        {
            mouse.Update();
        }

        public void PostUpdate()
        {
            mouse.PostUpdate();
        }
    }
}

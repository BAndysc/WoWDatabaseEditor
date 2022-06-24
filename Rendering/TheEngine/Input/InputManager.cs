using TheEngine.Interfaces;

namespace TheEngine.Input
{
    internal class InputManager : IInputManager
    {
        internal Mouse mouse { get; }
        public IMouse Mouse { get; }

        internal Keyboard keyboard { get; }

        public IKeyboard Keyboard { get; }

        internal InputManager(Engine engine)
        {
            Mouse = mouse = new Mouse(engine);
            Keyboard = keyboard = new Keyboard();
        }

        internal void Update(float deltaMs)
        {
            mouse.Update(deltaMs);
        }

        public void PostUpdate()
        {
            mouse.PostUpdate();
            keyboard.PostUpdate();
        }
    }
}

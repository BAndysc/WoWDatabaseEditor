using TheEngine.Input;

namespace TheEngine.Interfaces
{
    public interface IKeyboard
    {
        bool IsDown(Key keys);
        Vector3 GetAxis(Vector3 axis, Key positive, Key negative);
        bool JustPressed(Key keys);
    }
}

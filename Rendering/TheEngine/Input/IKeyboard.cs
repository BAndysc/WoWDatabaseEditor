using Avalonia.Input;
using TheMaths;

namespace TheEngine.Input
{
    public interface IKeyboard
    {
        bool IsDown(Key keys);
        Vector3 GetAxis(Vector3 axis, Key positive, Key negative);
        bool JustPressed(Key keys);
    }
}

namespace TheEngine.Interfaces;

public interface IInputManager
{
    IMouse Mouse { get; }
    IKeyboard Keyboard { get; }
}
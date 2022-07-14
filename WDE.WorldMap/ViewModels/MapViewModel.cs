namespace WDE.WorldMap.ViewModels;

public class MapViewModel
{
    public MapViewModel(int id, string name, string directory)
    {
        Id = id;
        Name = name;
        Directory = directory;
    }

    public int Id { get; }
    public string Name { get; }
    public string Directory { get; }

    public override string ToString() => Name;
}
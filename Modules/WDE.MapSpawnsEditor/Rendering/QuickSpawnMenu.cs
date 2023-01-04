using Avalonia.Input;
using ImGuiNET;
using WDE.Common.Database;
using WDE.Common.Services;
using IInputManager = TheEngine.Interfaces.IInputManager;

namespace WDE.MapSpawnsEditor.Rendering;

public class QuickSpawnMenu : IDisposable
{
    private struct ListItem
    {
        public ListItem(bool isCreature, uint entry, string name, string extra)
        {
            IsCreature = isCreature;
            Entry = entry;
            Name = name;
            EntryAsString = Entry.ToString();
            Header = name + " (" + EntryAsString + ")";
            Extra = extra;
        }

        public bool IsCreature { get; }
        public uint Entry { get; }
        public string EntryAsString { get; }
        public string Name { get; }
        public string Header { get; }
        public string Extra { get; }
    }
    
    private readonly IInputManager inputManager;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ModelPreviewRenderer modelPreviewRenderer;

    private List<ListItem> items = new();
    
    private bool popupOpened;
    private bool justOpened;
    private int selectedIndex = -1;
    private string searchText = "";
    
    public QuickSpawnMenu(IInputManager inputManager,
        ICachedDatabaseProvider databaseProvider,
        
        ModelPreviewRenderer modelPreviewRenderer)
    {
        this.inputManager = inputManager;
        this.databaseProvider = databaseProvider;
        this.modelPreviewRenderer = modelPreviewRenderer;
    }

    public void Dispose()
    {
        modelPreviewRenderer.Dispose();
    }

    private int prevSelectedIndex = -1;
    public void Render(float delta)
    {
        if (prevSelectedIndex != selectedIndex && selectedIndex >= 0 && selectedIndex < items.Count)
        {
            uint? displayId;
            if (items[selectedIndex].IsCreature)
                displayId = databaseProvider.GetCachedCreatureTemplate(items[selectedIndex].Entry)?.GetModel(0);
            else
                displayId = databaseProvider.GetCachedGameObjectTemplate(items[selectedIndex].Entry)?.DisplayId;
            modelPreviewRenderer.SetModel(displayId ?? 0, items[selectedIndex].IsCreature ? GuidType.Creature : GuidType.GameObject);
            prevSelectedIndex = selectedIndex;
        }
        modelPreviewRenderer.Render(delta);
    }

    public unsafe bool RenderGui(out uint spawnEntry, out bool spawnCreature)
    {
        ListItem? spawn = null;
        if (inputManager.Keyboard.IsDown(Key.Space) && !popupOpened)
        {
            popupOpened = true;
            justOpened = true;
            if (items.Count == 0)
            {
                searchText = "";
                selectedIndex = -1;
                DoSearch();
            }
            ImGui.OpenPopup("spawnmenu");
        }

        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(650, 400));
        if (ImGui.BeginPopupContextWindow("spawnmenu"))
        {
            bool scrollTo = false;
            if (justOpened)
            {
                ImGui.SetKeyboardFocusHere();
                justOpened = false;
            }
            if (ImGui.InputText("Search", ref searchText, 64, ImGuiInputTextFlags.AutoSelectAll))
            {
                DoSearch();
                if (selectedIndex >= items.Count)
                {
                    selectedIndex = 0;
                    scrollTo = true;
                }
            }
            
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                ImGui.CloseCurrentPopup();

            if (ImGui.IsKeyPressed(ImGuiKey.Enter))
            {
                if (selectedIndex >= 0 && selectedIndex < items.Count)
                    spawn = items[selectedIndex];
                else if (items.Count > 0)
                    spawn = items[0];
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow) && selectedIndex < items.Count - 1)
            {
                scrollTo = true;
                selectedIndex++;
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow) && selectedIndex > 0)
            {
                scrollTo = true;
                selectedIndex--;
            }

            var halfWidth = 650 / 2;
            ImGui.BeginChild("childL", new Vector2( halfWidth, 350));
            if (ImGui.BeginTable("list", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY, new Vector2(0, 350)))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Entry");
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Level/Type");
                ImGui.TableHeadersRow();

                ImGuiListClipper clipper = new ImGuiListClipper();
                ImGuiNative.ImGuiListClipper_Begin(&clipper, items.Count, ImGui.GetTextLineHeightWithSpacing());

                var top = selectedIndex * ImGui.GetTextLineHeightWithSpacing();
                var bottom = (selectedIndex + 1) * ImGui.GetTextLineHeightWithSpacing();
                var selectedItemInVisibleArea = top >= ImGui.GetScrollY() && bottom <= ImGui.GetScrollY() + 350;
                if (scrollTo && !selectedItemInVisibleArea)
                    ImGui.SetScrollY(selectedIndex * ImGui.GetTextLineHeightWithSpacing());
                while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
                {
                    for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                    
                        bool isSelected = selectedIndex == row;
                        if (ImGui.Selectable(items[row].EntryAsString, ref isSelected, ImGuiSelectableFlags.SpanAllColumns))
                            spawn = items[row];

                        if (ImGui.IsItemHovered() && ImGui.GetIO().MouseDelta.Length() > 0)
                            selectedIndex = row;
                    
                        ImGui.TableNextColumn();
                        ImGui.Text(items[row].Name);
                        ImGui.TableNextColumn();
                        ImGui.Text(items[row].Extra);

                        if (isSelected)
                            selectedIndex = row;
                    }
                }
            
                ImGui.EndTable();
            }
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("child_r", new Vector2(halfWidth, halfWidth));
            ImGui.Image(modelPreviewRenderer.TextureHandle, new Vector2(halfWidth, halfWidth));
            ImGui.EndChild();

            ImGui.EndPopup();
        }
        else
        {
            popupOpened = false;
        }

        spawnEntry = spawn?.Entry ?? 0;
        spawnCreature = spawn?.IsCreature ?? false;
        return spawn.HasValue;
    }

    private void DoSearch()
    {
        items.Clear();
        selectedIndex = 0;
        uint.TryParse(searchText, out var searchEntry);
        bool searchTextEmpty = string.IsNullOrEmpty(searchText);

        if (databaseProvider.GetCachedCreatureTemplates() is { } creatures)
            foreach (var creature in creatures)
            {
                if (searchTextEmpty ||
                    searchEntry == creature.Entry ||
                    creature.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                    items.Add(new ListItem(true, creature.Entry, creature.Name, creature.MinLevel == creature.MaxLevel ? creature.MinLevel.ToString() : $"{creature.MinLevel} - {creature.MaxLevel}"));
            }
        if (databaseProvider.GetCachedGameObjectTemplates() is { } gameobjects)
            foreach (var gameobject in gameobjects)
            {
                if (searchTextEmpty ||
                    searchEntry == gameobject.Entry ||
                    gameobject.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                    items.Add(new ListItem(false, gameobject.Entry, gameobject.Name, gameobject.Type.ToString()));
            }
    }
}
using System.Reflection;
using ImGuiNET;
using TheEngine.ECS;

namespace TheEngine.Utils;

public class EntityInspector
{
    private readonly Engine engine;

    public EntityInspector(Engine engine)
    {
        this.engine = engine;
    }

    private HashSet<Type> checkedTypes = new();
    private HashSet<Type> checkedManagedTypes = new();
    private Entity? inspectEntity = null;
    public unsafe void Draw()
    {
        var em = engine.entityManager;
        ImGui.Begin("Entity inspector");

        foreach (var type in em.KnownTypes)
        {
            bool isChecked = checkedTypes.Contains(type);
            if (ImGui.Checkbox(type.Name!, ref isChecked))
            {
                if (isChecked)
                    checkedTypes.Add(type);
                else
                    checkedTypes.Remove(type);
            }
        }
        foreach (var type in em.KnownManagedTypes)
        {
            bool isChecked = checkedManagedTypes.Contains(type);
            if (ImGui.Checkbox(type.Name!, ref isChecked))
            {
                if (isChecked)
                    checkedManagedTypes.Add(type);
                else
                    checkedManagedTypes.Remove(type);
            }
        }

        if (checkedTypes.Count > 0)
        {
            Archetype a = em.NewArchetype();
            foreach (var t in checkedTypes)
                a = a.WithComponentData(t);

            var itr = em.ArchetypeIterator(a);
            int total = 0;
            while (itr.MoveNext())
            {
                total += itr.Current.Length;
            }
            
            ImGui.Text("Found " + total + " total entities matching query");
            ImGui.BeginChild("items", new Vector2(400, 400), false, ImGuiWindowFlags.HorizontalScrollbar);
            ImGuiListClipper clipper = new ImGuiListClipper();
            ImGuiNative.ImGuiListClipper_Begin(&clipper, total, ImGui.GetTextLineHeightWithSpacing());
            ImGuiNative.ImGuiListClipper_Step(&clipper);

            var enumer = em.ArchetypeIterator(a);
            enumer.MoveNext();
            var chunkItr = enumer.Current;
            
            int subTotal = 0;
            while (chunkItr != null && subTotal + chunkItr.Length < clipper.DisplayStart)
            {
                subTotal += chunkItr.Length;
                if (!enumer.MoveNext())
                    break;
                chunkItr = enumer.Current;
            }
            
            var toDisplay = clipper.DisplayEnd - clipper.DisplayStart;
            int j = Math.Max(0, clipper.DisplayStart - subTotal);
            while (toDisplay > 0 && chunkItr != null)
            {
                toDisplay--;
                while (chunkItr.Length <= j)
                {
                    j = 0;
                    if (!enumer.MoveNext())
                        break;
                    chunkItr = enumer.Current;
                }

                if (j < chunkItr.Length)
                {
                    var entity = chunkItr[j];
                    if (ImGui.Button(entity.ToString()))
                    {
                        inspectEntity = entity;
                    }
                }

                j++;
            }

            ImGuiNative.ImGuiListClipper_End(&clipper);
            ImGui.EndChild();
        }

        ImGui.End();

        ImGui.Begin("Inspect entity");
        if (!inspectEntity.HasValue)
        {
            ImGui.Text("Select entity to inspect");
            ImGui.End();
            return;
        }

        var arch = em.GetArchetypeByEntity(inspectEntity.Value);
        foreach (var comp in arch.Components)
        {
            var chunkDataManager = em.GetEntityDataManagerByEntity(inspectEntity.Value);
            var obj = chunkDataManager.UnsafeDebugGetComponent(inspectEntity.Value, comp);
            if (ImGui.CollapsingHeader(comp.DataType.Name))
                DrawNestedObject(obj);
        }
        foreach (var comp in arch.ManagedComponents)
        {
            var chunkDataManager = em.GetEntityDataManagerByEntity(inspectEntity.Value);
            var obj = chunkDataManager.DebugGetManagedComponent(inspectEntity.Value, comp);
            nested = 0;
            if (ImGui.CollapsingHeader(comp.DataType.Name))
                DrawNestedObject(obj);
        }
        
        ImGui.End();
    }

    private int nested = 0;
    private void DrawNestedObject(object? o)
    {
        if (o == null)
            return;
        if (nested > 6)
        {
            ImGui.Text("Detected infinite nesting");
            return;
        }
        nested++;
        ImGui.Indent();
        if (o is string s)
            ImGui.Text(s);
        else if (o is float f)
            ImGui.Text(f.ToString());
        else if (o is int i)
            ImGui.Text(i.ToString());
        else if (o is uint ui)
            ImGui.Text(ui.ToString());
        else if (o is long l)
            ImGui.Text(l.ToString());
        else if (o is ulong ul)
            ImGui.Text(ul.ToString());
        else if (o is byte b)
            ImGui.Text(b.ToString());
        else if (o is bool bl)
            ImGui.Text(bl.ToString());
        else if (o is Vector2 v2)
            ImGui.Text($"{v2.X}, {v2.Y}");
        else if (o is Vector3 v3)
            ImGui.Text($"{v3.X}, {v3.Y}, {v3.Z}");
        else if (o is Vector4 v4)
            ImGui.Text($"{v4.X}, {v4.Y}, {v4.Z}, {v4.W}");
        else
        {
            var type = o.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                ImGui.BulletText(field.Name);
                DrawNestedObject(field.GetValue(o));
            }   
        }
        ImGui.Unindent();
        nested--;
    }
}
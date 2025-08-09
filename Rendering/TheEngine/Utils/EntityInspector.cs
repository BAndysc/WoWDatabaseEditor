using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DynamicData;
using ImGuiNET;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Inspectors;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Utils;

public interface IInspectorDrawer<T>
{
    void Draw(T component);
}

public interface IRefInspectorDrawer<T> where T : unmanaged, IComponentData
{
    void Draw(Entity entity, ref T component);
}

public class EntityInspector
{
    private readonly Engine engine;

    public EntityInspector(Engine engine)
    {
        this.engine = engine;
        RefInspectorDrawers[typeof(RenderEnabledBit)] = new RenderEnabledBitInspector(engine);
        RefInspectorDrawers[typeof(CopyParentTransform)] = new CopyParentTransformInspector(engine);
        RefInspectorDrawers[typeof(LocalToWorld)] = new LocalToWorldInspector(engine);
        RefInspectorDrawers[typeof(MeshRenderer)] = new MeshRendererInspector(engine);
        InspectorDrawers[typeof(Camera)] = new CameraInspector();
    }

    private Dictionary<Type, dynamic> InspectorDrawers { get; } = new();
    private Dictionary<Type, object> RefInspectorDrawers { get; } = new();
    private HashSet<Type> checkedTypes = new();
    private HashSet<Type> checkedManagedTypes = new();
    private Entity? inspectEntity = null;
    private Entity changeInspectedEntity = Entity.Empty;
    public unsafe void UpdateGui(float delta)
    {
#if ENGINE_RELEASE
        return;
#endif
        if (ImGui.GetIO().MouseClicked[0] &&
            engine.sceneView.IsHovered)
        {
            inspectEntity = engine.renderManager.PickSceneViewObject();
        }

        var em = engine.entityManager;
        DrawEntityHierarchyWindow();

        ImGuiEx.Begin("Inspector\0"u8);
        if (!inspectEntity.HasValue || inspectEntity.Value == Entity.Empty || !em.Exist(inspectEntity.Value))
        {
            ImGuiEx.TextUnformatted("Select entity to inspect\0"u8);
            ImGui.End();
            return;
        }

        ref var name = ref engine.entityManager.GetComponent<EntityName>(inspectEntity.Value);

        // Display entity name on the left and ID right-aligned on the same line
        ImGuiEx.TextUnformatted(name.AsSpan());
        ImGui.SameLine();
        var entityIdText = inspectEntity.Value.ToString();
        var textWidth = ImGui.CalcTextSize(entityIdText).X;
        var windowWidth = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(windowWidth - textWidth);
        ImGui.Text(entityIdText);

        var arch = em.GetArchetypeByEntity(inspectEntity.Value);
        foreach (var comp in arch.Components)
        {
            if (comp.DataType == typeof(EntityName))
            {
                continue;
            }

            var chunkDataManager = em.GetEntityDataManagerByEntity(inspectEntity.Value);
            var componentBytes = chunkDataManager.UnsafeDebugGetComponent(inspectEntity.Value, comp);
            if (ImGui.CollapsingHeader(comp.DataType.Name))
            {
                if (RefInspectorDrawers.TryGetValue(comp.DataType, out var drawer))
                {
                    var method = genericDrawHelper.MakeGenericMethod(comp.DataType);
                    method.Invoke(null, [inspectEntity.Value, new IntPtr(componentBytes), comp.SizeBytes, drawer]);
                }
                else
                {
                    var boxed = Marshal.PtrToStructure(new IntPtr(componentBytes), comp.DataType);
                    DrawNestedObject(boxed);
                }
            }
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

        if (changeInspectedEntity != Entity.Empty)
        {
            inspectEntity = changeInspectedEntity;
            changeInspectedEntity = Entity.Empty;
        }
    }

    int searchTextLength = 0;
    private byte[] searchText = new byte[64];
    private int[] searchTextKmpPartialTable = new int[64];
    private List<Entity> matchingEntities = new List<Entity>();
    private List<Entity>?[] threadLocalMatchingEntities = new List<Entity>[Environment.ProcessorCount];
    private byte[] selectableTextUtf8 =
        new byte[EntityName.MaxLength + 2 /* for ## */ + 10 /* for ID */ + 1 /* for comma */ + 10 /* for Version */ + 1 /* for null terminator */];

    private unsafe void DrawEntityHierarchyWindow()
    {
        var em = engine.entityManager;
        ImGuiEx.Begin("Entity inspector\0"u8);

        float filterButtonWidth = ImGui.CalcTextSize("Filter").X + ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ItemSpacing.X;
        float searchInputWidth = ImGui.GetContentRegionAvail().X - filterButtonWidth;
        if (searchInputWidth < 50) searchInputWidth = 50;
        ImGui.SetNextItemWidth(searchInputWidth);
        fixed(byte* namePtr = "##entity_search\0"u8)
        fixed(byte* placeholderPtr = "Search\0"u8)
        fixed (byte* searchTextPtr = searchText)
        {
            if (ImGuiNative.igInputTextWithHint(namePtr, placeholderPtr, searchTextPtr, (uint)searchText.Length, 0, null, (void*)0) > 0)
            {
                searchTextLength = 0;
                while (searchTextLength < searchText.Length && searchText[searchTextLength] != 0) searchTextLength++;

                KMP.BuildPartialMatchTable(searchText, searchTextKmpPartialTable);
            }
        }
        ImGui.SameLine();

        if (ImGui.Button("Filter"))
        {
            ImGui.OpenPopup("ComponentFilter");
        }

        // Component filter popup
        if (ImGui.BeginPopup("ComponentFilter"))
        {
            ImGui.Text("Select components to filter by:");
            ImGui.Separator();

            // Draw list of all components and whether they are selected:
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

            ImGui.EndPopup();
        }

        bool isSearchEmpty = searchTextLength == 0;
        matchingEntities.Clear();

        // draw a list of all entities
        if (checkedTypes.Count + checkedManagedTypes.Count > 0 || true)
        {
            Archetype a = em.NewArchetype();

            if (checkedTypes.Count > 0)
            {
                foreach (var t in checkedTypes)
                    a = a.WithComponentData(t);
            }
            if (checkedManagedTypes.Count > 0)
            {
                foreach (var t in checkedManagedTypes)
                    a = a.WithManagedComponentData(t);
            }

            int total = 0;
            if (isSearchEmpty)
            {
                var itr = em.ArchetypeIterator(a);
                while (itr.MoveNext())
                {
                    total += itr.Current.Length;
                }
            }
            else
            {
                a.ParallelForEachState<EntityInspector, EntityName>(this, static (ei, itr, thread, start, end, entityNames) =>
                {
                    var localList = ei.threadLocalMatchingEntities[thread] ??= new List<Entity>();
                    var searchSpan = ei.searchText.AsSpan(0, ei.searchTextLength);
                    for (int i = start; i < end; i++)
                    {
                        var entity = itr[i];
                        ref var entityName = ref entityNames[i];
                        if (KMP.KmpSearch(entityName.AsSpan(), searchSpan, ei.searchTextKmpPartialTable.AsSpan()) != -1)
                        {
                            localList.Add(entity);
                        }
                    }
                });
                foreach (var list in threadLocalMatchingEntities)
                {
                    if (list != null)
                    {
                        total += list.Count;
                        matchingEntities.AddRange(list);
                        list.Clear();
                    }
                }
            }

            ImGui.Text("Found " + total + " total entities matching query");
            // Use available content region for child size to avoid double scrollbars
            ImGui.BeginChild("items", ImGui.GetContentRegionAvail(), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            ImGuiListClipper clipper = new ImGuiListClipper();
            ImGuiNative.ImGuiListClipper_Begin(&clipper, total, ImGui.GetTextLineHeightWithSpacing());

            void DrawEntity(Entity entity)
            {
                var selectableTextUtf8Span = selectableTextUtf8.AsSpan();
                ref var entityName = ref engine.entityManager.GetComponent<EntityName>(entity);

                // Check if this entity is the currently inspected one
                bool isSelected = inspectEntity.HasValue && inspectEntity.Value == entity;

                var entityNameSpan = entityName.AsSpan();
                int offset = 0;
                entityNameSpan.CopyTo(selectableTextUtf8Span);
                offset = entityNameSpan.Length - 1; // entity name contains a null terminator, so I do -1 to override it
                "##"u8.CopyTo(selectableTextUtf8Span.Slice(offset));
                offset += 2;
                entity.Id.TryFormat(selectableTextUtf8Span.Slice(offset), out var bytesWritten);
                offset += bytesWritten;
                selectableTextUtf8Span[offset++] = (byte)',';
                entity.Version.TryFormat(selectableTextUtf8Span.Slice(offset), out bytesWritten);
                offset += bytesWritten;
                selectableTextUtf8Span[offset] = 0; // null terminator

                if (ImGuiEx.Selectable(selectableTextUtf8Span, isSelected))
                {
                    inspectEntity = entity;
                }
            }

            while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
            {
                if (!isSearchEmpty)
                {
                    for (int j = clipper.DisplayStart; j < clipper.DisplayEnd; j++)
                    {
                        if (j < matchingEntities.Count)
                        {
                            DrawEntity(matchingEntities[j]);
                        }
                    }
                }
                else
                {
                    var enumer = em.ArchetypeIterator(a);
                    if (enumer.MoveNext())
                    {
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
                                DrawEntity(entity);
                            }

                            j++;
                        }
                    }
                }
            }

            ImGuiNative.ImGuiListClipper_End(&clipper);
            ImGui.EndChild();
        }
        ImGui.End();
    }

    private static readonly MethodInfo genericDrawHelper = typeof(EntityInspector)
        .GetMethod(nameof(DrawHelper), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static unsafe void DrawHelper<T>(Entity entity, IntPtr data, int sizeInBytes, object boxedDrawer)
        where T : unmanaged, IComponentData
    {
        if (sizeInBytes < Unsafe.SizeOf<T>())
            throw new ArgumentException($"Span too small for {typeof(T)}");

        var drawer = (IRefInspectorDrawer<T>)boxedDrawer;
        ref T value = ref Unsafe.AsRef<T>(data.ToPointer());
        drawer.Draw(entity, ref value);
    }

    public void DrawInspector<T>(T model)
    {
        DrawNestedObject(model);
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
        else if (o is Entity entity)
        {
            if (ImGui.Button(entity.ToString()))
            {
                changeInspectedEntity = entity;
            }
        }
        else if (InspectorDrawers.TryGetValue(o.GetType(), out var drawer))
        {
            drawer.Draw((dynamic)o);
        }
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

    public void SceneViewRender()
    {
        if (inspectEntity.HasValue && inspectEntity.Value != Entity.Empty && engine.entityManager.Exist(inspectEntity.Value))
        {
            if (engine.entityManager.HasComponent<WorldMeshBounds>(inspectEntity.Value))
            {
                var bounds = engine.entityManager.GetComponent<WorldMeshBounds>(inspectEntity.Value);
                engine.renderManager.DrawBox(bounds.box.Minimum, bounds.box.Maximum, Vector4.One);
            }

            if (engine.entityManager.HasComponent<LocalToWorld>(inspectEntity.Value))
            {
                var l2w = engine.entityManager.GetComponent<LocalToWorld>(inspectEntity.Value);
                var upVector = Vector3.TransformNormal(Vectors.Up, l2w.Matrix);
                var forwardVector = Vector3.TransformNormal(Vectors.Forward, l2w.Matrix);
                var rightVector = Vector3.TransformNormal(Vectors.Right, l2w.Matrix);
                engine.renderManager.DrawLine(l2w.Position, l2w.Position + upVector * 3, new Vector4(1, 0, 0, 1));
                engine.renderManager.DrawLine(l2w.Position, l2w.Position + forwardVector * 3, new Vector4(0, 0, 1, 1));
                engine.renderManager.DrawLine(l2w.Position, l2w.Position + rightVector * 3, new Vector4(0, 1, 0, 1));
            }
        }

        {
            var l2w = engine.cameraManger.MainCamera.InverseViewMatrix;
            var upVector = Vector3.TransformNormal(new Vector3(0, 1, 0), l2w);
            var forwardVector = Vector3.TransformNormal(new Vector3(0, 0, -1), l2w);
            var rightVector = Vector3.TransformNormal(Vectors.Forward, l2w);
            engine.renderManager.DrawLine(engine.cameraManger.MainCamera.Transform.Position, engine.cameraManger.MainCamera.Transform.Position + upVector * 3, new Vector4(1, 0, 0, 1));
            engine.renderManager.DrawLine(engine.cameraManger.MainCamera.Transform.Position, engine.cameraManger.MainCamera.Transform.Position + forwardVector * 3, new Vector4(0, 0, 1, 1));
            engine.renderManager.DrawLine(engine.cameraManger.MainCamera.Transform.Position, engine.cameraManger.MainCamera.Transform.Position + rightVector * 3, new Vector4(0, 1, 0, 1));
        }
    }

    public void InspectEntity(Entity entity)
    {
        changeInspectedEntity = entity;
    }

    public void RegisterInspectorDrawer<T>(IInspectorDrawer<T> drawer)
    {
        InspectorDrawers[typeof(T)] = drawer;
    }
}
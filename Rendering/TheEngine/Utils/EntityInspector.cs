using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Tedd;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Inspectors;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Physics;
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
        addComponentFilter = ImGui.ImGuiTextFilter();
        RefInspectorDrawers[typeof(RenderEnabledBit)] = new RenderEnabledBitInspector(engine);
        RefInspectorDrawers[typeof(CopyParentTransform)] = new CopyParentTransformInspector(engine);
        RefInspectorDrawers[typeof(LocalToWorld)] = new LocalToWorldInspector(engine);
        RefInspectorDrawers[typeof(MeshRenderer)] = new MeshRendererInspector(engine);
        RefInspectorDrawers[typeof(AmbientOcclusion)] = new AmbientOcclusionInspector(engine);
        RefInspectorDrawers[typeof(Light)] = new LightInspector(engine);
        RefInspectorDrawers[typeof(CascadeShadowMap)] = new CascadeShadowMapInspector(engine);
        RefInspectorDrawers[typeof(Decal)] = new DecalInspector(engine);
        RefInspectorDrawers[typeof(RigidBody)] = new RigidBodyInspector();
        RefInspectorDrawers[typeof(BoxCollider)] = new BoxColliderInspector();
        RefInspectorDrawers[typeof(SphereCollider)] = new SphereColliderInspector();
        RefInspectorDrawers[typeof(CapsuleCollider)] = new CapsuleColliderInspector();
        RefInspectorDrawers[typeof(MeshCollider)] = new MeshColliderInspector(engine);
        RefInspectorDrawers[typeof(PhysicsMaterial)] = new PhysicsMaterialInspector();
        RefInspectorDrawers[typeof(Trigger)] = new TriggerInspector();
        InspectorDrawers[typeof(Camera)] = new CameraInspector();
        InspectorDrawers[typeof(UIManager.DrawTextData)] = new DrawTextDataInspector();
    }

    /// <summary>Which transform the scene-view ImGuizmo manipulates (Translate / Rotate / Scale).
    /// Set from the scene-view toolbar; only relevant in the editor scene view.</summary>
    public ImGuizmoOperation GizmoOperation { get; set; } = ImGuizmoOperation.Translate;

    private Dictionary<Type, dynamic> InspectorDrawers { get; } = new();
    private Dictionary<Type, object> RefInspectorDrawers { get; } = new();
    private HashSet<Type> checkedTypes = new();
    private HashSet<Type> checkedManagedTypes = new();
    private Entity? inspectEntity = null;
    private Entity changeInspectedEntity = Entity.Empty;
    private readonly ImGuiTextFilterPtr addComponentFilter;
    private List<(Type Type, bool IsManaged, bool IsArray)> addComponentCandidates = new();
    private string? addComponentError;
    public unsafe void UpdateGui(float delta)
    {
#if ENGINE_RELEASE
        return;
#endif
        // Only pick when the click landed on the scene image itself - not on a toolbar widget
        // (IsAnyItemHovered) and not on the transform gizmo of the current selection (IsOver/IsUsing).
        // Otherwise clicking a button or dragging the gizmo over empty space would pick nothing and
        // clear the selection - which also removes the very gizmo you were interacting with.
        bool overGizmo = inspectEntity.HasValue && (ImGuizmo.IsOver() || ImGuizmo.IsUsing());
        bool overViewCube = ImGuizmo.IsViewManipulateHovered() || ImGuizmo.IsUsingViewManipulate();
        if (ImGui.GetIO().MouseClicked[0] &&
            engine.sceneView.IsHovered &&
            !ImGui.IsAnyItemHovered() &&
            !overGizmo &&
            !overViewCube)
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
            if (comp.IsArray)
            {
                ComponentArrayIndex arrayOffset = *(ComponentArrayIndex*)componentBytes;
                var arrayData = chunkDataManager.UnsafeDebugGetArrayBytesComponent(inspectEntity.Value, comp);
                for (int i = 0; i < arrayOffset.Count; ++i)
                {
                    var elementBytes = arrayData + (i + arrayOffset.Index) * comp.SizeBytes;
                    if (ImGui.CollapsingHeader(comp.DataType.Name + " [" + i + "]"))
                    {
                        if (RefInspectorDrawers.TryGetValue(comp.DataType, out var drawer))
                        {
                            var method = genericDrawHelper.MakeGenericMethod(comp.DataType);
                            method.Invoke(null, [inspectEntity.Value, new IntPtr(elementBytes), comp.SizeBytes, drawer]);
                        }
                        else
                        {
                            var boxed = Marshal.PtrToStructure(new IntPtr(elementBytes), comp.DataType);
                            DrawNestedObject(boxed);
                        }
                    }
                }
            }
            else
            {
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
        }
        foreach (var comp in arch.ManagedComponents)
        {
            var chunkDataManager = em.GetEntityDataManagerByEntity(inspectEntity.Value);
            var obj = chunkDataManager.DebugGetManagedComponent(inspectEntity.Value, comp);
            nested = 0;
            if (ImGui.CollapsingHeader(comp.DataType.Name))
                DrawNestedObject(obj);
        }

        ImGui.Separator();
        if (ImGui.Button("Add Component"u8))
        {
            addComponentCandidates = GetAllComponentTypes(em);
            addComponentFilter.Clear();
            addComponentError = null;
            ImGui.OpenPopup("AddComponentPopup"u8);
        }
        DrawAddComponentPopup(em, arch, inspectEntity.Value);

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
    private int searchRevision = 0;
    private List<Entity> matchingEntities = new List<Entity>();
    private List<Entity>?[] threadLocalMatchingEntities = new List<Entity>[Environment.ProcessorCount];
    private byte[] selectableTextUtf8 =
        new byte[EntityName.MaxLength + 2 /* for ## */ + 10 /* for ID */ + 1 /* for comma */ + 10 /* for Version */ + 1 /* for null terminator */];

    private bool hierarchyTreeMode = true;
    private HashSet<Entity> expandedNodes = new();
    private int expandRevision = 0;
    // passMask: bit c set means an ancestor at gutter column c has a later sibling, so a
    // plain pass-through guide line must be drawn through that column for this row.
    private List<(Entity entity, int depth, ulong passMask, bool hasNextSibling)> flatRows = new();
    private HashSet<Entity> searchVisible = new();
    private int flattenedForStructuralVersion = -1;
    private int flattenedForExpandRevision = -1;
    private int flattenedForSearchRevision = -1;

    private unsafe void DrawEntityHierarchyWindow()
    {
        var em = engine.entityManager;
        ImGui.Begin("Entity inspector\0"u8);

        float filterButtonWidth = hierarchyTreeMode ? 0 : ImGui.CalcTextSize("Filter"u8).X + ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ItemSpacing.X;
        float searchInputWidth = ImGui.GetContentRegionAvail().X - filterButtonWidth;
        if (searchInputWidth < 50) searchInputWidth = 50;
        ImGui.SetNextItemWidth(searchInputWidth);
        fixed(byte* namePtr = "##entity_search\0"u8)
        fixed(byte* placeholderPtr = "Search\0"u8)
        fixed (byte* searchTextPtr = searchText)
        {
            if (ImGui.InputTextWithHint(namePtr, placeholderPtr, searchTextPtr, (uint)searchText.Length, 0, null, (void*)0))
            {
                searchTextLength = 0;
                while (searchTextLength < searchText.Length && searchText[searchTextLength] != 0) searchTextLength++;

                KMP.BuildPartialMatchTable(searchText, searchTextKmpPartialTable);
                searchRevision++;
            }
        }

        if (!hierarchyTreeMode)
        {
            ImGui.SameLine();
            if (ImGui.Button("Filter"u8))
            {
                ImGui.OpenPopup("ComponentFilter"u8);
            }

            if (ImGui.BeginPopup("ComponentFilter"u8))
            {
                ImGui.Text("Select components to filter by:"u8);
                ImGui.Separator();

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
        }

        ImGui.Checkbox("Tree view"u8, ref hierarchyTreeMode);

        if (hierarchyTreeMode)
        {
            DrawHierarchyTree(em);
            ImGui.End();
            return;
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

            Span<byte> foundNEntitiesText = stackalloc byte[128];
            var temp = foundNEntitiesText;
            temp.MoveWrite("Found "u8);
            temp.MoveWriteAsDecimal(total);
            temp.MoveWrite(" total entities matching query\0"u8);

            ImGui.Text(foundNEntitiesText);
            // Use available content region for child size to avoid double scrollbars
            ImGui.BeginChild("items"u8, ImGui.GetContentRegionAvail(), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
            ImGuiListClipper clipper = new ImGuiListClipper();
            clipper.Begin(total, ImGui.GetTextLineHeightWithSpacing());

            while (clipper.Step())
            {
                if (!isSearchEmpty)
                {
                    for (int j = clipper.DisplayStart; j < clipper.DisplayEnd; j++)
                    {
                        if (j < matchingEntities.Count)
                        {
                            DrawEntitySelectable(matchingEntities[j]);
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
                                DrawEntitySelectable(entity);
                            }

                            j++;
                        }
                    }
                }
            }

            clipper.End();
            ImGui.EndChild();
        }
        ImGui.End();
    }

    private unsafe void DrawEntitySelectable(Entity entity, float rowHeight = 0)
    {
        var em = engine.entityManager;
        var selectableTextUtf8Span = selectableTextUtf8.AsSpan();
        ref var entityName = ref em.GetComponent<EntityName>(entity);

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

        if (ImGuiEx.Selectable(selectableTextUtf8Span, isSelected, new System.Numerics.Vector2(0, rowHeight)))
        {
            inspectEntity = entity;
        }
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            var hasTransform = em.NewArchetype().WithComponentData<LocalToWorld>();
            if (em.Is(entity, hasTransform))
            {
                var pos = em.GetComponent<LocalToWorld>(entity).Position;
                engine.sceneView.FocusOn(pos, 2f);
            }
        }
    }

    // Re-flattens the visible hierarchy rows (respecting collapse state, or - while searching -
    // auto-expanding the path to every match) into `flatRows`. Only called when something that
    // affects the visible set actually changed (structural edit, expand/collapse, search text),
    // so steady-state drawing never re-walks the tree.
    private void RebuildHierarchyFlatten(EntityManager em)
    {
        flatRows.Clear();
        bool searching = searchTextLength > 0;

        if (searching)
        {
            searchVisible.Clear();
            foreach (var list in threadLocalMatchingEntities)
                list?.Clear();

            var baseArchetype = em.NewArchetype();
            baseArchetype.ParallelForEachState<EntityInspector, EntityName>(this, static (ei, itr, thread, start, end, names) =>
            {
                var localList = ei.threadLocalMatchingEntities[thread] ??= new List<Entity>();
                var searchSpan = ei.searchText.AsSpan(0, ei.searchTextLength);
                for (int i = start; i < end; i++)
                {
                    ref var entityName = ref names[i];
                    if (KMP.KmpSearch(entityName.AsSpan(), searchSpan, ei.searchTextKmpPartialTable.AsSpan()) != -1)
                        localList.Add(itr[i]);
                }
            });

            foreach (var list in threadLocalMatchingEntities)
            {
                if (list == null)
                    continue;
                foreach (var match in list)
                {
                    // Mark the match and every ancestor up to the root as visible.
                    var cur = match;
                    while (cur != Entity.Empty && searchVisible.Add(cur))
                        cur = em.GetComponent<Relationship>(cur).Parent;
                }
                list.Clear();
            }
        }

        // Next sibling that will actually appear in the flattened rows: while searching that
        // skips siblings the filter hid, so the elbow shape only reflects what's drawn.
        Entity NextVisibleSibling(Entity e)
        {
            var next = em.GetComponent<Relationship>(e).NextSibling;
            while (next != Entity.Empty && searching && !searchVisible.Contains(next))
                next = em.GetComponent<Relationship>(next).NextSibling;
            return next;
        }

        void Walk(Entity node, int depth, ulong passMask, bool hasNextSibling)
        {
            flatRows.Add((node, depth, passMask, hasNextSibling));

            // While searching, every visible node is auto-expanded along the path to its
            // matches; otherwise respect the user's manual expand/collapse state.
            if (!searching && !expandedNodes.Contains(node))
                return;

            // Roots have no gutter column of their own, so children of a root start with a
            // clean mask regardless of how many other roots exist.
            ulong childPassMask = depth == 0 ? 0UL : passMask | (hasNextSibling ? 1UL << (depth - 1) : 0UL);

            var child = em.GetComponent<Relationship>(node).FirstChild;
            while (child != Entity.Empty && searching && !searchVisible.Contains(child))
                child = em.GetComponent<Relationship>(child).NextSibling;

            while (child != Entity.Empty)
            {
                var next = NextVisibleSibling(child);
                Walk(child, depth + 1, childPassMask, next != Entity.Empty);
                child = next;
            }
        }

        var root = em.HierarchyFirstRoot;
        while (root != Entity.Empty && searching && !searchVisible.Contains(root))
            root = em.GetComponent<Relationship>(root).NextSibling;
        while (root != Entity.Empty)
        {
            var next = NextVisibleSibling(root);
            Walk(root, 0, 0, next != Entity.Empty);
            root = next;
        }
    }

    private void DrawHierarchyTree(EntityManager em)
    {
        if (flattenedForStructuralVersion != em.StructuralVersion ||
            flattenedForExpandRevision != expandRevision ||
            flattenedForSearchRevision != searchRevision)
        {
            RebuildHierarchyFlatten(em);
            flattenedForStructuralVersion = em.StructuralVersion;
            flattenedForExpandRevision = expandRevision;
            flattenedForSearchRevision = searchRevision;
        }

        Span<byte> foundNEntitiesText = stackalloc byte[128];
        var temp = foundNEntitiesText;
        temp.MoveWrite("Found "u8);
        temp.MoveWriteAsDecimal(flatRows.Count);
        temp.MoveWrite(" visible entities\0"u8);
        ImGui.Text(foundNEntitiesText);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(0, 0));
        ImGui.BeginChild("items"u8, ImGui.GetContentRegionAvail(), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        var arrowSize = ImGui.GetFrameHeight();
        var indentStep = arrowSize;
        var baseX = ImGui.GetCursorPosX();
        // the arrow/dummy box (sized to GetFrameHeight) is the tallest item in the row, so
        // that - not the plain text line height - is the real per-row pitch the cursor advances
        // by; using the text-only height here under-sized the background rect/guide lines.
        var lineHeight = arrowSize;
        var drawList = ImGui.GetWindowDrawList();
        var rowBgX0 = ImGui.GetWindowPos().X;
        var rowBgWidth = ImGui.GetWindowWidth();
        var altRowColor = ImGui.GetColorU32(ImGuiCol.TableRowBgAlt);
        var guideColor = ImGui.GetColorU32(ImGuiCol.TreeLines);
        var guideThickness = ImGui.GetStyle().TreeLinesSize;

        ImGuiListClipper clipper = new ImGuiListClipper();
        clipper.Begin(flatRows.Count, lineHeight);
        while (clipper.Step())
        {
            for (int j = clipper.DisplayStart; j < clipper.DisplayEnd; j++)
            {
                if (j >= flatRows.Count)
                    break;

                var (entity, depth, passMask, hasNextSibling) = flatRows[j];
                var rowTop = ImGui.GetCursorScreenPos().Y;

                if ((j & 1) != 0)
                    drawList.AddRectFilled(new System.Numerics.Vector2(rowBgX0, rowTop), new System.Numerics.Vector2(rowBgX0 + rowBgWidth, rowTop + lineHeight), altRowColor);

                ImGui.PushID((int)entity.Id);

                // Full-row hit target drawn first (label hidden, just hover/select highlight)
                // so clicking or hovering anywhere on the row - including the arrow/indent gutter -
                // selects it, rather than only the text portion to the right of the arrow.
                bool isSelected = inspectEntity.HasValue && inspectEntity.Value == entity;
                ImGui.SetCursorPosX(baseX);
                ImGui.SetNextItemAllowOverlap();
                if (ImGuiEx.Selectable("##rowsel\0"u8, isSelected, new System.Numerics.Vector2(rowBgWidth, lineHeight)))
                    inspectEntity = entity;
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    var hasTransform = em.NewArchetype().WithComponentData<LocalToWorld>();
                    if (em.Is(entity, hasTransform))
                    {
                        var pos = em.GetComponent<LocalToWorld>(entity).Position;
                        engine.sceneView.FocusOn(pos, 2f);
                    }
                }
                ImGui.SameLine();
                ImGui.SetCursorPosX(baseX + depth * indentStep);
                var arrowPos = ImGui.GetCursorScreenPos();

                if (depth > 0)
                {
                    var rowMid = rowTop + lineHeight * 0.5f;
                    var rowBottom = rowTop + lineHeight;

                    // plain pass-through guides for ancestors that still have more children below
                    for (int c = 0; c < depth - 1; c++)
                    {
                        if ((passMask & (1UL << c)) == 0)
                            continue;
                        var x = arrowPos.X - (depth - c) * indentStep + arrowSize * 0.5f;
                        drawList.AddLine(new System.Numerics.Vector2(x, rowTop), new System.Numerics.Vector2(x, rowBottom), guideColor, guideThickness);
                    }

                    // this row's own elbow: down from the parent row, across into the arrow box,
                    // and onward to the next sibling if there is one (a tee instead of an L)
                    var elbowX = arrowPos.X - indentStep + arrowSize * 0.5f;
                    drawList.AddLine(new System.Numerics.Vector2(elbowX, rowTop), new System.Numerics.Vector2(elbowX, rowMid), guideColor, guideThickness);
                    drawList.AddLine(new System.Numerics.Vector2(elbowX, rowMid), new System.Numerics.Vector2(arrowPos.X, rowMid), guideColor, guideThickness);
                    if (hasNextSibling)
                        drawList.AddLine(new System.Numerics.Vector2(elbowX, rowMid), new System.Numerics.Vector2(elbowX, rowBottom), guideColor, guideThickness);
                }

                bool hasChildren = em.GetComponent<Relationship>(entity).FirstChild != Entity.Empty;
                if (hasChildren)
                {
                    bool isExpanded = expandedNodes.Contains(entity);
                    ImGui.InvisibleButton("##exp"u8, new System.Numerics.Vector2(arrowSize, arrowSize));
                    var col = ImGui.GetColorU32(ImGui.IsItemHovered() ? ImGuiCol.Text : ImGuiCol.TextDisabled);
                    // RenderArrow draws a FontSize-tall glyph from its top-left corner; since our
                    // box is the taller FrameHeight, nudge down so the glyph sits centered in it.
                    var arrowGlyphPos = arrowPos + new System.Numerics.Vector2(0, (arrowSize - ImGui.GetFontSize()) * 0.5f);
                    ImGuiP.RenderArrow(drawList, arrowGlyphPos, col, isExpanded ? ImGuiDir.Down : ImGuiDir.Right);
                    if (ImGui.IsItemClicked())
                    {
                        if (isExpanded)
                            expandedNodes.Remove(entity);
                        else
                            expandedNodes.Add(entity);
                        expandRevision++;
                    }
                }
                else
                {
                    ImGui.Dummy(new System.Numerics.Vector2(arrowSize, arrowSize));
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGuiEx.TextUnformatted(em.GetComponent<EntityName>(entity).AsSpan());
                ImGui.PopID();
            }
        }
        clipper.End();
        ImGui.EndChild();
        ImGui.PopStyleVar(2);
    }

    private void DrawAddComponentPopup(EntityManager em, Archetype arch, Entity entity)
    {
        if (!ImGui.BeginPopup("AddComponentPopup"u8))
            return;

        addComponentFilter.Draw("##addComponentSearch");
        ImGui.Separator();

        if (addComponentError != null)
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.4f, 0.4f, 1), addComponentError);

        ImGui.BeginChild("AddComponentList"u8, new System.Numerics.Vector2(350, 300));
        foreach (var candidate in addComponentCandidates)
        {
            if (!addComponentFilter.PassFilter(candidate.Type.Name))
                continue;

            if (!candidate.IsArray && EntityAlreadyHas(arch, candidate.Type, candidate.IsManaged))
                continue;

            if (ImGui.Selectable(candidate.Type.Name))
            {
                try
                {
                    AddComponentToEntity(em, entity, candidate.Type, candidate.IsManaged);
                    addComponentError = null;
                    ImGui.CloseCurrentPopup();
                }
                catch (Exception e)
                {
                    addComponentError = $"Failed to add {candidate.Type.Name}: {e.InnerException?.Message ?? e.Message}";
                }
            }
        }
        ImGui.EndChild();

        ImGui.EndPopup();
    }

    private static bool EntityAlreadyHas(Archetype arch, Type type, bool isManaged)
    {
        if (isManaged)
        {
            foreach (var comp in arch.ManagedComponents)
                if (comp.DataType == type)
                    return true;
        }
        else
        {
            foreach (var comp in arch.Components)
                if (comp.DataType == type)
                    return true;
        }
        return false;
    }

    private static List<(Type Type, bool IsManaged, bool IsArray)> GetAllComponentTypes(EntityManager em)
    {
        var list = new List<(Type Type, bool IsManaged, bool IsArray)>();
        foreach (var t in em.KnownTypes)
            list.Add((t, false, t.GetCustomAttributes(typeof(ArrayComponentAttribute), false).Length > 0));
        foreach (var t in em.KnownManagedTypes)
            list.Add((t, true, false));
        list.Sort((a, b) => string.CompareOrdinal(a.Type.Name, b.Type.Name));
        return list;
    }

    private static void AddComponentToEntity(EntityManager em, Entity entity, Type type, bool isManaged)
    {
        if (isManaged)
            em.ManagedTypeData(type).AddDefault(em, entity);
        else
            em.TypeData(type).AddDefault(em, entity);
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
            ImGui.Text("Detected infinite nesting"u8);
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
    static Span<float> AsSpan(ref Matrix4x4 matrix)
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<Matrix4x4, float>(ref matrix),
            16
        );
    }
    static Matrix4x4 FromSpan(Span<float> matrix)
    {
        return Unsafe.As<float, Matrix4x4>(ref matrix[0]);
    }
    public unsafe void SceneViewRender()
    {
        {
            var l2w = engine.cameraManger.MainCamera.InverseViewMatrix;
            var upVector = Vector3.TransformNormal(new Vector3(0, 1, 0), l2w);
            var forwardVector = Vector3.TransformNormal(new Vector3(0, 0, -1), l2w);
            var rightVector = Vector3.TransformNormal(Vectors.Forward, l2w);
            engine.renderManager.DrawLine(engine.cameraManger.MainCamera.Transform.Position, engine.cameraManger.MainCamera.Transform.Position + upVector * 3, new Vector4(1, 0, 0, 1));
            engine.renderManager.DrawLine(engine.cameraManger.MainCamera.Transform.Position, engine.cameraManger.MainCamera.Transform.Position + forwardVector * 3, new Vector4(0, 0, 1, 1));
            engine.renderManager.DrawLine(engine.cameraManger.MainCamera.Transform.Position, engine.cameraManger.MainCamera.Transform.Position + rightVector * 3, new Vector4(0, 1, 0, 1));
        }

        // Selection bounds. Must be queued here (render phase, scene target active) rather than in
        // DrawSceneInspector (GUI phase): lines queued in the GUI phase get flushed into the game
        // view by the first flush of the next render, so they'd show up in the wrong view.
        if (inspectEntity.HasValue && inspectEntity.Value != Entity.Empty && engine.entityManager.Exist(inspectEntity.Value)
            && engine.entityManager.HasComponent<WorldMeshBounds>(inspectEntity.Value))
        {
            var bounds = engine.entityManager.GetComponent<WorldMeshBounds>(inspectEntity.Value);
            engine.renderManager.DrawBox(bounds.box.Minimum, bounds.box.Maximum, Vector4.One);
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

    public unsafe void DrawSceneInspector(RectangleF viewRect)
    {
        if (inspectEntity.HasValue && inspectEntity.Value != Entity.Empty && engine.entityManager.Exist(inspectEntity.Value))
        {
            // Note: the selection bounds box is drawn in SceneViewRender (render phase), not here -
            // see the comment there. This method only drives the ImGuizmo overlay, which is GUI-phase.
            if (engine.entityManager.HasComponent<LocalToWorld>(inspectEntity.Value))
            {
                ref var l2w = ref engine.entityManager.GetComponent<LocalToWorld>(inspectEntity.Value);

                var view = engine.cameraManger.SceneViewCamera.ViewMatrix;
                var proj = engine.cameraManger.SceneViewCamera.ProjectionMatrix;
                var local = l2w.Matrix;
                fixed (float* viewPtr = AsSpan(ref view))
                {
                    fixed (float* projPtr = AsSpan(ref proj))
                    {
                        fixed (float* localPtr = AsSpan(ref local))
                        {
                            // ImGuizmo only scales correctly in Local space; translate/rotate use World.
                            var mode = GizmoOperation == ImGuizmoOperation.Scale
                                ? ImGuizmoMode.Local
                                : ImGuizmoMode.World;
                            if (ImGuizmo.Manipulate(
                                    viewPtr,
                                    projPtr,
                                    GizmoOperation,
                                    mode,
                                    localPtr
                                ))
                            {
                                l2w.Matrix = local;
                                engine.physicsManager.SyncDynamicPoseFromEditor(inspectEntity.Value, l2w.Position, l2w.Rotation);
                            }
                        }
                    }
                }
            }
        }
    }
}
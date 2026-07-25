using Hexa.NET.ImGui;
using TheEngine.Interfaces;
using WDE.MpqReader.Structures;
using System;
using System.Collections.Generic;
using System.Numerics;
using TheEngine.Utils;

namespace WDE.MapRenderer.Inspectors;

public class M2Inspector : IInspectorDrawer<M2>
{
    private static readonly string[] flagNames = Enum.GetNames(typeof(M2Flags));
    private static readonly Array flagValues = Enum.GetValues(typeof(M2Flags));
    private static readonly System.Text.StringBuilder previewBuilder = new();

    private static readonly Dictionary<int, string> BoneNames = new Dictionary<int, string>()
    {
        [00] = "ArmL",
        [01] = "ArmR",
        [02] = "ShoulderL",
        [03] = "ShoulderR",
        [04] = "SpineLow",
        [05] = "Waist",
        [06] = "Head",
        [07] = "Jaw",
        [08] = "IndexFingerR",
        [09] = "MiddleFingerR",
        [10] = "PinkyFingerR",
        [11] = "RingFingerR",
        [12] = "ThumbR",
        [13] = "IndexFingerL",
        [14] = "MiddleFingerL",
        [15] = "PinkyFingerL",
        [16] = "RingFingerL",
        [17] = "ThumbL",
        [18] = "$BTH",
        [19] = "$CSR",
        [20] = "$CSL",
        [21] = "_Breath",
        [22] = "_Name",
        [23] = "_NameMount",
        [24] = "$CHD",
        [25] = "$CCH",
        [26] = "Root",
        [27] = "Wheel1",
        [28] = "Wheel2",
        [29] = "Wheel3",
        [30] = "Wheel4",
        [31] = "Wheel5",
        [32] = "Wheel6",
        [33] = "Wheel7",
        [34] = "Wheel8",
        [35] = "FaceAttenuation",
        [36] = "EXP_C1_Cape1",
        [37] = "EXP_C1_Cape2",
        [38] = "EXP_C1_Cape3",
        [39] = "EXP_C1_Cape4",
        [40] = "EXP_C1_Cape5",
        [41] = "EXP_C1_Tail1",
        [42] = "EXP_C1_Tail2",
        [43] = "EXP_C1_LoinBk1",
        [44] = "EXP_C1_LoinBk2",
        [45] = "EXP_C1_LoinBk3",
        [48] = "EXP_C1_Spine2",
        [49] = "EXP_C1_Neck1",
        [50] = "EXP_C1_Neck2",
        [51] = "EXP_C1_Pelvis1",
        [52] = "Buckle",
        [53] = "Chest",
        [54] = "Main",
        [55] = "EXP_R1_Leg1Twist1",
        [56] = "EXP_L1_Leg1Twist1",
        [57] = "EXP_R1_Leg2Twist1",
        [58] = "EXP_L1_Leg2Twist1",
        [59] = "FootL",
        [60] = "FootR",
        [61] = "ElbowR",
        [62] = "ElbowL",
        [63] = "EXP_L1_Shield1",
        [64] = "HandR",
        [65] = "HandL",
        [66] = "WeaponR",
        [67] = "WeaponL",
        [68] = "SpellHandL",
        [69] = "SpellHandR",
        [70] = "EXP_R1_Leg1Twist3",
        [71] = "EXP_L1_Leg1Twist3",
        [72] = "EXP_R1_Arm1Twist2",
        [73] = "EXP_L1_Arm1Twist2",
        [74] = "EXP_R1_Arm1Twist3",
        [75] = "EXP_L1_Arm1Twist3",
        [76] = "EXP_R1_Arm2Twist2",
        [77] = "EXP_L1_Arm2Twist2",
        [78] = "EXP_R1_Arm2Twist3",
        [79] = "EXP_L1_Arm2Twist3",
        [80] = "ForearmR",
        [81] = "ForearmL",
        [82] = "EXP_R1_Arm1Twist1",
        [83] = "EXP_L1_Arm1Twist1",
        [84] = "EXP_R1_Arm2Twist1",
        [85] = "EXP_L1_Arm2Twist1",
        [86] = "EXP_R1_FingerClawA1",
        [87] = "EXP_R1_FingerClawB1",
        [88] = "EXP_L1_FingerClawA1",
        [89] = "EXP_L1_FingerClawB1",
        [190] = "_BackCloak",
        [191] = "face_hair_00_M_JNT",
        [192] = "face_beard_00_M_JNT",
        [193] = "face_cheek_02_L_SkinPoint",
        [194] = "face_cheek_02_R_SkinPoint",
        [195] = "face_eyeCornerIn_00_L_SkinPoint",
        [196] = "face_eyeCornerIn_00_R_SkinPoint",
        [197] = "face_eyeCornerOut_00_L_SkinPoint",
        [198] = "face_eyeCornerOut_00_R_SkinPoint",
        [199] = "face_eyebrow_00_L_SkinPoint",
        [200] = "face_eyebrow_00_M_SkinPoint",
        [201] = "face_eyebrow_00_R_SkinPoint",
        [202] = "face_eyebrow_01_L_SkinPoint",
        [203] = "face_eyebrow_01_R_SkinPoint",
        [204] = "face_eyebrow_02_L_SkinPoint",
        [205] = "face_eyebrow_02_R_SkinPoint",
        [206] = "face_eyebrow_03_L_SkinPoint",
        [207] = "face_eyebrow_03_R_SkinPoint",
        [208] = "face_eyelidBot_00_L_SkinPoint",
        [209] = "face_eyelidBot_00_R_SkinPoint",
        [210] = "face_eyelidBot_01_L_SkinPoint",
        [211] = "face_eyelidBot_01_R_SkinPoint",
        [212] = "face_eyelidBot_02_L_SkinPoint",
        [213] = "face_eyelidBot_02_R_SkinPoint",
        [214] = "face_eyelidTop_00_L_SkinPoint",
        [215] = "face_eyelidTop_00_R_SkinPoint",
        [216] = "face_eyelidTop_01_L_SkinPoint",
        [217] = "face_eyelidTop_01_R_SkinPoint",
        [218] = "face_eyelidTop_02_L_SkinPoint",
        [219] = "face_eyelidTop_02_R_SkinPoint",
        [220] = "face_noseBridge_00_L_SkinPoint",
        [221] = "face_noseBridge_00_R_SkinPoint",
        [222] = "face_overEye_00_L_SkinPoint",
        [223] = "face_overEye_00_R_SkinPoint",
        [224] = "face_overOuterEye_00_L_SkinPoint",
        [225] = "face_overOuterEye_00_R_SkinPoint",
        [226] = "face_underEye_00_L_SkinPoint",
        [227] = "face_underEye_00_R_SkinPoint",
        [228] = "face_cheekPuff_00_L_SkinPoint",
        [229] = "face_cheekPuff_00_R_SkinPoint",
        [230] = "face_cheek_00_L_SkinPoint",
        [231] = "face_cheek_00_R_SkinPoint",
        [232] = "face_cheek_01_L_SkinPoint",
        [233] = "face_cheek_01_R_SkinPoint",
        [234] = "face_chin_00_L_SkinPoint",
        [235] = "face_chin_00_M_SkinPoint",
        [236] = "face_chin_00_R_SkinPoint",
        [237] = "face_ear_00_L_SkinPoint",
        [238] = "face_ear_00_R_SkinPoint",
        [239] = "face_jaw_01_M_SkinPoint",
        [240] = "face_jowl_00_L_SkinPoint",
        [241] = "face_jowl_00_R_SkinPoint",
        [242] = "face_jowl_01_L_SkinPoint",
        [243] = "face_jowl_01_R_SkinPoint",
        [244] = "face_lipBotBase_00_M_SkinPoint",
        [245] = "face_lipTopBase_00_M_SkinPoint",
        [246] = "face_mouthCorner_00_L_SkinPoint",
        [247] = "face_mouthCorner_00_R_SkinPoint",
        [248] = "face_mouthCurlBot_00_M_SkinPoint",
        [249] = "face_mouthCurlTop_00_M_SkinPoint",
        [250] = "face_mouth_00_M_SkinPoint",
        [251] = "face_nasLab_00_L_SkinPoint",
        [252] = "face_nasLab_00_R_SkinPoint",
        [253] = "face_nasLab_01_L_SkinPoint",
        [254] = "face_nasLab_01_R_SkinPoint",
        [255] = "face_noseBase_00_M_SkinPoint",
        [256] = "face_sneerDriver_00_L_SkinPoint",
        [257] = "face_sneerDriver_00_R_SkinPoint",
        [258] = "face_sneerLower_00_L_SkinPoint",
        [259] = "face_sneerLower_00_R_SkinPoint",
        [260] = "face_sneer_00_L_SkinPoint",
        [261] = "face_sneer_00_R_SkinPoint",
        [262] = "face_teethBot_00_M_SkinPoint",
        [263] = "face_teethTop_00_M_SkinPoint",
        [264] = "face_tongue_00_M_SkinPoint",
        [265] = "root_main_00_M_SkinPoint",
        [266] = "spine_mainBendy_00_M_SkinPoint",
        [267] = "clavicle_main_00_L_SkinPoint",
        [268] = "arm_shoulderBendy_00_L_SkinPoint",
        [269] = "hand_main_00_L_JNT",
        [270] = "hand_index_00_L_SkinPoint",
        [271] = "hand_main_00_L_SkinPoint",
        [272] = "hand_ring_00_L_SkinPoint",
        [273] = "hand_pinky_00_L_SkinPoint",
        [274] = "hand_thumb_00_L_SkinPoint",
        [275] = "clavicle_main_00_R_SkinPoint",
        [276] = "arm_shoulderBendy_00_R_SkinPoint",
        [277] = "hand_main_00_R_JNT",
        [278] = "hand_main_00_R_SkinPoint",
        [279] = "hand_middle_00_R_SkinPoint",
        [280] = "hand_ring_00_R_SkinPoint",
        [281] = "hand_pinky_00_R_SkinPoint",
        [282] = "hand_thumb_00_R_SkinPoint",
        [283] = "head_main_00_M_SkinPoint",
        [284] = "face_jaw_00_M_SkinPoint",
        [285] = "EXP_L1_Eye1",
        [286] = "EXP_R1_Eye1",
        [287] = "EXP_L1_EyeLid1",
        [288] = "EXP_R1_EyeLid1",
        [289] = "EXP_L1_EyeLid2",
        [290] = "EXP_R1_EyeLid2",
        [292] = "EXP_L1_WingArm1Twist1",
        [293] = "EXP_R1_WingArm1Twist1",
        [296] = "waterfall_top_sound",
        [297] = "waterfall_bottom_sound",
    };

    private readonly M2AnimationWindowManager animationWindowManager = new();

    public void Draw(M2 m2)
    {
        // Dynamically calculate max label width
        float maxLabelWidth = 0;
        string[] labels = { "Name", "Flags", "Global Loops", "Sequences", "SequenceIdToAnimationId", "Bones", "BoneIndicesById", "Vertices", "Colors", "Textures", "TextureWeights", "TextureTransforms" };
        foreach (var label in labels)
        {
            float width = ImGui.CalcTextSize(label).X;
            if (width > maxLabelWidth)
                maxLabelWidth = width;
        }
        maxLabelWidth += ImGui.GetStyle().ItemSpacing.X + 8;

        ImGui.Columns(2, "m2_columns", false);
        ImGui.SetColumnWidth(0, maxLabelWidth);

        // Name row
        var name = m2.name.AsSpan();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Name");
        ImGui.NextColumn();
        ImGui.TextUnformatted(new string(m2.name.AsSpan()));
        ImGui.NextColumn();

        // Flags row (generic flags renderer)
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Flags");
        ImGui.NextColumn();
        DrawFlags(m2.global_flags, typeof(M2Flags));
        ImGui.NextColumn();

        // Global Loops row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Global Loops");
        ImGui.NextColumn();
        DrawArray<uint>(
            m2.global_loops,
            "Global Loops",
            (v, _) => v.ToString(),
            null
        );
        ImGui.NextColumn();

        // Sequences row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Sequences");
        ImGui.NextColumn();
        DrawArray<M2Sequence>(
            m2.sequences,
            "Sequences",
            (seq, _) => ((M2AnimationType)seq.id).ToString(),
            DrawM2Sequence
        );
        ImGui.NextColumn();

        // SequenceIdToAnimationId row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("SequenceIdToAnimationId");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.sequenceIdToAnimationId,
            "SequenceIdToAnimationId",
            (v, index) => ((M2AnimationType)index) + " -> " + v.ToString(),
            null
        );
        ImGui.NextColumn();

        // Bones row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Bones");
        ImGui.NextColumn();
        DrawArray<M2CompBone>(
            m2.bones.array,
            "Bones",
            (bone, _) => GetBoneName(bone.key_bone_id),
            DrawM2CompBone
        );
        ImGui.NextColumn();

        // BoneIndicesById row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("BoneIndicesById");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.boneIndicesById,
            "BoneIndicesById",
            (v, index) => GetBoneName(index) + " -> " + v,
            null
        );
        ImGui.NextColumn();

        // Vertices row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Vertices");
        ImGui.NextColumn();
        DrawArray<M2Vertex>(
            m2.vertices,
            "Vertices",
            (v, _) => $"({v.pos.X:0.##}, {v.pos.Y:0.##}, {v.pos.Z:0.##})",
            DrawM2Vertex
        );
        ImGui.NextColumn();

        // Colors row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Colors");
        ImGui.NextColumn();
        bool hasColors = m2.colors.Length > 0;
        string buttonLabel = $"Open Colors ({m2.colors.Length})##open_colors";
        if (!hasColors)
            ImGui.BeginDisabled();
        if (ImGui.Button(buttonLabel) && hasColors)
        {
            animationWindowManager.OpenWindow(new M2ColorWindow(m2.colors, m2.sequenceIdToAnimationId, name.ToString()));
        }
        if (!hasColors)
            ImGui.EndDisabled();
        ImGui.NextColumn();

        // TextureWeights row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("TextureWeights");
        ImGui.NextColumn();
        bool hasWeights = m2.textureWeights.Length > 0;
        string buttonLabelWeights = $"Open TextureWeights ({m2.textureWeights.Length})##open_textureweights";
        if (!hasWeights)
            ImGui.BeginDisabled();
        if (ImGui.Button(buttonLabelWeights) && hasWeights)
        {
            animationWindowManager.OpenWindow(new M2TextureWeightWindow(m2.textureWeights, m2.sequenceIdToAnimationId, name.ToString()));
        }
        if (!hasWeights)
            ImGui.EndDisabled();
        ImGui.NextColumn();

        // TextureTransforms row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("TextureTransforms");
        ImGui.NextColumn();
        bool hasTransforms = m2.texture_transforms.Length > 0;
        string buttonLabelTransforms = $"Open TextureTransforms ({m2.texture_transforms.Length})##open_texturetransforms";
        if (!hasTransforms)
            ImGui.BeginDisabled();
        if (ImGui.Button(buttonLabelTransforms) && hasTransforms)
        {
            animationWindowManager.OpenWindow(new M2TextureTransformWindow(m2.texture_transforms, m2.sequenceIdToAnimationId, name.ToString()));
        }
        if (!hasTransforms)
            ImGui.EndDisabled();
        ImGui.NextColumn();

        // TextureIndicesById row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("TextureIndicesById");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.textureIndicesById,
            "TextureIndicesById",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        // Textures row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Textures");
        ImGui.NextColumn();
        DrawArray<M2Texture>(
            m2.textures,
            "Textures",
            (t, _) => $"{t.type} {(t.filename.Length > 1 ? new string(t.filename.AsSpan()) : "")}",
            DrawM2TextureTooltip
        );
        ImGui.NextColumn();

        // Materials row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Materials");
        ImGui.NextColumn();
        DrawArray<M2Material>(
            m2.materials,
            "Materials",
            (mat, _) => $"{mat.blending_mode} | {mat.flags}",
            DrawM2MaterialTooltip
        );
        ImGui.NextColumn();

        // Bone Lookup Table row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Bone Lookup Table");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.bone_lookup_table,
            "Bone Lookup Table",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        // Texture Lookup Table row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Texture Lookup Table");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.texture_lookup_table,
            "Texture Lookup Table",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        // Tex Unit Lookup Table row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Tex Unit Lookup Table");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.tex_unit_lookup_table,
            "Tex Unit Lookup Table",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        // Transparency Lookup Table row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Transparency Lookup Table");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.transparency_lookup_table,
            "Transparency Lookup Table",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        // Texture Transforms Lookup Table row
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Texture Transforms Lookup Table");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.texture_transforms_lookup_table,
            "Texture Transforms Lookup Table",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        // Attachments Information
        ImGui.Separator();
        ImGui.TextUnformatted("Attachments");
        ImGui.NextColumn();
        DrawArray<M2Attachment>(
            m2.attachments,
            "Attachments",
            (att, _) => $"{att.id} (Bone: {att.bone})",
            DrawM2AttachmentTooltip
        );
        ImGui.NextColumn();

        // Attachment Indices By Id
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Attachment Indices By Id");
        ImGui.NextColumn();
        DrawArray<short>(
            m2.attachmentIndicesById,
            "Attachment Indices By Id",
            (id, index) => $"[{index}] = {id}",
            null
        );
        ImGui.NextColumn();

        ImGui.Columns(1);

        // Bounding and Collision Information
        ImGui.Separator();
        ImGui.TextUnformatted("Geometry Information");
        ImGui.Separator();

        if (ImGui.BeginTable("geometry_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            // Bounding Box
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Bounding Box");
            ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"Min: ({m2.bounding_box.min.X:F3}, {m2.bounding_box.min.Y:F3}, {m2.bounding_box.min.Z:F3})");
            ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted($"Max: ({m2.bounding_box.max.X:F3}, {m2.bounding_box.max.Y:F3}, {m2.bounding_box.max.Z:F3})");

            // Bounding Sphere
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Bounding Sphere Radius");
            ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{m2.bounding_sphere_radius:F3}");
            ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted("Used for detail doodad draw distance");

            // Collision Box
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Collision Box");
            ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"Min: ({m2.collision_box.min.X:F3}, {m2.collision_box.min.Y:F3}, {m2.collision_box.min.Z:F3})");
            ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted($"Max: ({m2.collision_box.max.X:F3}, {m2.collision_box.max.Y:F3}, {m2.collision_box.max.Z:F3})");

            // Collision Sphere
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Collision Sphere Radius");
            ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{m2.collision_sphere_radius:F3}");
            ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted("Used for collision detection");

            ImGui.EndTable();
        }

        // Draw all open animation windows
        animationWindowManager.Draw();
    }

    private string GetBoneName(int boneKeyBoneId)
    {
        if (BoneNames.TryGetValue(boneKeyBoneId, out var value))
        {
            return $"{value} ({boneKeyBoneId})";
        }
        return boneKeyBoneId.ToString();
    }

    private Dictionary<Type, (string[] names, Array values)> enumCache = new();

    // Generic flags renderer for any enum with [Flags]
    private void DrawFlags<T>(T flags, Type enumType) where T : Enum
    {
        // Use cache for names/values
        if (!enumCache.TryGetValue(enumType, out var cache))
        {
            cache = (Enum.GetNames(enumType), Enum.GetValues(enumType));
            enumCache[enumType] = cache;
        }
        var flagNames = cache.names;
        var flagValues = cache.values;

        previewBuilder.Clear();
        bool any = false;
        long flagsValue = Convert.ToInt64(flags);
        for (int i = 0; i < flagValues.Length; ++i)
        {
            var value = flagValues.GetValue(i)!;
            long longValue = Convert.ToInt64(value);
            if (longValue == 0)
                continue;
            if ((flagsValue & longValue) != 0)
            {
                if (any)
                    previewBuilder.Append(", ");
                previewBuilder.Append(flagNames[i]);
                any = true;
            }
        }
        string preview = any ? previewBuilder.ToString() : "<None>";

        if (ImGui.BeginCombo("##flags_" + enumType.Name, preview, ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.HeightLargest))
        {
            for (int i = 0; i < flagValues.Length; ++i)
            {
                var value = flagValues.GetValue(i)!;
                long longValue = Convert.ToInt64(value);
                if (longValue == 0)
                    continue; // skip None/0 flag

                bool isSet = (flagsValue & longValue) != 0;
                ImGui.Selectable(flagNames[i], isSet, ImGuiSelectableFlags.Disabled);
            }
            ImGui.EndCombo();
        }
    }

    // Generic array renderer with optional tooltip popup, now with clipping for performance
    private unsafe void DrawArray<T>(
        M2Array<T> array,
        string id,
        Func<T, int, string> renderLabel,
        Action<T>? renderTooltip)
    {
        const float maxHeight = 90.0f;
        const float itemHeight = 22.0f;
        float height = Math.Min(array.Length * itemHeight + ImGui.GetStyle().FramePadding.Y * 2, maxHeight);

        string childId = $"##array_{id}";

        if (ImGui.BeginChild(childId, new System.Numerics.Vector2(0, height), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.HorizontalScrollbar))
        {
            var clipper = new ImGuiListClipper();
            clipper.Begin(array.Length, itemHeight);
            while (clipper.Step())
            {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; ++i)
                {
                    var value = array[i];
                    string label = $"{i}: {renderLabel(value, i)}";

                    ImGui.Selectable(label, false, ImGuiSelectableFlags.SpanAllColumns);

                    if (renderTooltip != null && ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        renderTooltip(value);
                        ImGui.EndTooltip();
                    }
                }
            }
            clipper.End();
        }
        ImGui.EndChild();
    }

    // Helper to render M2Sequence in two columns
    private void DrawM2Sequence(M2Sequence seq)
    {
        ImGui.BeginTable("seq_table", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("id");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{(M2AnimationType)seq.id} ({seq.id})");
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("variationIndex");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.variationIndex.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("duration");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.duration.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("movespeed");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.movespeed.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("flags");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.flags.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("frequency");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.frequency.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("replay");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{seq.replay.minimum} - {seq.replay.maximum}");
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("blendTime");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.blendTime.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("bounds.extent.min");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.bounds.extent.min.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("bounds.extent.max");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.bounds.extent.max.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("bounds.radius");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.bounds.radius.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("variationNext");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.variationNext.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("aliasNext");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(seq.aliasNext.ToString());
        ImGui.EndTable();
    }

    // Helper to render M2CompBone in two columns, skipping translation, rotation, scale
    private void DrawM2CompBone(M2CompBone bone)
    {
        ImGui.BeginTable("bone_table", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("key_bone_id");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(GetBoneName(bone.key_bone_id));
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("flags");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(bone.flags.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("parent_bone");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(bone.parent_bone.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("submesh_id");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(bone.submesh_id.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("boneNameCRC");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(bone.boneNameCRC.ToString("X8"));
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("pivot");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(bone.pivot.ToString());
        ImGui.EndTable();
    }

    // Helper to render M2Vertex in two columns
    private void DrawM2Vertex(M2Vertex v)
    {
        ImGui.BeginTable("vertex_table", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("pos");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{v.pos.X:0.###}, {v.pos.Y:0.###}, {v.pos.Z:0.###}");
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("bone_weights");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(v.bone_weights.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("bone_indices");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(v.bone_indices.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("normal");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{v.normal.X:0.###}, {v.normal.Y:0.###}, {v.normal.Z:0.###}");
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("tex_coord1");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{v.tex_coord1.X:0.###}, {v.tex_coord1.Y:0.###}");
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("tex_coord2");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"{v.tex_coord2.X:0.###}, {v.tex_coord2.Y:0.###}");
        ImGui.EndTable();
    }

    private void DrawM2TextureTooltip(M2Texture t)
    {
        ImGui.BeginTable("texture_table", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Type");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(t.type.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Flags");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(t.flags.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Filename");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(t.filename.Length > 1 ? new string(t.filename.AsSpan()) : "");
        ImGui.EndTable();
    }

    private void DrawM2MaterialTooltip(M2Material mat)
    {
        ImGui.BeginTable("material_table", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Blending Mode");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(mat.blending_mode.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Flags");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(mat.flags.ToString());
        ImGui.EndTable();
    }

    private void DrawM2AttachmentTooltip(M2Attachment att)
    {
        ImGui.BeginTable("attachment_table", 2, ImGuiTableFlags.SizingFixedFit);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Attachment Type");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(att.id.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Bone");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(att.bone.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Unknown");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(att.unknown.ToString());
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0); ImGui.TextUnformatted("Position");
        ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted($"({att.position.X:F3}, {att.position.Y:F3}, {att.position.Z:F3})");
        ImGui.EndTable();
    }
}

// --- Animation Window Manager ---

public class M2AnimationWindowManager
{
    private readonly List<M2AnimationWindowBase> openWindows = new();

    public void OpenWindow(M2AnimationWindowBase window)
    {
        if (!openWindows.Exists(w => w.WindowName == window.WindowName))
            openWindows.Add(window);
    }

    public void Draw()
    {
        for (int i = openWindows.Count - 1; i >= 0; --i)
        {
            if (!openWindows[i].Draw())
                openWindows.RemoveAt(i);
        }
    }
}

// --- Animation Window Base ---

public abstract class M2AnimationWindowBase
{
    protected int selectedAnimIdx = 0;
    protected readonly List<(int index, string label)> animOptions = new();
    public string WindowName { get; }

    protected M2AnimationWindowBase(M2Array<short> sequences, string name, string windowType)
    {
        WindowName = $"{windowType}: {name}###{windowType}_{name}";
        for (int i = 0; i < sequences.Length; ++i)
        {
            var seq = sequences[i];
            if (seq != -1)
                animOptions.Add((i, $"{(M2AnimationType)i} ({i})"));
        }
    }

    public bool Draw()
    {
        bool open = true;
        if (ImGui.Begin(WindowName, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            // Animation picker
            if (ImGui.BeginCombo("Animation", animOptions.Count > 0 ? animOptions[selectedAnimIdx].label : "<none>"))
            {
                for (int i = 0; i < animOptions.Count; ++i)
                {
                    bool isSelected = selectedAnimIdx == i;
                    if (ImGui.Selectable(animOptions[i].label, isSelected))
                        selectedAnimIdx = i;
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            DrawContent();
        }
        ImGui.End();
        return open;
    }

    protected abstract void DrawContent();
}

// --- Color Window ---

public class M2ColorWindow : M2AnimationWindowBase
{
    private readonly M2Array<M2Color> colors;

    public M2ColorWindow(M2Array<M2Color> colors, M2Array<short> sequences, string name)
        : base(sequences, name, "M2 Colors")
    {
        this.colors = colors;
    }

    protected override void DrawContent()
    {
        for (int colorIdx = 0; colorIdx < colors.Length; ++colorIdx)
        {
            var color = colors[colorIdx];
            ImGui.Separator();
            ImGui.TextUnformatted($"Color {colorIdx}");
            DrawColorTimeline(color, selectedAnimIdx);
        }
    }

    private void DrawColorTimeline(M2Color color, int animIdx)
    {
        // Get color timeline data if available
        M2Array<uint> timestamps = default;
        M2Array<Vector3> values = default;
        int colorCount = 0;
        bool hasColor = animIdx < color.color.values.Length && animIdx < color.color.timestamps.Length;
        if (hasColor)
        {
            timestamps = color.color.Timestamps(animIdx);
            values = color.color.Values(animIdx);
            colorCount = Math.Min(timestamps.Length, values.Length);
        }

        // Get alpha timeline data if available
        M2Array<uint> alphaTimestamps = default;
        M2Array<Fixed16> alphaValues = default;
        int alphaCount = 0;
        bool hasAlpha = animIdx < color.alpha.values.Length && animIdx < color.alpha.timestamps.Length;
        if (hasAlpha)
        {
            alphaTimestamps = color.alpha.Timestamps(animIdx);
            alphaValues = color.alpha.Values(animIdx);
            alphaCount = Math.Min(alphaTimestamps.Length, alphaValues.Length);
        }

        // If both are missing, nothing to show
        if (!hasColor && !hasAlpha)
        {
            ImGui.TextUnformatted("No color or alpha keys.");
            return;
        }

        Vector2 plotSize = new Vector2(400, 120);

        // Find min/max X for scaling
        float minX = float.MaxValue, maxX = float.MinValue;
        if (hasColor && colorCount > 0)
        {
            for (int i = 0; i < colorCount; ++i)
            {
                float x = timestamps[i];
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }
        if (hasAlpha && alphaCount > 0)
        {
            for (int i = 0; i < alphaCount; ++i)
            {
                float x = alphaTimestamps[i];
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }
        if (minX == float.MaxValue || maxX == float.MinValue)
        {
            minX = 0;
            maxX = 1;
        }
        float rangeX = Math.Max(1e-5f, maxX - minX);

        // Prepare all curves for plotting
        List<(Vector4 color, float[] xs, float[] ys, string label)> curves = new();

        // RGB
        if (hasColor && colorCount > 0)
        {
            float[] xs = new float[colorCount];
            float[] r = new float[colorCount];
            float[] g = new float[colorCount];
            float[] b = new float[colorCount];
            for (int i = 0; i < colorCount; ++i)
            {
                xs[i] = (timestamps[i] - minX) / rangeX;
                var v = values[i];
                r[i] = v.X;
                g[i] = v.Y;
                b[i] = v.Z;
            }
            curves.Add((new Vector4(1, 0, 0, 1), xs, r, "R"));
            curves.Add((new Vector4(0, 1, 0, 1), xs, g, "G"));
            curves.Add((new Vector4(0, 0, 1, 1), xs, b, "B"));
        }
        else
        {
            float[] xs = { 0, 1 };
            float[] ones = { 1, 1 };
            curves.Add((new Vector4(1, 0, 0, 1), xs, ones, "R"));
            curves.Add((new Vector4(0, 1, 0, 1), xs, ones, "G"));
            curves.Add((new Vector4(0, 0, 1, 1), xs, ones, "B"));
        }

        // Alpha
        if (hasAlpha && alphaCount > 0)
        {
            float[] xs = new float[alphaCount];
            float[] a = new float[alphaCount];
            for (int i = 0; i < alphaCount; ++i)
            {
                xs[i] = (alphaTimestamps[i] - minX) / rangeX;
                a[i] = alphaValues[i].Value;
            }
            curves.Add((new Vector4(1, 1, 1, 1), xs, a, "A"));
        }
        else
        {
            float[] xs = { 0, 1 };
            float[] ones = { 1, 1 };
            curves.Add((new Vector4(1, 1, 1, 1), xs, ones, "A"));
        }

        // Draw all curves on the same chart
        var drawList = ImGui.GetWindowDrawList();
        Vector2 basePos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(plotSize);

        // Draw axes
        drawList.AddRect(basePos, basePos + plotSize, ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1)));

        // Plot each curve
        foreach (var (col, xs, ys, label) in curves)
        {
            if (xs.Length < 2)
                continue;
            for (int i = 0; i < xs.Length - 1; ++i)
            {
                float x0 = basePos.X + xs[i] * plotSize.X;
                float y0 = basePos.Y + (1 - ys[i]) * plotSize.Y;
                float x1 = basePos.X + xs[i + 1] * plotSize.X;
                float y1 = basePos.Y + (1 - ys[i + 1]) * plotSize.Y;
                drawList.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), ImGui.GetColorU32(col), 2.0f);
            }
        }

        // Draw legend
        float legendX = basePos.X + 5;
        float legendY = basePos.Y + 5;
        foreach (var (col, _, _, label) in curves)
        {
            drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(col));
            drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), label);
            legendY += 16;
        }
    }
}

// --- Texture Weight Window ---

public class M2TextureWeightWindow : M2AnimationWindowBase
{
    private readonly M2Array<M2TextureWeight> weights;

    public M2TextureWeightWindow(M2Array<M2TextureWeight> weights, M2Array<short> sequences, string name)
        : base(sequences, name, "M2 TextureWeights")
    {
        this.weights = weights;
    }

    protected override void DrawContent()
    {
        for (int idx = 0; idx < weights.Length; ++idx)
        {
            var weight = weights[idx];
            ImGui.Separator();
            ImGui.TextUnformatted($"TextureWeight {idx}");
            DrawWeightTimeline(weight, selectedAnimIdx);
        }
    }

    private void DrawWeightTimeline(M2TextureWeight weight, int animIdx)
    {
        // Defensive: check bounds
        bool hasWeight = animIdx < weight.weight.values.Length && animIdx < weight.weight.timestamps.Length;
        M2Array<uint> timestamps = default;
        M2Array<Fixed16> values = default;
        int count = 0;
        if (hasWeight)
        {
            timestamps = weight.weight.Timestamps(animIdx);
            values = weight.weight.Values(animIdx);
            count = Math.Min(timestamps.Length, values.Length);
        }

        if (!hasWeight || count == 0)
        {
            ImGui.TextUnformatted("No weight keys.");
            return;
        }

        // Find min/max X for scaling
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < count; ++i)
        {
            float x = timestamps[i];
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }
        if (minX == float.MaxValue || maxX == float.MinValue)
        {
            minX = 0;
            maxX = 1;
        }
        float rangeX = Math.Max(1e-5f, maxX - minX);

        // Prepare curve
        float[] xs = new float[count];
        float[] ys = new float[count];
        for (int i = 0; i < count; ++i)
        {
            xs[i] = (timestamps[i] - minX) / rangeX;
            ys[i] = values[i].Value;
        }

        // Draw curve
        Vector2 plotSize = new Vector2(400, 120);
        var drawList = ImGui.GetWindowDrawList();
        Vector2 basePos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(plotSize);

        // Draw axes
        drawList.AddRect(basePos, basePos + plotSize, ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1)));

        // Plot curve (yellow)
        if (xs.Length >= 2)
        {
            for (int i = 0; i < xs.Length - 1; ++i)
            {
                float x0 = basePos.X + xs[i] * plotSize.X;
                float y0 = basePos.Y + (1 - ys[i]) * plotSize.Y;
                float x1 = basePos.X + xs[i + 1] * plotSize.X;
                float y1 = basePos.Y + (1 - ys[i + 1]) * plotSize.Y;
                drawList.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), ImGui.GetColorU32(new Vector4(1, 1, 0, 1)), 2.0f);
            }
        }

        // Draw legend
        float legendX = basePos.X + 5;
        float legendY = basePos.Y + 5;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(1, 1, 0, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "Weight");
    }
}

// --- Texture Transform Window ---

public class M2TextureTransformWindow : M2AnimationWindowBase
{
    private readonly M2Array<M2TextureTransform> transforms;
    private int selectedTrack = 0;
    private static readonly string[] trackNames = { "Translation", "Rotation", "Scale" };

    public M2TextureTransformWindow(M2Array<M2TextureTransform> transforms, M2Array<short> sequences, string name)
        : base(sequences, name, "M2 TextureTransforms")
    {
        this.transforms = transforms;
    }

    protected override void DrawContent()
    {
        // Track selector
        ImGui.TextUnformatted("Track:");
        ImGui.SameLine();
        if (ImGui.BeginCombo("##track", trackNames[selectedTrack]))
        {
            for (int i = 0; i < trackNames.Length; ++i)
            {
                bool isSelected = selectedTrack == i;
                if (ImGui.Selectable(trackNames[i], isSelected))
                    selectedTrack = i;
                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        for (int idx = 0; idx < transforms.Length; ++idx)
        {
            var transform = transforms[idx];
            ImGui.Separator();
            ImGui.TextUnformatted($"TextureTransform {idx}");
            DrawTransformTimeline(transform, selectedAnimIdx, selectedTrack);
        }
    }

    private void DrawTransformTimeline(M2TextureTransform transform, int animIdx, int track)
    {
        // Select which track to show
        if (track == 0)
            DrawVector3Track(transform.translation, animIdx, "Translation");
        else if (track == 1)
            DrawQuaternionTrack(transform.rotation, animIdx, "Rotation");
        else
            DrawVector3Track(transform.scaling, animIdx, "Scale");
    }

    private void DrawVector3Track(MutableM2Track<Vector3> track, int animIdx, string label)
    {
        bool hasTrack = animIdx < track.Length;
        if (!hasTrack)
        {
            ImGui.TextUnformatted($"No {label.ToLower()} keys.");
            return;
        }

        var timestamps = track.Timestamps(animIdx);
        var values = track.Values(animIdx);
        int count = Math.Min(timestamps.Length, values.Length);

        if (count == 0)
        {
            ImGui.TextUnformatted($"No {label.ToLower()} keys.");
            return;
        }

        // Find min/max X for scaling
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < count; ++i)
        {
            float x_ = timestamps[i];
            if (x_ < minX) minX = x_;
            if (x_ > maxX) maxX = x_;
        }
        if (minX == float.MaxValue || maxX == float.MinValue)
        {
            minX = 0;
            maxX = 1;
        }
        float rangeX = Math.Max(1e-5f, maxX - minX);

        // Prepare curves
        float[] xs = new float[count];
        float[] x = new float[count];
        float[] y = new float[count];
        float[] z = new float[count];
        for (int i = 0; i < count; ++i)
        {
            xs[i] = (timestamps[i] - minX) / rangeX;
            var v = values[i];
            x[i] = v.X;
            y[i] = v.Y;
            z[i] = v.Z;
        }

        // Draw curves
        Vector2 plotSize = new Vector2(400, 120);
        var drawList = ImGui.GetWindowDrawList();
        Vector2 basePos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(plotSize);

        drawList.AddRect(basePos, basePos + plotSize, ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1)));

        DrawCurve(drawList, basePos, plotSize, xs, x, new Vector4(1, 0, 0, 1));
        DrawCurve(drawList, basePos, plotSize, xs, y, new Vector4(0, 1, 0, 1));
        DrawCurve(drawList, basePos, plotSize, xs, z, new Vector4(0, 0, 1, 1));

        // Draw legend
        float legendX = basePos.X + 5;
        float legendY = basePos.Y + 5;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(1, 0, 0, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "X");
        legendY += 16;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(0, 1, 0, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "Y");
        legendY += 16;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(0, 0, 1, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "Z");
    }

    private void DrawQuaternionTrack(MutableM2Track<Quaternion> track, int animIdx, string label)
    {
        bool hasTrack = animIdx < track.Length;
        if (!hasTrack)
        {
            ImGui.TextUnformatted($"No {label.ToLower()} keys.");
            return;
        }

        var timestamps = track.Timestamps(animIdx);
        var values = track.Values(animIdx);
        int count = Math.Min(timestamps.Length, values.Length);

        if (count == 0)
        {
            ImGui.TextUnformatted($"No {label.ToLower()} keys.");
            return;
        }

        // Find min/max X for scaling
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < count; ++i)
        {
            float x_ = timestamps[i];
            if (x_ < minX) minX = x_;
            if (x_ > maxX) maxX = x_;
        }
        if (minX == float.MaxValue || maxX == float.MinValue)
        {
            minX = 0;
            maxX = 1;
        }
        float rangeX = Math.Max(1e-5f, maxX - minX);

        // Prepare curves
        float[] xs = new float[count];
        float[] x = new float[count];
        float[] y = new float[count];
        float[] z = new float[count];
        float[] w = new float[count];
        for (int i = 0; i < count; ++i)
        {
            xs[i] = (timestamps[i] - minX) / rangeX;
            var v = values[i];
            x[i] = v.X;
            y[i] = v.Y;
            z[i] = v.Z;
            w[i] = v.W;
        }

        // Draw curves
        Vector2 plotSize = new Vector2(400, 120);
        var drawList = ImGui.GetWindowDrawList();
        Vector2 basePos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(plotSize);

        drawList.AddRect(basePos, basePos + plotSize, ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1)));

        DrawCurve(drawList, basePos, plotSize, xs, x, new Vector4(1, 0, 0, 1));
        DrawCurve(drawList, basePos, plotSize, xs, y, new Vector4(0, 1, 0, 1));
        DrawCurve(drawList, basePos, plotSize, xs, z, new Vector4(0, 0, 1, 1));
        DrawCurve(drawList, basePos, plotSize, xs, w, new Vector4(1, 1, 0, 1));

        // Draw legend
        float legendX = basePos.X + 5;
        float legendY = basePos.Y + 5;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(1, 0, 0, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "X");
        legendY += 16;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(0, 1, 0, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "Y");
        legendY += 16;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(0, 0, 1, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "Z");
        legendY += 16;
        drawList.AddRectFilled(new Vector2(legendX, legendY), new Vector2(legendX + 12, legendY + 12), ImGui.GetColorU32(new Vector4(1, 1, 0, 1)));
        drawList.AddText(new Vector2(legendX + 16, legendY), ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), "W");
    }

    private void DrawCurve(ImDrawListPtr drawList, Vector2 basePos, Vector2 plotSize, float[] xs, float[] ys, Vector4 color)
    {
        if (xs.Length < 2)
            return;
        for (int i = 0; i < xs.Length - 1; ++i)
        {
            float x0 = basePos.X + xs[i] * plotSize.X;
            float y0 = basePos.Y + (1 - ys[i]) * plotSize.Y;
            float x1 = basePos.X + xs[i + 1] * plotSize.X;
            float y1 = basePos.Y + (1 - ys[i + 1]) * plotSize.Y;
            drawList.AddLine(new Vector2(x0, y0), new Vector2(x1, y1), ImGui.GetColorU32(color), 2.0f);
        }
    }
}


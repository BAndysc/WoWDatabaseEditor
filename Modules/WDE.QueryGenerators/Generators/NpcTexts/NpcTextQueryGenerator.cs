using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.NpcTexts;

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityCata", "TrinityWrath")]
public class NpcTextQueryGenerator : BaseInsertQueryProvider<INpcTextFull>, IDeleteQueryProvider<INpcText>
{
    protected override object Convert(INpcTextFull text)
    {
        return new
        {
            ID = text.Id,
            text0_0 = text.Text0_0,
            text0_1 = text.Text0_1,
            BroadcastTextID0 = text.BroadcastTextID0,
            lang0 = text.Lang0,
            Probability0 = text.Probability0,
            EmoteDelay0_0 = text.EmoteDelay0_0,
            Emote0_0 = text.Emote0_0,
            EmoteDelay0_1 = text.EmoteDelay0_1,
            Emote0_1 = text.Emote0_1,
            EmoteDelay0_2 = text.EmoteDelay0_2,
            Emote0_2 = text.Emote0_2,
            text1_0 = text.Text1_0,
            text1_1 = text.Text1_1,
            BroadcastTextID1 = text.BroadcastTextID1,
            lang1 = text.Lang1,
            Probability1 = text.Probability1,
            EmoteDelay1_0 = text.EmoteDelay1_0,
            Emote1_0 = text.Emote1_0,
            EmoteDelay1_1 = text.EmoteDelay1_1,
            Emote1_1 = text.Emote1_1,
            EmoteDelay1_2 = text.EmoteDelay1_2,
            Emote1_2 = text.Emote1_2,
            text2_0 = text.Text2_0,
            text2_1 = text.Text2_1,
            BroadcastTextID2 = text.BroadcastTextID2,
            lang2 = text.Lang2,
            Probability2 = text.Probability2,
            EmoteDelay2_0 = text.EmoteDelay2_0,
            Emote2_0 = text.Emote2_0,
            EmoteDelay2_1 = text.EmoteDelay2_1,
            Emote2_1 = text.Emote2_1,
            EmoteDelay2_2 = text.EmoteDelay2_2,
            Emote2_2 = text.Emote2_2,
            text3_0 = text.Text3_0,
            text3_1 = text.Text3_1,
            BroadcastTextID3 = text.BroadcastTextID3,
            lang3 = text.Lang3,
            Probability3 = text.Probability3,
            EmoteDelay3_0 = text.EmoteDelay3_0,
            Emote3_0 = text.Emote3_0,
            EmoteDelay3_1 = text.EmoteDelay3_1,
            Emote3_1 = text.Emote3_1,
            EmoteDelay3_2 = text.EmoteDelay3_2,
            Emote3_2 = text.Emote3_2,
            text4_0 = text.Text4_0,
            text4_1 = text.Text4_1,
            BroadcastTextID4 = text.BroadcastTextID4,
            lang4 = text.Lang4,
            Probability4 = text.Probability4,
            EmoteDelay4_0 = text.EmoteDelay4_0,
            Emote4_0 = text.Emote4_0,
            EmoteDelay4_1 = text.EmoteDelay4_1,
            Emote4_1 = text.Emote4_1,
            EmoteDelay4_2 = text.EmoteDelay4_2,
            Emote4_2 = text.Emote4_2,
            text5_0 = text.Text5_0,
            text5_1 = text.Text5_1,
            BroadcastTextID5 = text.BroadcastTextID5,
            lang5 = text.Lang5,
            Probability5 = text.Probability5,
            EmoteDelay5_0 = text.EmoteDelay5_0,
            Emote5_0 = text.Emote5_0,
            EmoteDelay5_1 = text.EmoteDelay5_1,
            Emote5_1 = text.Emote5_1,
            EmoteDelay5_2 = text.EmoteDelay5_2,
            Emote5_2 = text.Emote5_2,
            text6_0 = text.Text6_0,
            text6_1 = text.Text6_1,
            BroadcastTextID6 = text.BroadcastTextID6,
            lang6 = text.Lang6,
            Probability6 = text.Probability6,
            EmoteDelay6_0 = text.EmoteDelay6_0,
            Emote6_0 = text.Emote6_0,
            EmoteDelay6_1 = text.EmoteDelay6_1,
            Emote6_1 = text.Emote6_1,
            EmoteDelay6_2 = text.EmoteDelay6_2,
            Emote6_2 = text.Emote6_2,
            text7_0 = text.Text7_0,
            text7_1 = text.Text7_1,
            BroadcastTextID7 = text.BroadcastTextID7,
            lang7 = text.Lang7,
            Probability7 = text.Probability7,
            EmoteDelay7_0 = text.EmoteDelay7_0,
            Emote7_0 = text.Emote7_0,
            EmoteDelay7_1 = text.EmoteDelay7_1,
            Emote7_1 = text.Emote7_1,
            EmoteDelay7_2 = text.EmoteDelay7_2,
            Emote7_2 = text.Emote7_2,
            VerifiedBuild = text.VerifiedBuild,
        };
    }

    public IQuery Delete(INpcText t)
    {
        return Queries.Table(TableName)
            .Where(x => x.Column<uint>("ID") == t.Id)
            .Delete();
    }
    
    public override DatabaseTable TableName => DatabaseTable.WorldTable("npc_text");
}
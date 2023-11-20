using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.NpcTexts;

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]
public class CMangosNpcTextQueryGenerator : BaseInsertQueryProvider<INpcTextFull>, IDeleteQueryProvider<INpcText>
{
    protected override object Convert(INpcTextFull text)
    {
        return new
        {
            ID = text.Id,
            text0_0 = text.Text0_0,
            text0_1 = text.Text0_1,
            lang0 = text.Lang0,
            prob0 = text.Probability0,
            em0_3 = text.Emote0_1,
            em0_0 = text.EmoteDelay0_0,
            em0_4 = text.EmoteDelay0_2,
            em0_1 = text.Emote0_0,
            em0_5 = text.Emote0_2,
            em0_2 = text.EmoteDelay0_1,
            text1_0 = text.Text1_0,
            text1_1 = text.Text1_1,
            lang1 = text.Lang1,
            prob1 = text.Probability1,
            em1_3 = text.Emote1_1,
            em1_0 = text.EmoteDelay1_0,
            em1_4 = text.EmoteDelay1_2,
            em1_1 = text.Emote1_0,
            em1_5 = text.Emote1_2,
            em1_2 = text.EmoteDelay1_1,
            text2_0 = text.Text2_0,
            text2_1 = text.Text2_1,
            lang2 = text.Lang2,
            prob2 = text.Probability2,
            em2_3 = text.Emote2_1,
            em2_0 = text.EmoteDelay2_0,
            em2_4 = text.EmoteDelay2_2,
            em2_1 = text.Emote2_0,
            em2_5 = text.Emote2_2,
            em2_2 = text.EmoteDelay2_1,
            text3_0 = text.Text3_0,
            text3_1 = text.Text3_1,
            lang3 = text.Lang3,
            prob3 = text.Probability3,
            em3_3 = text.Emote3_1,
            em3_0 = text.EmoteDelay3_0,
            em3_4 = text.EmoteDelay3_2,
            em3_1 = text.Emote3_0,
            em3_5 = text.Emote3_2,
            em3_2 = text.EmoteDelay3_1,
            text4_0 = text.Text4_0,
            text4_1 = text.Text4_1,
            lang4 = text.Lang4,
            prob4 = text.Probability4,
            em4_3 = text.Emote4_1,
            em4_0 = text.EmoteDelay4_0,
            em4_4 = text.EmoteDelay4_2,
            em4_1 = text.Emote4_0,
            em4_5 = text.Emote4_2,
            em4_2 = text.EmoteDelay4_1,
            text5_0 = text.Text5_0,
            text5_1 = text.Text5_1,
            lang5 = text.Lang5,
            prob5 = text.Probability5,
            em5_3 = text.Emote5_1,
            em5_0 = text.EmoteDelay5_0,
            em5_4 = text.EmoteDelay5_2,
            em5_1 = text.Emote5_0,
            em5_5 = text.Emote5_2,
            em5_2 = text.EmoteDelay5_1,
            text6_0 = text.Text6_0,
            text6_1 = text.Text6_1,
            lang6 = text.Lang6,
            prob6 = text.Probability6,
            em6_3 = text.Emote6_1,
            em6_0 = text.EmoteDelay6_0,
            em6_4 = text.EmoteDelay6_2,
            em6_1 = text.Emote6_0,
            em6_5 = text.Emote6_2,
            em6_2 = text.EmoteDelay6_1,
            text7_0 = text.Text7_0,
            text7_1 = text.Text7_1,
            lang7 = text.Lang7,
            prob7 = text.Probability7,
            em7_3 = text.Emote7_1,
            em7_0 = text.EmoteDelay7_0,
            em7_4 = text.EmoteDelay7_2,
            em7_1 = text.Emote7_0,
            em7_5 = text.Emote7_2,
            em7_2 = text.EmoteDelay7_1
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
using System;

namespace WowPacketParser.Proto.Processing
{
    public abstract class PacketProcessor<T> : IPacketProcessor<T>
    {
        protected ulong GameBuild { get; private set; }
        
        public virtual void Initialize(ulong gameBuild)
        {
            GameBuild = gameBuild;
        }

        public unsafe TPtr? Unpack<TPtr>(TPtr* ptr) where TPtr : unmanaged => ptr == null ? null : *ptr;

        public unsafe delegate TVal UnpackDelegate<TPtr, TVal>(TPtr* ptr) where TPtr : unmanaged;

        public unsafe TVal? Unpack<TPtr, TVal>(TPtr* ptr, UnpackDelegate<TPtr, TVal> extract) where TPtr : unmanaged where TVal : unmanaged => ptr == null ? null : extract(ptr);

        public virtual T? Process(ref readonly PacketHolder packet)
        {
            switch (packet.KindCase)
            {
                case PacketHolder.KindOneofCase.None:
                    return default;
                case PacketHolder.KindOneofCase.Chat:
                    return Process(in packet.BaseData, in packet.Chat);
                case PacketHolder.KindOneofCase.QueryCreatureResponse:
                    return Process(in packet.BaseData, in packet.QueryCreatureResponse);
                case PacketHolder.KindOneofCase.Emote:
                    return Process(in packet.BaseData, in packet.Emote);
                case PacketHolder.KindOneofCase.PlaySound:
                    return Process(in packet.BaseData, in packet.PlaySound);
                case PacketHolder.KindOneofCase.PlayMusic:
                    return Process(in packet.BaseData, in packet.PlayMusic);
                case PacketHolder.KindOneofCase.PlayObjectSound:
                    return Process(in packet.BaseData, in packet.PlayObjectSound);
                case PacketHolder.KindOneofCase.GossipHello:
                    return Process(in packet.BaseData, in packet.GossipHello);
                case PacketHolder.KindOneofCase.GossipMessage:
                    return Process(in packet.BaseData, in packet.GossipMessage);
                case PacketHolder.KindOneofCase.GossipSelect:
                    return Process(in packet.BaseData, in packet.GossipSelect);
                case PacketHolder.KindOneofCase.GossipClose:
                    return Process(in packet.BaseData, in packet.GossipClose);
                case PacketHolder.KindOneofCase.SpellStart:
                    return Process(in packet.BaseData, in packet.SpellStart);
                case PacketHolder.KindOneofCase.SpellGo:
                    return Process(in packet.BaseData, in packet.SpellGo);
                case PacketHolder.KindOneofCase.AuraUpdate:
                    return Process(in packet.BaseData, in packet.AuraUpdate);
                case PacketHolder.KindOneofCase.MonsterMove:
                    return Process(in packet.BaseData, in packet.MonsterMove);
                case PacketHolder.KindOneofCase.PhaseShift:
                    return Process(in packet.BaseData, in packet.PhaseShift);
                case PacketHolder.KindOneofCase.SpellClick:
                    return Process(in packet.BaseData, in packet.SpellClick);
                case PacketHolder.KindOneofCase.PlayerLogin:
                    return Process(in packet.BaseData, in packet.PlayerLogin);
                case PacketHolder.KindOneofCase.OneShotAnimKit:
                    return Process(in packet.BaseData, in packet.OneShotAnimKit);
                case PacketHolder.KindOneofCase.SetAnimKit:
                    return Process(in packet.BaseData, in packet.SetAnimKit);
                case PacketHolder.KindOneofCase.PlaySpellVisualKit:
                    return Process(in packet.BaseData, in packet.PlaySpellVisualKit);
                case PacketHolder.KindOneofCase.QuestGiverAcceptQuest:
                    return Process(in packet.BaseData, in packet.QuestGiverAcceptQuest);
                case PacketHolder.KindOneofCase.QuestGiverCompleteQuestRequest:
                    return Process(in packet.BaseData, in packet.QuestGiverCompleteQuestRequest);
                case PacketHolder.KindOneofCase.QuestGiverQuestComplete:
                    return Process(in packet.BaseData, in packet.QuestGiverQuestComplete);
                case PacketHolder.KindOneofCase.QuestGiverRequestItems:
                    return Process(in packet.BaseData, in packet.QuestGiverRequestItems);
                case PacketHolder.KindOneofCase.NpcText:
                    return Process(in packet.BaseData, in packet.NpcText);
                case PacketHolder.KindOneofCase.NpcTextOld:
                    return Process(in packet.BaseData, in packet.NpcTextOld);
                case PacketHolder.KindOneofCase.DbReply:
                    return Process(in packet.BaseData, in packet.DbReply);
                case PacketHolder.KindOneofCase.UpdateObject:
                    return Process(in packet.BaseData, in packet.UpdateObject);
                case PacketHolder.KindOneofCase.QueryGameObjectResponse:
                    return Process(in packet.BaseData, in packet.QueryGameObjectResponse);
                case PacketHolder.KindOneofCase.ClientAreaTrigger:
                    return Process(in packet.BaseData, in packet.ClientAreaTrigger);
                case PacketHolder.KindOneofCase.QueryPlayerNameResponse:
                    return Process(in packet.BaseData, in packet.QueryPlayerNameResponse);
                case PacketHolder.KindOneofCase.QuestComplete:
                    return Process(in packet.BaseData, in packet.QuestComplete);
                case PacketHolder.KindOneofCase.QuestFailed:
                    return Process(in packet.BaseData, in packet.QuestFailed);
                case PacketHolder.KindOneofCase.QuestAddKillCredit:
                    return Process(in packet.BaseData, in packet.QuestAddKillCredit);
                case PacketHolder.KindOneofCase.ClientUseItem:
                    return Process(in packet.BaseData, in packet.ClientUseItem);
                case PacketHolder.KindOneofCase.ClientQuestGiverChooseReward:
                    return Process(in packet.BaseData, in packet.ClientQuestGiverChooseReward);
                case PacketHolder.KindOneofCase.ClientMove:
                    return Process(in packet.BaseData, in packet.ClientMove);
                case PacketHolder.KindOneofCase.ClientUseGameObject:
                    return Process(in packet.BaseData, in packet.ClientUseGameObject);
                case PacketHolder.KindOneofCase.GossipPoi:
                    return Process(in packet.BaseData, in packet.GossipPoi);
                case PacketHolder.KindOneofCase.GameObjectCustomAnim:
                    return Process(in packet.BaseData, in packet.GameObjectCustomAnim);
                case PacketHolder.KindOneofCase.SpellCastFailed:
                    return Process(in packet.BaseData, in packet.SpellCastFailed);
                case PacketHolder.KindOneofCase.SpellFailure:
                    return Process(in packet.BaseData, in packet.SpellFailure);
                case PacketHolder.KindOneofCase.LoginSetTimeSpeed:
                    return Process(in packet.BaseData, in packet.LoginSetTimeSpeed);
                case PacketHolder.KindOneofCase.AuraUpdateAll:
                    return Process(in packet.BaseData, in packet.AuraUpdateAll);
                case PacketHolder.KindOneofCase.AiReaction:
                    return Process(in packet.BaseData, in packet.AiReaction);
                case PacketHolder.KindOneofCase.InitWorldStates:
                    return Process(in packet.BaseData, in packet.InitWorldStates);
                case PacketHolder.KindOneofCase.UpdateWorldState:
                    return Process(in packet.BaseData, in packet.UpdateWorldState);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQueryGameObjectResponse packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketClientAreaTrigger packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketPhaseShift packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipClose packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipSelect packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipMessage packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipHello packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketPlayObjectSound packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketPlayMusic packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySound packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketEmote packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQueryCreatureResponse packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellClick packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketPlayerLogin packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketOneShotAnimKit packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketSetAnimKit packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySpellVisualKit packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverAcceptQuest packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverCompleteQuestRequest packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverQuestComplete packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverRequestItems packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketNpcText packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketNpcTextOld packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketDbReply packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQueryPlayerNameResponseWrapper packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestComplete packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestFailed packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestAddKillCredit packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseItem packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketClientQuestGiverChooseReward packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketClientMove packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseGameObject packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipPoi packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketGameObjectCustomAnim packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellCastFailed packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellFailure packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketLoginSetTimeSpeed packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdateAll packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketAIReaction packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketInitWorldStates packet) => default;
        protected virtual T? Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateWorldState packet) => default;
    }
}

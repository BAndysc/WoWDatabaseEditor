using System.Collections.Generic;
using System.Linq;
using WDE.PacketViewer.Processing.Processors.ActionReaction;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class ActionReasonPredictionViewModel
    {
        public int PacketNumber { get; }
        public int Chance { get; }
        public int Diff { get; }
        public string Description { get; }
        public string Explain { get; }
        
        public ActionReasonPredictionViewModel(PacketBase action, double probability, string explain, EventHappened reason)
        {
            PacketNumber = reason.PacketNumber;
            Description = reason.Description;
            Chance = (int)(probability * 100);
            Explain = explain;
            Diff = (int)(action.Time.ToDateTime() - reason.Time).TotalMilliseconds;
        }
    }

    public class DetectedActionViewModel
    {
        public DetectedActionViewModel(ActionHappened actionHappened)
        {
            ActionKind = actionHappened.Kind.ToString();
            Description = actionHappened.Description;
            MainActor = actionHappened.MainActor.ToWowParserString();
            AdditionalActors = actionHappened.AdditionalActors?.Select(s => s.ToWowParserString()).ToList();
            if (actionHappened.EventLocation != null)
                Location = $"X: {actionHappened.EventLocation.Value.X} Y: {actionHappened.EventLocation.Value.Y} Z: {actionHappened.EventLocation.Value.Z}";
        }

        public string ActionKind { get; }
        public string Description { get; }
        public string? MainActor { get; }
        public string? Location { get; }
        public List<string>? AdditionalActors { get; }
    }


    public class DetectedEventViewModel
    {
        public DetectedEventViewModel(EventHappened eventHappened)
        {
            EventKind = eventHappened.Kind.ToString();
            Description = eventHappened.Description;
            MainActor = eventHappened.MainActor.ToWowParserString();
            AdditionalActors = eventHappened.AdditionalActors?.Select(s => s.ToWowParserString()).ToList();
            if (eventHappened.EventLocation != null)
                Location = $"X: {eventHappened.EventLocation.Value.X} Y: {eventHappened.EventLocation.Value.Y} Z: {eventHappened.EventLocation.Value.Z}";
        }

        public string EventKind { get; }
        public string Description { get; }
        public string? MainActor { get; }
        public string? Location { get; }
        public List<string>? AdditionalActors { get; }
    }

    public class PossibleActionViewModel
    {
        public int PacketNumber { get; }
        public int Chance { get; }
        public int Diff { get; }
        public string Description { get; }
        public string Explain { get; }
        
        public PossibleActionViewModel(PacketBase @event, double probability, string explain, ActionHappened action)
        {
            PacketNumber = action.PacketNumber;
            Description = action.Description;
            Chance = (int)(probability * 100);
            Explain = explain;
            Diff = (int)(action.Time - @event.Time.ToDateTime()).TotalMilliseconds;
        }
    }
    
}
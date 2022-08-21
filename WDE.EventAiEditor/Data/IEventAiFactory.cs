using System;
using WDE.Common.Database;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Data
{
    public interface IEventAiFactory
    {
        EventAiEvent EventFactory(uint id);

        EventAiEvent EventFactory(IEventAiLine line);

        EventAiAction ActionFactory(uint id);

        EventAiAction? ActionFactory(IEventAiLine line, int actionIndex);
        
        void UpdateEvent(EventAiEvent eventAiEvent, uint id);

        void UpdateAction(EventAiAction eventAiAction, uint id);
    }

    public class InvalidActionException : Exception
    {
        public InvalidActionException(uint id) : base("Invalid action with id " + id) { }
    }

    public class InvalidEventException : Exception
    {
        public InvalidEventException(uint id) : base("Invalid event with id " + id) { }
    }
}
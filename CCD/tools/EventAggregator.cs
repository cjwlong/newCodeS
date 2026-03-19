using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCD.tools
{
    public class EventAggregator
    {
        private Dictionary<Type, List<Action<object>>> _eventSubscribers = new Dictionary<Type, List<Action<object>>>();

        private static EventAggregator instance;

        public static EventAggregator Instance
        {
            get
            {
                instance ??= new EventAggregator();
                return instance;
            }
        }

        private EventAggregator() { }
        public void Subscribe<TEventType>(Action<TEventType> action)
        {
            Type eventType = typeof(TEventType);
            if (!_eventSubscribers.ContainsKey(eventType))
            {
                _eventSubscribers[eventType] = new List<Action<object>>();
            }
            _eventSubscribers[eventType].Add(obj => action((TEventType)obj));
        }

        public void Publish<TEventType>(TEventType eventData)
        {
            Type eventType = typeof(TEventType);
            if (_eventSubscribers.ContainsKey(eventType))
            {
                foreach (var action in _eventSubscribers[eventType])
                {
                    action.Invoke(eventData);
                }
            }
        }


    }
}

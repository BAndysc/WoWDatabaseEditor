using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace AvaloniaStyles.Utils
{
    public class SolidBrushTransition : Transition<IBrush>
    {
        /// <inheritdocs/>
        public override IObservable<IBrush> DoTransition(IObservable<double> progress, IBrush oldValue,
            IBrush newValue)
        {
            return progress
                .Select(p =>
                {
                    SolidColorBrush old = oldValue as SolidColorBrush;
                    SolidColorBrush nnew = newValue as SolidColorBrush;
                    if (old == null || nnew == null)
                        return newValue;
                    var f = Easing.Ease(p);
                    return new SolidColorBrush(new Color(
                        (byte)((nnew.Color.A - old.Color.A) * f + old.Color.A),
                        (byte)((nnew.Color.R - old.Color.R) * f + old.Color.R),
                        (byte)((nnew.Color.G - old.Color.G) * f + old.Color.G),
                        (byte)((nnew.Color.B - old.Color.B) * f + old.Color.B)
                    ));
                });
        }
    }

    // copy paste of Avalonia Transition class, because I do not know why original doesn't work here...
    public abstract class Transition<T> : AvaloniaObject, ITransition
    {
        private AvaloniaProperty _prop;

        /// <summary>
        /// Gets or sets the duration of the transition.
        /// </summary> 
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets delay before starting the transition.
        /// </summary> 
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets the easing class to be used.
        /// </summary>
        public Easing Easing { get; set; } = new LinearEasing();

        /// <inheritdocs/>
        public AvaloniaProperty Property
        {
            get { return _prop; }
            set
            {
                if (!(value.PropertyType.IsAssignableFrom(typeof(T))))
                    throw new InvalidCastException
                        ($"Invalid property type \"{typeof(T).Name}\" for this transition: {GetType().Name}.");

                _prop = value;
            }
        }

        /// <summary>
        /// Apply interpolation to the property.
        /// </summary>
        public abstract IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue);

        /// <inheritdocs/>
        public virtual IDisposable Apply(Animatable control, IClock clock, object oldValue, object newValue)
        {
            var transition = DoTransition(new TransitionInstance(clock, Delay, Duration), (T) oldValue, (T) newValue);
            return control.Bind<T>((AvaloniaProperty<T>) Property, transition, Avalonia.Data.BindingPriority.Animation);
        }
    }
    
    internal class TransitionInstance : SingleSubscriberObservableBase<double>
    {
        private IDisposable _timerSubscription;
        private TimeSpan _delay;
        private TimeSpan _duration;
        private readonly IClock _baseClock;
        private IClock _clock;

        public TransitionInstance(IClock clock, TimeSpan delay, TimeSpan duration)
        {
            clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _delay = delay;
            _duration = duration;
            _baseClock = clock;
        }

        private void TimerTick(TimeSpan t)
        {

            // [<------------- normalizedTotalDur ------------------>]
            // [<---- Delay ---->][<---------- Duration ------------>]
            //                   ^- normalizedDelayEnd
            //                    [<----   normalizedInterpVal   --->]

            var normalizedInterpVal = 1d;

            if (!MathUtilities.AreClose(_duration.TotalSeconds, 0d))
            {
                var normalizedTotalDur = _delay + _duration;
                var normalizedDelayEnd = _delay.TotalSeconds / normalizedTotalDur.TotalSeconds;
                var normalizedPresentationTime = t.TotalSeconds / normalizedTotalDur.TotalSeconds;

                if (normalizedPresentationTime < normalizedDelayEnd
                    || MathUtilities.AreClose(normalizedPresentationTime, normalizedDelayEnd))
                {
                    normalizedInterpVal = 0d;
                }
                else
                {
                    normalizedInterpVal = (t.TotalSeconds - _delay.TotalSeconds) / _duration.TotalSeconds;
                }
            }

            // Clamp interpolation value.
            if (normalizedInterpVal >= 1d || normalizedInterpVal < 0d)
            {
                PublishNext(1d);
                PublishCompleted();
            }
            else
            {
                PublishNext(normalizedInterpVal);
            }
        }

        protected override void Unsubscribed()
        {
            _timerSubscription?.Dispose();
            _clock.PlayState = PlayState.Stop;
        }

        protected override void Subscribed()
        {
            _clock = new Clock(_baseClock);
            _timerSubscription = _clock.Subscribe(TimerTick);
            PublishNext(0.0d);
        }
    }
}
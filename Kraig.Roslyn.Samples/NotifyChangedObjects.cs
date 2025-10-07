using Kraig.Attributes;

namespace Kraig.Roslyn.Samples.NotifyChangedObjects
{
    internal partial class DefaultClass
    {
        [NotifyChanged] private int _digit = 10;

        public int Sqrt() => (int)Math.Sqrt(Digit);
    }

    internal partial class ClassEvent
    {
        [NotifyChanged(GenerateEvent = true)] private int _digit = 15;

        public ClassEvent()
        {
            DigitChanged += () => Console.Beep();
        }
    }
}

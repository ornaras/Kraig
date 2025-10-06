using Kraig.Attributes;

namespace Kraig.Roslyn.Samples.SingletonObjects
{
    [Singleton]
    public partial class SimpleSingleton
    {
        public Guid Id { get; init; } = Guid.NewGuid();
    }

    [Singleton]
    public partial class SingletonWithConstructor
    {
        public Guid Id { get; init; }
        private SingletonWithConstructor()
        {
            Id = Guid.NewGuid();
        }
    }

    [Singleton]
    public partial class SingletonWithPublicConstructor
    {
        public Guid Id { get; init; }
        public SingletonWithPublicConstructor()
        {
            Id = Guid.NewGuid();
        }
    }

    [Singleton]
    public partial class SingletonWithParameteredConstructor
    {
        public Guid Id { get; init; }
        private SingletonWithParameteredConstructor(string id)
        {
            Id = Guid.Parse(id);
        }
    }
}

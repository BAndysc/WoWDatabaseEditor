using System;

namespace TheEngine.ECS
{
    public readonly struct Entity
    {
        public readonly uint Id;
        public readonly uint Version;

        public Entity(uint id, uint version)
        {
            Id = id;
            Version = version;
        }

        public bool IsEmpty() => Id == 0 && Version == 0;

        public static Entity Empty => default;

        public bool Equals(Entity other) => Id == other.Id && Version == other.Version;

        public override bool Equals(object? obj) => obj is Entity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, Version);

        public static bool operator ==(Entity left, Entity right) => left.Equals(right);

        public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

        public override string ToString()
        {
            return $"Entity[{Id}, {Version}]";
        }
    }
}
using TheEngine.ECS;

namespace TheEngine.Components
{
    /// <summary>
    /// Intrusive doubly-linked scene-graph node. Present on every entity (added by
    /// <see cref="IEntityManager.NewArchetype"/>, just like <see cref="EntityName"/>).
    ///
    /// All links are <see cref="Entity.Empty"/> when absent. Children of a node form a doubly
    /// linked list reachable from <see cref="FirstChild"/> via <see cref="NextSibling"/>.
    /// Roots (entities whose <see cref="Parent"/> is Empty) form the same kind of list,
    /// chained off EntityManager's internal firstRoot — i.e. roots are the "children" of a
    /// virtual Empty parent. Maintained exclusively through <see cref="IEntityManager.SetParent"/>,
    /// entity creation and destruction; do not poke the fields directly.
    /// </summary>
    public struct Relationship : IComponentData
    {
        public Entity Parent;
        public Entity FirstChild;
        public Entity PrevSibling;
        public Entity NextSibling;
        public int ChildCount;

        public bool HasChildren => FirstChild != Entity.Empty;
    }

    public static class RelationshipExtensions
    {
        /// <summary>Re-parents <paramref name="child"/> under <paramref name="parent"/>
        /// (or makes it a root when <paramref name="parent"/> is <see cref="Entity.Empty"/>).</summary>
        public static void SetParent(this Entity child, IEntityManager entityManager, Entity parent)
        {
            entityManager.SetParent(child, parent);
        }
    }
}

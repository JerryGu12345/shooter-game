using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerAuthoring> {
        public override void Bake(PlayerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player());
            //AddComponent(entity, new PlayerNeedsChildren());
        }
    }
}

public struct Player : IComponentData {
    [GhostField] public float health;
    public float firecooldown;
}

[GhostComponent]
public struct PlayerNeedsChildren : IComponentData
{
    public Entity Head;
    public Entity Weapon;
}
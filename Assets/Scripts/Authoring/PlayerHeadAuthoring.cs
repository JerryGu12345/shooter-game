using Unity.Entities;
using UnityEngine;

public class PlayerHeadAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerHeadAuthoring> {
        public override void Bake(PlayerHeadAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerHead());
        }
    }
}

public struct PlayerHead : IComponentData {

}
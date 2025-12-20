using Unity.Entities;
using UnityEngine;

public class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject playerPrefabGameObject, headPrefabGameObject, weaponPrefabGameObject;
    public GameObject bulletPrefabGameObject;
    public class Baker : Baker<EntitiesReferencesAuthoring> {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntitiesReferences {
                playerPrefabEntity = GetEntity(authoring.playerPrefabGameObject, TransformUsageFlags.Dynamic),
                headPrefabEntity = GetEntity(authoring.headPrefabGameObject, TransformUsageFlags.Dynamic),
                weaponPrefabEntity = GetEntity(authoring.weaponPrefabGameObject, TransformUsageFlags.Dynamic),

                bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct EntitiesReferences : IComponentData {
    public Entity playerPrefabEntity, headPrefabEntity, weaponPrefabEntity;
    public Entity bulletPrefabEntity;
}
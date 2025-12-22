using System.Collections.Generic;
using System.Threading;
using Unity.Entities;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public class Baker : Baker<BulletAuthoring> {
        public override void Bake(BulletAuthoring authoring)
        {/*
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Bullet {
                timer = 0.0f,
                moveSpeed = 10f
            });*/
        }
    }
}

public struct Bullet : IComponentData {
    public float timer;
    public float moveSpeed;
    public int p0;
    public int p1;
    public int p2;
    public int damage;
}
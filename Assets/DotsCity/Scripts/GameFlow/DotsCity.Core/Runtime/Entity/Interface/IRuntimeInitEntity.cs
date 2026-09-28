using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public interface IRuntimeInitEntity
    {
        public void Initialize(EntityManager entityManager, GameObject root, Entity entity);
    }
}

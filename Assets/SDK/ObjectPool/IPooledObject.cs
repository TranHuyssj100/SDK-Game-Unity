using UnityEngine;

namespace SDK.ObjectPool
{
    public interface IPooledObject
    {
        void OnObjectGet();
        void OnObjectReturn();
    }
}
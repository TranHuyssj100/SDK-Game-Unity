using UnityEngine;

namespace LSDK.Core.ObjectPools
{
    public interface IPooledObject
    {
        void OnObjectGet();
        void OnObjectReturn();
    }
}
using UnityEngine;

namespace LSDK.Core.ObjectPools
{

  public interface IObjectPool
  {

    string Id { get; }
    GameObject Prefab { get; }
    int CountInActive { get; }
    int CountActive { get; }


    GameObject GetObject();
    void ReturnObject(GameObject obj);
    void Clear(bool destroyObjects);
  }
}


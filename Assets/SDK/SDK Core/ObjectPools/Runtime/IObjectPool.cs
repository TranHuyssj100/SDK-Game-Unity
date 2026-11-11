using UnityEngine;

namespace LSDK.Core.ObjectPools
{

  public interface IObjectPool
  {

    int Id { get; }
    GameObject Prefab { get; }
    int CountInActive { get; }
    int CountActive { get; }


    GameObject GetObject();
    void ReturnObject(GameObject obj);
    void Clear(bool destroyObjects);
  }
}


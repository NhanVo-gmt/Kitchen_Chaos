using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject prefab;
    
    private NetworkVariable<MyCustomData> randomNumber = new(new MyCustomData()
    {
        a = 1,
        b = "hello"
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct MyCustomData : INetworkSerializable
    {
        public int    a;
        public string b;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref a);    
            serializer.SerializeValue(ref b);    
        }
    }

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (MyCustomData previousVal, MyCustomData newVal) =>
        {
            Debug.Log($"{OwnerClientId}: {newVal.a} {newVal.b}");
        };
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetKey(KeyCode.T))
        {
            SpawnPrefab();
            // TestServerRpc();
        }
        
        Movement();
    }

    void Movement()
    {
        Vector3 moveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) moveDir.z += 1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z -= 1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x -= 1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x += 1f;

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    void SpawnPrefab()
    {
        SpawnPrefabServerRpc();
    }

    [ServerRpc]
    void SpawnPrefabServerRpc()
    {
        GameObject spawnedGameObject = Instantiate(prefab);
        spawnedGameObject.GetComponent<NetworkObject>().Spawn(true);
    }

    [ServerRpc]
    private void TestServerRpc()
    {
        Debug.Log(1231231);
    }
}

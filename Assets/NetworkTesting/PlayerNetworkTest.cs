using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerNetworkTest : NetworkBehaviour
{
    [SerializeField] private Transform spawnedObjectPrefab;


    //You can use primite value types and structs, but structs must contain only primitive value types
    public NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct MyCustomData : INetworkSerializable
    {
        public int myInt;
        public bool myBool;
        FixedString128Bytes networkString; //Fixed Strings dont grow, so dont fuck up the size : 1 bit = 1 character

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref myInt);
            serializer.SerializeValue(ref myBool);
            serializer.SerializeValue(ref networkString);
        }
    }

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (int previousValus, int newValue) => {
            Debug.Log(OwnerClientId + "," + randomNumber.Value.ToString());
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) { return; }


        if (Input.GetKeyDown(KeyCode.T))
        {
            //Transform spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
            //spawnedObjectTransform.GetComponent<NetworkObject>().Spawn();
            TestClientRpc(new ClientRpcParams());
        }

        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) { moveDir.z = 1; }

        if (Input.GetKey(KeyCode.S)) { moveDir.z = -1; }

        if (Input.GetKey(KeyCode.A)) { moveDir.x = 1; }

        if (Input.GetKey(KeyCode.D)) { moveDir.x = -1; }

        float moveSpeed = 3.0f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }


    //Server RPC are to be fired from a Client and then runs on Only the Server
    [ServerRpc()]
    private void TestServerRpc(ServerRpcParams serverRpcParams)
    {
        Debug.Log("Test Server RPC " + OwnerClientId + " , " +  serverRpcParams.Receive.SenderClientId);
    }

    //Client RPC are to be fired from a Server and thens runs on Each Client, Cannot be fired from a Client
    [ClientRpc]
    private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log("Client RPC");
    }
}

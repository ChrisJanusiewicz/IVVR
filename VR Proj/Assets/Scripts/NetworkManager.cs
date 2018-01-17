﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class NetworkManager : MonoBehaviour {

	public static bool serverHosted = false;

	private int socketId;
	private int hostId;
	private int connectionId;
	private int myReliableChannelId;
	private int myUnreliableChannelId;
	private int myUpdateChannelId;
    private int myStateChannelId;
	private bool networkInitialised;
    private bool clientConnected;

	public GameObject leftController;
	public GameObject rightController;
    public GameObject Camera;
    public GameObject spooker;
    public GameObject throwBall;
    public GameObject wallDemo;
    public GameObject tracker;
    //dictionary to identify individual objects, may not be super necessary eventually
    public Dictionary<int, GameObject> networkedObjects;
    //message queue to help ease load off of the send buffer.
    public Queue messageQueue = new Queue();

    //value to check when we should send a new batch of updates
    private float timer;
    public int updateRatePerSec = 20;

	// Use this for initialization
	void Start () {
        //needs to host!
        networkInitialised = false;

        if (!networkInitialised)
        {
            NetworkTransport.Init();
            Debug.Log("Host Started");
            ConnectionConfig config = new ConnectionConfig();
            myUnreliableChannelId = config.AddChannel(QosType.Unreliable);
            myUpdateChannelId = config.AddChannel(QosType.Reliable);
            myStateChannelId = config.AddChannel(QosType.StateUpdate);
            HostTopology topology = new HostTopology(config, 2);
            socketId = NetworkTransport.AddHost(topology, 9090);
            Debug.Log("Socket ID: " + socketId);
            networkInitialised = true;/* TODO For the time being! */
        }
        //get the list of networked objects saved in a list
        GameObject[] netObjects = GameObject.FindGameObjectsWithTag("NetworkedObject");
        //put them in a list
        networkedObjects = new Dictionary<int, GameObject>();
        for (int i = 0; i < netObjects.Length; i++) {
            //assume they are all balls to start!
            GameObject gameObject = netObjects[i];
            int id = gameObject.GetComponent<NetworkIdentity>().getObjectId();
            networkedObjects.Add(id, gameObject);
        }
        //initialise timer
        timer = Time.timeSinceLevelLoad;
	}
	
	// Update is called once per frame
	void Update () {
		//test for buttons to do stuff! 
        //Debug.Log("Coordinates for Left Controller: " + leftController.transform.position.ToString());
        /* GET RID OF THIS FOR PHONE */
        if (Input.GetKeyDown(KeyCode.A))
            Camera.GetComponent<shootBall>().throwBall(1);
        else if (Input.GetKeyDown(KeyCode.S))
            Camera.GetComponent<wallDemo>().spawnWall();
        else if (Input.GetKeyDown(KeyCode.D))
            Camera.GetComponent<wallDemo>().demolishWall();
        else if (Input.GetKeyDown(KeyCode.F))
            spooker.GetComponent<Spook>().spookPlayer();
        
        if (networkInitialised)
        {
            int recHostId;
            int connectionId;
            int channelId;
            byte[] recBuffer = new byte[530]; //Info for actions to occur
            int bufferSize = 530;
            int dataSize;
            byte error;
            BinaryFormatter bf; //in case it is needed in the switch
            NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
            switch (recData)
            {
                case NetworkEventType.Nothing:
                    //because nothing is happening, let's try and update the AR on positions
                    break;
                    /* IGNORING THIS STUFF FOR NOW!
                    if (clientConnected && (Time.timeSinceLevelLoad - timer > (1.0/updateRatePerSec))) {
                        Debug.Log("Trying to update client on " + networkedObjects.Count + " objects");
                        for (int i = 0; i < networkedObjects.Count; i++) {
                            NetworkMessage message = new NetworkMessage(3, i, null, networkedObjects[i].transform);
                            byte error2;
                            bf = new BinaryFormatter();
                            using (MemoryStream ms = new MemoryStream()) {
                                bf.Serialize(ms, message);
                                NetworkTransport.Send(socketId, this.connectionId, myUpdateChannelId, ms.ToArray(), 1024, out error2);
                            }
                        }
                        //finished update, save timer val
                        timer = Time.timeSinceLevelLoad;
                    }
                    break;
                    */
                case NetworkEventType.ConnectEvent: //AR connects
                    Debug.Log("Connection request from id: " + connectionId + " Received");
                    this.connectionId = connectionId;
                    for (int i = 0; i<networkedObjects.Count; i++) {
                        NetworkMessage message = new NetworkMessage(0, i, (int)Prefabs.PID.Ball, networkedObjects[i].transform);
                        byte error2;
                        bf = new BinaryFormatter();
                        using (MemoryStream ms = new MemoryStream()) {
                            bf.Serialize(ms, message);
                            NetworkTransport.Send(socketId, connectionId, myUnreliableChannelId, ms.ToArray(), 530, out error2);
                        }
                    }
                    clientConnected = true;
                    break;
                case NetworkEventType.DataEvent:
                    Debug.Log("Data Received");
                    NetworkMessage data;
                    bf = new BinaryFormatter();
                    using (MemoryStream ms = new MemoryStream(recBuffer)){
                        data = bf.Deserialize(ms) as NetworkMessage;
                    }
                    switch (data.type){
                        case 0:
                            //leftController.GetComponent<ControllerGrabObject>().RemoveFire();
						    //rightController.GetComponent<ControllerGrabObject>().RemoveFire();
                            break;
                        case 1:
                            //leftController.GetComponent<ControllerGrabObject>().SpawnFire();
						    //rightController.GetComponent<ControllerGrabObject>().SpawnFire();
                            break;
                        case 2:
                            Debug.Log("Trying to throw ball");
                            throwBall.GetComponent<shootBall>().throwBall(data.prefabId.Value); //shootBall.cs is on camera(eyes) shoots ball in direction facing.
                            break;
                        case 3:
                            wallDemo.GetComponent<wallDemo>().spawnWall();
                            break;
                        case 4:
                            wallDemo.GetComponent<wallDemo>().demolishWall();
                            break;
                        case 5:
                            spooker.GetComponent<Spook>().spookPlayer();
                            break;
                    }
                    break;
                case NetworkEventType.DisconnectEvent: //AR disconnects
                    networkInitialised = false;
					Debug.Log("Disconnect Received");
                    break;
            }
            if (clientConnected && Input.GetKeyDown(KeyCode.U)) {
                Debug.Log("Trying to update client on " + messageQueue.Count + " objects");
                for (int i = 0; i < messageQueue.Count; i++) {
                    GameObject gameObject = messageQueue.Dequeue() as GameObject;
                    int id = gameObject.GetComponent<NetworkIdentity>().getObjectId();
                    NetworkMessage message = new NetworkMessage(3, id, null, gameObject.transform);
                    byte error2;
                    bf = new BinaryFormatter();
                    using (MemoryStream ms = new MemoryStream()) {
                        bf.Serialize(ms, message);
                        NetworkTransport.Send(socketId, this.connectionId, myUpdateChannelId, ms.ToArray(), 530, out error2);
                    }
                }
                //finished update, save timer val
                timer = Time.timeSinceLevelLoad;
            }
            if (clientConnected) {
                //also send the transform of the tracker over the state channel
                Debug.Log("Sending tracker info");
                NetworkMessage message = new NetworkMessage(6, null, null, tracker.transform);
                byte error2;
                bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream()) {
                    bf.Serialize(ms, message);
                    NetworkTransport.Send(socketId, this.connectionId, myStateChannelId, ms.ToArray(), 530, out error2);
                }
            }
        }
	}

    //Add object to the list
    public void addObject(GameObject gameObject, Prefabs.PID objectType) {
        int id = gameObject.GetComponent<NetworkIdentity>().getObjectId();
        networkedObjects.Add(id, gameObject);
        NetworkMessage message = new NetworkMessage(1, id, (int)objectType, gameObject.transform);
        byte error2;
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream()) {
            bf.Serialize(ms, message);
            NetworkTransport.Send(socketId, connectionId, myUnreliableChannelId, ms.ToArray(), 530, out error2);
        }
        
    }

    public class Coder{
        
        private byte[] buff;
        private int count;
        
        public Coder(int size){
            buff = new byte[size];
            count = 0;
        }

        public Coder(byte[] array) {
            buff = array;
            count = buff[0];
        }

        public void addSerial(byte type, int id, int prefabId, Transform transform){
            int index = 1 + (count++*49);
            buff[index] = type;
            writeIn(id, index+1);
            writeIn(prefabId, index+5);
            
            //setup for position
            Vector3 pos = transform.position;
            writeIn(pos.x, index+9);
            writeIn(pos.y, index+13);
            writeIn(pos.z, index+17);

            //setup for rotation
            Quaternion rot = transform.rotation;
            writeIn(rot.x, index+21);
            writeIn(rot.y, index+25);
            writeIn(rot.z, index+29);
            writeIn(rot.w, index+33);
            
            //setup for velocity
            Vector3 vel = transform.GetComponent<Rigidbody>().velocity;
            writeIn(vel.x, index+37);
            writeIn(vel.y, index+41);
            writeIn(vel.z, index+45);
            
        }

        public void writeIn(int value, int pos) {
            byte[] writeIn = BitConverter.GetBytes(value);
            for (int i = 0; i < writeIn.Length; i++) {
                buff[i+pos] = writeIn[i];
            }
        }

        public void writeIn(float value, int pos) {
            byte[] writeIn = BitConverter.GetBytes(value);
            for (int i = 0; i < writeIn.Length; i++) {
                buff[i+pos] = writeIn[i];
            }
        }

        public Transform GetTransform(int index) {
            //we already have the array
            Transform transform;
        }

    }

    [System.Serializable]
    public class NetworkMessage{
        
		public byte type;
        public int? id;
        public int? prefabId;

        public SerializeableTransform transform;

		public NetworkMessage(byte type, int? id, int? prefabId, Transform transform){
			this.type = type;
			this.id = id;
			this.prefabId = prefabId;
			this.transform = new SerializeableTransform(transform); 

		}

    }

    [System.Serializable]
    public class SerializeableTransform{
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;


        public SerializeableTransform(Transform transform){
            posX = transform.position.x;
            posY = transform.position.y;
            posZ = transform.position.z;
            rotX = transform.rotation.x;
            rotY = transform.rotation.y;
            rotZ = transform.rotation.z;
            rotW = transform.rotation.w;
        }

        public override string ToString(){
            return "x = " + posX + "\ny = " + posY + "\nz = " + posZ;
        }

    }
}

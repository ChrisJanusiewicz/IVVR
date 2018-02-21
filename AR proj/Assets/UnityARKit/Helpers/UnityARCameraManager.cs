using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class UnityARCameraManager : MonoBehaviour {

    public Camera m_camera;
    private UnityARSessionNativeInterface m_session;

	private Material savedClearMaterial;

	[Header("AR Config Options")]

	public UnityARAlignment startAlignment = UnityARAlignment.UnityARAlignmentGravity;
	public UnityARPlaneDetection planeDetection = UnityARPlaneDetection.None;

	//htcTrackerRelayEnabled = true => Unity camera pose will be set to tracker pose every frame
	public bool htcTrackerRelayEnabled = false;
	//htcTrackerOffsetEnabled = true => Unity camera pose will be set to ARKit pose + offset
	//This offset will be calculated upon calibration whenever the user chooses to calibrate.
	public bool htcTrackerOffsetEnabled = true;

	public bool getPointCloud = true;
	public bool enableLightEstimation = true;

	//These may be used later if camera control (setting position and rotation equal to the tracker infromation) is done through this class
	public Vector4 tracker_position = new Vector4 (0, 0, 0, 1);
	public Quaternion tracker_rotation = new Quaternion (1, 0, 0, 0); // 0,1,0,0 is equal to pi rotation around X axis
	public Vector4 arkit_position;
	public Quaternion arkit_rotation;
	private Vector4 offset_position = new Vector4 (0, 0, 0, 0);
	private Quaternion offset_rotation = new Quaternion ();
	private bool tracker_updated = false;



	// Use this for initialization
	void Start () {

		m_session = UnityARSessionNativeInterface.GetARSessionNativeInterface();
	
#if !UNITY_EDITOR
		Application.targetFrameRate = 60;
        ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration();
		config.planeDetection = planeDetection;
		config.alignment = startAlignment;
		config.getPointCloudData = getPointCloud;
		config.enableLightEstimation = enableLightEstimation;
        m_session.RunWithConfig(config);

		if (m_camera == null) {
			m_camera = Camera.main;
		}
#else
		ARKitSessionConfiguration sessionConfig = new ARKitSessionConfiguration (startAlignment, true, true);
		m_session.RunWithConfig(sessionConfig);

		//put some defaults so that it doesnt complain
		UnityARCamera scamera = new UnityARCamera ();
		scamera.worldTransform = new UnityARMatrix4x4 (new Vector4 (1, 0, 0, 0), new Vector4 (0, 1, 0, 0), new Vector4 (0, 0, 1, 0), new Vector4 (0, 0, 0, 1));
		Matrix4x4 projMat = Matrix4x4.Perspective (60.0f, 1.33f, 0.1f, 30.0f);
		scamera.projectionMatrix = new UnityARMatrix4x4 (projMat.GetColumn(0),projMat.GetColumn(1),projMat.GetColumn(2),projMat.GetColumn(3));

		UnityARSessionNativeInterface.SetStaticCamera (scamera);

#endif
	}

	public void SetCamera(Camera newCamera)
	{
		if (m_camera != null) {
			UnityARVideo oldARVideo = m_camera.gameObject.GetComponent<UnityARVideo> ();
			if (oldARVideo != null) {
				savedClearMaterial = oldARVideo.m_ClearMaterial;
				Destroy (oldARVideo);
			}
		}
		SetupNewCamera (newCamera);
	}

	private void SetupNewCamera(Camera newCamera)
	{
		m_camera = newCamera;

        if (m_camera != null) {
            UnityARVideo unityARVideo = m_camera.gameObject.GetComponent<UnityARVideo> ();
            if (unityARVideo != null) {
                savedClearMaterial = unityARVideo.m_ClearMaterial;
                Destroy (unityARVideo);
            }
            unityARVideo = m_camera.gameObject.AddComponent<UnityARVideo> ();
            unityARVideo.m_ClearMaterial = savedClearMaterial;
        }
	}

	// Update is called once per frame


	//Edited by Chris 21/01/2018
	void Update () {
		
        if (m_camera != null)
        {
			//This is the original way of retrieving position and rotation from UnityARSessionNativeInterface
			//m_session.GetCameraPose(); retrieves the unaltered pose as estimated by ARKit.
			//Altering the pose here, in UnityARCameraManager, is simpler than attempting to do so in
			//UnityARSessionNativeInterface, as it does not require the position and rotation information to
			//be injected into the world transform matrix. 

            Matrix4x4 matrix = m_session.GetCameraPose();
			arkit_position = UnityARMatrixOps.GetPosition (matrix);
			arkit_rotation = UnityARMatrixOps.GetRotation (matrix);

			//It is instead possible to attempt to perform all camera adjustments below:

			Vector4 unityCameraPosition;
			Quaternion unityCameraRotation;

			if (htcTrackerRelayEnabled) {
				unityCameraPosition = tracker_position;
				unityCameraRotation = tracker_rotation;
			} else if (htcTrackerOffsetEnabled && tracker_updated) {
				//Take care to multiply offset_rotation * tracker_rotation, and not other way around
				//This is because quaternion multiplication is not commutative.
				unityCameraPosition = offset_position + arkit_position;
				unityCameraRotation = offset_rotation * arkit_rotation;
			} else {
				////Setting the Unity camera pose to purely ARKit's pose estimation
				unityCameraPosition = arkit_position;
				unityCameraRotation = arkit_rotation;
			}

			m_camera.transform.localPosition = unityCameraPosition;
			m_camera.transform.localRotation = unityCameraRotation;

			//TODO: Check whether the projectionMatrix is influenced by localposition and rotation
			//(But it shouldn't be, and we should not need to alter it)
			m_camera.projectionMatrix = m_session.GetCameraProjection ();
        }

	}

	//calls the appropriate UnityARSessionNativeInterface method which attempts 
	//to calibrate the camera using the last known tracker position and rotation.
	//It would make sense to update tracker position before performing the offset calculations.
	//Therefore when the user presses the calibrate button, tracker pose should be sent to the AR device,
	//Followed by this routine being called. Otherwise outdated tracker pose may be used.
	public void calibrate() {
		//First retrieve ARKit's last world transform's position and rotation.
		Matrix4x4 matrix = m_session.GetCameraPose();

		Vector4 lastPosARKit = UnityARMatrixOps.GetPosition (matrix);
		Quaternion lastRotARKit = UnityARMatrixOps.GetRotation (matrix);

		//The offsets for position and rotation are trivially calculated.
		//Make sure tracker_position and lastPosARKit both have w set to 1.
		offset_position = tracker_position - lastPosARKit;
		offset_rotation = tracker_rotation * Quaternion.Inverse(lastRotARKit);


	}

	public void updateTrackerPosition(Vector4 pos) {
		tracker_position = pos;
	}

	public void updateTrackerRotation(Quaternion rot) {
		tracker_updated = true;
		tracker_rotation = rot;
	}
	public Vector4 getARKitPosition(){
		return arkit_position;
	}

	public Quaternion getARKitRotation(){
		return arkit_rotation;
	}


	public Vector3 getUnityCameraPosition() {
		if (m_camera != null) {
			return m_camera.transform.localPosition;
		}
		return new Vector3();
	}
	public Quaternion getUnityCameraRotation() {
		if (m_camera != null) {
			return m_camera.transform.localRotation;
		}
		return new Quaternion();
	}

	public Transform GetUnityCameraTransform() {
		if (m_camera != null) {
			return m_camera.transform;
		} else {
			Debug.Log("Cannot retrieve unity camera transform; main camera is null.");
			return null;
			//Return something other than null.
			//Transform does not have constructors...
		}
	}
	/*
	//This returns the actual position that the unity camera is set to,
	//As opposed to the position and rotation known to ARKit.
	//This will be equal to the position and rotation known to ARKit if tracker/offset
	//position and rotation are injected in ARCameraManager as opposed to ARNativeSessionInterface
	public Vector3 getEnginePosition(){
		if (m_camera != null) {
			Matrix4x4 matrix = m_session.GetCameraPose ();
			return m_camera.transform.localPosition = UnityARMatrixOps.GetPosition (matrix);
		}
		return new Vector3 ();
	}

	//See getEnginePosition()
	public Quaternion getEngineRotation(){
		if (m_camera != null) {
			Matrix4x4 matrix = m_session.GetCameraPose ();
			return m_camera.transform.localRotation = UnityARMatrixOps.GetRotation (matrix);
		}
		return new Quaternion ();
	}
	*/

	/*
	// Getters and setters in case htcTrackerEnabled is changed to be a private variable.

	public void setHtcTracker(bool htcTrackerEnabled) {
		this.htcTrackerEnabled = htcTrackerEnabled;
	}
	public bool getHtcTracker() {
		return htcTrackerEnabled;
	}
	*/
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimSpawn : MonoBehaviour {
    
    public bool aiming;
    public bool activate;

    private GameObject trackedObj;
    public GameObject aimPrefab;
    public Grid grid;
    private GameObject aim;
    private Transform aimTransform;

    public LayerMask spawnerMask;
    private GameObject spawning;
    private Cube spawnFrom;

    // Use this for initialization
    void Start () {
        aim = Instantiate(aimPrefab);
        aimTransform = aim.transform;
    }
	
	// Update is called once per frame
	void Update () {
        try
        {
            if (aiming)
            {
                RaycastHit hit;


                if (Physics.Raycast(trackedObj.transform.position, transform.forward, out hit, 100,
                        spawnerMask))
                {
                    ShowAim(hit);
                    if (spawning != null)
                        spawning.GetComponent<MeshRenderer>().enabled = false;
                    spawning = hit.transform.gameObject;
                    spawning.GetComponent<MeshRenderer>().enabled = true;
                    spawnFrom = hit.transform.GetComponentInParent<Cube>();
                }
            }
            else
            {
                aim.SetActive(false);
            }

            //Only allow pulling objects when game is active
            if (activate && spawning != null)
            {
                // TODO spawn the object
                // choose which of the sides it is
                if (spawning == spawnFrom.right)            grid.AddCube(spawnFrom.GetX() + 1, spawnFrom.GetY(), spawnFrom.GetZ());
                else if (spawning == spawnFrom.left)       grid.AddCube(spawnFrom.GetX() - 1, spawnFrom.GetY(), spawnFrom.GetZ());
                else if (spawning == spawnFrom.front)       grid.AddCube(spawnFrom.GetX(), spawnFrom.GetY(), spawnFrom.GetZ() + 1);
                else if (spawning == spawnFrom.back)        grid.AddCube(spawnFrom.GetX(), spawnFrom.GetY(), spawnFrom.GetZ() - 1);
                else if (spawning == spawnFrom.above)       grid.AddCube(spawnFrom.GetX(), spawnFrom.GetY() + 1, spawnFrom.GetZ());
                else if (spawning == spawnFrom.below)        grid.AddCube(spawnFrom.GetX(), spawnFrom.GetY() - 1, spawnFrom.GetZ());
            }
        }
        catch (MissingReferenceException e)
        {

        }
    }


/* 
    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.controllerIndex); }
    }

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedController>();
    }*/

    private void ShowAim(RaycastHit hit)
    {
        aim.SetActive(true);

        aimTransform.position = Vector3.Lerp(trackedObj.transform.position, hit.point, .5f);
        aimTransform.LookAt(hit.point);
        aimTransform.localScale = new Vector3(aimTransform.localScale.x, aimTransform.localScale.y,
            hit.distance);

    }

}

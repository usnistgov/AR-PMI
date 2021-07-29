using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoOffset : MonoBehaviour
{

    [SerializeField] GameObject toOffset;

    [SerializeField] GameObject target;

    private SkinnedMeshRenderer skinRend;

    private MeshFilter meshFilter;

    [SerializeField] private Vector3 diff;
    // Start is called before the first frame update
    void Start()
    {
        
        


    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButton("PlayAnim")){
            toOffset = this.gameObject;if (this.GetComponent<SkinnedMeshRenderer>()){
                skinRend = GetComponent<SkinnedMeshRenderer>();
                }
            
            if (this.GetComponent<MeshFilter>()){
                meshFilter = GetComponent<MeshFilter>();
            }

            Vector3 meshcenter = skinRend.bounds.center;
            Vector3 targetcenter = target.GetComponent<MeshRenderer>().bounds.center;

            diff = targetcenter - meshcenter;

                this.transform.position += diff;
            }

        
        
    }
}

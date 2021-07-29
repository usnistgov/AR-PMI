using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateTransform : MonoBehaviour
{
    [SerializeField] private Transform bone;
    [SerializeField] private Transform offset;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate(){

        this.transform.localPosition = bone.position ;
        this.transform.rotation = bone.rotation;
        
    }
}

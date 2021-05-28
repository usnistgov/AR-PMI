using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnnotationTrigger : MonoBehaviour
{
    ReadX3D x3dScript;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnMouseDown()
    {
        Debug.Log("TRIGGERED " + this.gameObject.name);
        GameObject view = this.gameObject.transform.parent.parent.gameObject;

        x3dScript = view.GetComponent<ReadX3D>();

        string[] splitName = this.gameObject.name.Split('|');
        int index = -1;
        if (splitName.Length > 2)
            index = x3dScript.FindAnnotation(splitName[2]);

        if(index != -1)
            try
            {
                x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.SetOverrideTag("RenderType", "Cutout");
            }
            catch
            {

            }
            
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnnotationTrigger : MonoBehaviour
{
    ReadX3D x3dScript;
    // Start is called before the first frame update
    void Start()
    {
        GameObject view = this.gameObject.transform.parent.parent.gameObject;

        x3dScript = view.GetComponent<ReadX3D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnMouseDown()
    {
        Debug.Log("TRIGGERED " + this.gameObject.name);


        string[] splitName = this.gameObject.name.Split('|');
        //int index = -1;
        List<int> indexes = new List<int>();
        if (splitName.Length > 2)
            indexes = FindAnnotations(splitName[2]);
        else if (splitName.Length == 1)
            indexes = FindSurfaces(splitName[0]);

        foreach (int index in indexes)
        {
            /*if(x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.shader == Shader.Find("Unlit/Color"))
            {
                x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.shader = Shader.Find("Standard");
            }
            else
            {
                x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.shader = Shader.Find("Unlit/Color");
            }*/
            if(x3dScript.annotationList[index].surface != null)
                if (x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.renderQueue == 3000)
                {
                    x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.renderQueue = 1;

                    ShowAnnotations();
                }
                else
                {
                    x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.renderQueue = 3000;

                    HideAnnotations(index);
                }
            //x3dScript.annotationList[index].surface.GetComponent<MeshRenderer>().material.SetOverrideTag("RenderType", "Cutout");
        }
    }
    List<int> FindAnnotations(string annotationName)
    {
        List<int> indexes = new List<int>();
        for (int i = 0; i < x3dScript.annotationList.Count; i++)
        {
            if (x3dScript.annotationList[i].name.ToUpper().Trim() == annotationName.ToUpper().Trim())
            {
                indexes.Add(i);
            }
        }

        return indexes;
    }
    List<int> FindSurfaces(string annotationName)
    {
        List<int> indexes = new List<int>();
        for (int i = 0; i < x3dScript.annotationList.Count; i++)
        {
            if (x3dScript.annotationList[i].surface != null)
                if (x3dScript.annotationList[i].surface.gameObject.name.ToUpper().Trim() == annotationName.ToUpper().Trim())
                {
                    indexes.Add(i);
                }
        }

        return indexes;
    }

    void HideAnnotations(int exceptionIndex)
    {
        for (int i = 0; i < x3dScript.annotationList.Count; i++)
        {
            if (i != exceptionIndex)
            {
                x3dScript.annotationList[i].annotationObject.SetActive(false);
                //annotationList[i].surface.SetActive(false);
                if(x3dScript.annotationList[i].surface != null)
                    x3dScript.annotationList[i].surface.GetComponent<MeshRenderer>().material.renderQueue = 1;
            }

        }
    }

    void ShowAnnotations()
    {
        for (int i = 0; i < x3dScript.annotationList.Count; i++)
        {
            x3dScript.annotationList[i].annotationObject.SetActive(true);
        }
    }
}

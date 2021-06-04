using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Annotation
{
    public string id { get; set; }
    public string description { get; set; }
    public string name { get; set; }

    public GameObject annotationObject { get; set; }
    public GameObject surface { get; set; }

    public Annotation(string id, string description, string name, GameObject annotationObject)
    {
        this.id = id;
        this.description = description;
        this.name = name;
        this.annotationObject = annotationObject;
    }
    public void SetSurface(GameObject surface)
    {
        this.surface = surface;
    }

    override public string ToString()
    {
        if (surface != null)
            return "Id: " + id + ", Description: " + description + ", Name: " + name + ", GameObject: " + annotationObject.name + ", Surface: " + surface.name;
        else
            return "Id: " + id + ", Description: " + description + ", Name: " + name + ", GameObject: " + annotationObject.name;
    }
}

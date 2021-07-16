using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Text.RegularExpressions;
using System;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine.Networking;

public class ReadX3D : MonoBehaviour
{
    public float lineWidth = 0.5f;
    public string x3dFile = "NIST_MTC_CRADA_PLATE_REV-A-sfa.x3d";
    public bool flipYZ = true, combineSubmeshes = true, saveMeshes = false, addMeshColliders = true;
    IDictionary<string, string> dictionary = new Dictionary<string, string>();
    IDictionary<string, Material> matDictionary = new Dictionary<string, Material>();
    public GameObject scaleToTarget;
    int assetIncrement = 0;
    public List<Annotation> annotationList = new List<Annotation>();

    void Start()
    {
        string filePath = Application.streamingAssetsPath + "/X3D/" + x3dFile;


        string xml = ReadXML(filePath);
        ParseXML(xml);

        ScaleToTarget(); //TODO: test this more.
    }

    #region XML Parsing
    string ReadXML(string filePath)
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(filePath);
        unityWebRequest.SendWebRequest();
        while (!unityWebRequest.isDone)
        {
            //Waiting for request
        }

        return unityWebRequest.downloadHandler.text;
    }
    void ParseXML(string xml)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlNodeList switchList;

        xmlDoc.LoadXml(xml);
        switchList = xmlDoc.SelectNodes("X3D/Scene/Switch");

        foreach (XmlNode switchNode in switchList)
        {
            GameObject viewObject = ParseSwitchNode(switchNode);

            viewObject.transform.SetParent(this.gameObject.transform);
            
            
        }
    }

    /* Description: Creates and returns a GameObject for each <Switch> element */
    GameObject ParseSwitchNode(XmlNode switchNode)
    {
        XmlNodeList groupList, transformList;
        string switchId = "Switch";

        if(switchNode.Attributes["id"] != null)
        {
            switchId = switchNode.Attributes["id"].Value;
        }

        GameObject viewObject = new GameObject(switchId);

        groupList = switchNode.SelectNodes("Group");
        foreach (XmlNode groupNode in groupList)
        {
            ParseGroupNode(groupNode, viewObject, switchId);
        }

        viewObject.transform.SetParent(this.gameObject.transform);
        ResetTransform(viewObject);

        transformList = switchNode.SelectNodes("Transform");
        foreach (XmlNode transformNode in transformList)
        {
            ParseTransformNode(transformNode, viewObject, switchId);
        }

        //viewObject.transform.SetParent(this.gameObject.transform);

        //ResetTransform(viewObject);

        return viewObject;
    }

    void ParseGroupNode(XmlNode groupNode, GameObject viewObject, string parentSwitchId)
    {
        XmlNodeList shapeNodeList, groupNodeList, switchList, transformNodeList;
        List<CombineInstance> combineList = new List<CombineInstance>();
        List<Material> materialList = new List<Material>();
        string groupId = "Group";
        //int triangleCount = 0;

        if (groupNode.Attributes["id"] != null)
            groupId = groupNode.Attributes["id"].Value;

        switchList = groupNode.SelectNodes("Switch");
        foreach (XmlNode switchNode in switchList)
        {
            GameObject nestedViewObject = ParseSwitchNode(switchNode);
            nestedViewObject.transform.SetParent(viewObject.transform);
            ResetTransform(nestedViewObject);
        }

        groupNodeList = groupNode.SelectNodes("Group");
        foreach (XmlNode nestedGroupNode in groupNodeList)
        {
            ParseGroupNode(nestedGroupNode, viewObject, parentSwitchId);
        }

        transformNodeList = groupNode.SelectNodes("Transform");
        foreach (XmlNode transformNode in transformNodeList)
        {
            ParseTransformNode(transformNode, viewObject, parentSwitchId);
        }

        shapeNodeList = groupNode.SelectNodes("Shape");
        foreach (XmlNode shapeNode in shapeNodeList)
        {
            Tuple<Material, Mesh> shape = ParseShapeNode(shapeNode);

            if (combineSubmeshes)
            {
                AddToCombineList(combineList, materialList, shape, viewObject);
            }
            else
            {
                GenerateGeometryObject(groupId, shape, viewObject, Vector3.one, Vector3.zero, new Quaternion(0, 0, 0, 0));
            }
        }
        if (combineSubmeshes && combineList.Count > 0)
        {
            GenerateCombinedGeometryObject(groupId, parentSwitchId, materialList, combineList, viewObject, Vector3.one, Vector3.zero, new Quaternion(0, 0, 0, 0));
        }

        //ResetTransform(viewObject);
    }

    void ParseTransformNode(XmlNode transformNode, GameObject viewObject, string parentSwitchId)
    {
        XmlNodeList shapeList, groupList, switchList;
        List<CombineInstance> combineList = new List<CombineInstance>();
        List<Material> materialList = new List<Material>();
        Vector3 scale = new Vector3(1, 1, 1), translation = new Vector3(0, 0, 0);
        Quaternion rotation = new Quaternion(0, 0, 0, 0);
        string transformId = "Transform";
        

        if (transformNode.Attributes["id"] != null)
        {
            transformId = transformNode.Attributes["id"].Value;
        }

        if (transformNode.Attributes["scale"] != null)
        {
            string scaleStr = transformNode.Attributes["scale"].Value;
            scale = StringToVector3(scaleStr);
        }

        if (transformNode.Attributes["rotation"] != null)
        {
            string rotationStr = transformNode.Attributes["rotation"].Value;
            rotation = StringToAxisAngles(rotationStr);
        }

        if (transformNode.Attributes["translation"] != null)
        {
            string translationStr = transformNode.Attributes["translation"].Value;
            translation = StringToVector3(translationStr);
        }


        groupList = transformNode.SelectNodes("Group");
        foreach (XmlNode groupNode in groupList)
        {
            ParseGroupNode(groupNode, viewObject, parentSwitchId);
        }

        shapeList = transformNode.SelectNodes("Shape");
        foreach (XmlNode shapeNode in shapeList)
        {
            Tuple<Material, Mesh> shape = ParseShapeNode(shapeNode);

            if (combineSubmeshes)
            {
                AddToCombineList(combineList, materialList, shape, viewObject);
            }
            else
            {
                GenerateGeometryObject(transformId, shape, viewObject, scale, translation, rotation);
            }
        }
        if (combineSubmeshes && combineList.Count > 0)
        {
            GenerateCombinedGeometryObject(transformId, parentSwitchId, materialList, combineList, viewObject, scale, translation, rotation);
        }

        switchList = transformNode.SelectNodes("Switch");
        foreach (XmlNode switchNode in switchList)
        {
            GameObject nestedViewObject = ParseSwitchNode(switchNode);
            nestedViewObject.transform.SetParent(this.gameObject.transform);
            ResetTransform(nestedViewObject, scale, translation, rotation);
        }

        ResetTransform(viewObject, scale, translation, rotation);
    }

    void AddToCombineList(List<CombineInstance>combineList, List<Material> materialList, Tuple<Material, Mesh> shape, GameObject viewObject)
    {
        CombineInstance combineInstance = new CombineInstance();
        materialList.Add(shape.Item1);
        combineInstance.mesh = shape.Item2;
        combineInstance.transform = viewObject.transform.localToWorldMatrix;
        combineList.Add(combineInstance);
    }

    GameObject GenerateGeometryObject(string objectId, Tuple<Material, Mesh> shape, GameObject viewObject, Vector3 scale, Vector3 translation, Quaternion rotation) 
    {
        GameObject geometryObject;

        geometryObject = new GameObject(objectId);
        geometryObject.AddComponent<MeshFilter>().mesh = shape.Item2;
        geometryObject.AddComponent<MeshRenderer>().material = shape.Item1;
        geometryObject.transform.SetParent(viewObject.transform);

        ResetTransform(geometryObject, scale, translation, rotation);
        //viewObject.transform.SetParent(this.gameObject.transform);
        ResetTransform(viewObject, scale, translation, rotation);

        return geometryObject;
    }

    GameObject GenerateCombinedGeometryObject(string objectId, string parentSwitchId, List<Material> materialList, List<CombineInstance> combineList, GameObject viewObject, Vector3 scale, Vector3 translation, Quaternion rotation)
    {
        GameObject geometryObject;
        Mesh combinedMesh = new Mesh();

        geometryObject = new GameObject(objectId);
        string[] splitObjectId = objectId.Split('|');

        if (ContainsDistinct(materialList))
        {
            //Don't combine submeshes and use different materials
            combinedMesh.CombineMeshes(combineList.ToArray(), false);
            geometryObject.AddComponent<MeshRenderer>().materials = materialList.ToArray();
        }
        else
        {
            //Combine submeshes and use a single material
            combinedMesh.CombineMeshes(combineList.ToArray(), true);
            geometryObject.AddComponent<MeshRenderer>().material = materialList[0];
        }

        //TODO: Unity has a limit to the number of vertices (65534) and triangles a mesh can have. The all annotations view exceeds this. This might be the issue.
        //Debug.Log("Individual: " + triangleCount + " Combined: " + combinedMesh.triangles.Length);
        //Debug.Log(combinedMesh.vertices.Length);
        //combinedMesh.RecalculateNormals();

        geometryObject.AddComponent<MeshFilter>().mesh = combinedMesh;

        geometryObject.transform.SetParent(viewObject.transform);

        // ADD MESH COLLIDERS. DO NOT ADD COLLIDERS TO PART GEOMETRY
        if (addMeshColliders && parentSwitchId != "geometrySwitch")
        {
            geometryObject.AddComponent<MeshCollider>();
            geometryObject.AddComponent<AnnotationTrigger>();
        }
        else if (parentSwitchId == "geometrySwitch")
        {
            geometryObject.GetComponent<MeshRenderer>().material.renderQueue = 2000;
        }

        ResetTransform(geometryObject, scale, translation, rotation);
        //viewObject.transform.SetParent(this.gameObject.transform);
        ResetTransform(viewObject, scale, translation, rotation);

        if (splitObjectId.Length > 2 && parentSwitchId.Contains("swView")) //TODO: talk to Bob about this. Is this reliable?
        {
            annotationList.Add(new Annotation(splitObjectId[0], splitObjectId[1], splitObjectId[2], geometryObject));
        }
        else if (parentSwitchId == "surfaceSwitch") //TODO: talk to Bob about this.
        {
            int index = FindAnnotation(geometryObject.name);
            if (index != -1)
            {
                annotationList[index].SetSurface(geometryObject);
            }
        }
        return geometryObject;
    }

    /* <Shape> Description
     * The Shape node has two elements, Appearance and a geometry node, which are used to create rendered objects in the world. 
     * The Appearance node specifies the visual attributes (e.g., material and texture) to be applied to the geometry. 
     * The specified geometry node (Box, Sphere, IndexedFaceSet) is rendered with the specified appearance nodes applied.
     */
    Tuple<Material, Mesh> ParseShapeNode(XmlNode shapeNode)
    {
        XmlNode appearanceNode, indexedLineSetNode, indexedFaceSetNode;
        Mesh geometry = new Mesh();
        Material appearance;

        appearanceNode = shapeNode.SelectSingleNode("Appearance");
        if (appearanceNode != null)
        {
            appearance = ParseAppearanceNode(appearanceNode);
        }
        else
        {
            appearance = new Material(Shader.Find("Standard"));
            Debug.Log("Expected <Appearance> node, but it was not found. Using default material.");
        }

        indexedLineSetNode = shapeNode.SelectSingleNode("IndexedLineSet");
        indexedFaceSetNode = shapeNode.SelectSingleNode("IndexedFaceSet");
        if (indexedLineSetNode != null) //Line Geometry
        {
            geometry = ParseIndexedLineSet(indexedLineSetNode);
        }
        else if (indexedFaceSetNode != null) //Face Geometry
        {
            geometry = ParseIndexedFaceSet(indexedFaceSetNode);
        }
        else //Other Geometry
        {
            //TODO: could also be box, sphere and other kinds of geometry 
            Debug.Log("Expected <IndexedLineSet> or <IndexedFaceSet> geometry but none were found. Other geometries are not currently supported.");
        }

        return new Tuple<Material, Mesh>(appearance, geometry);
    }

    /* <Appearance> Description
     * The Appearance node specifies the visual properties of a geometry.
     * An Appearance element can contain one Material node. 
     * If no Material node is given, then, lighting is off (all lights are ignored during rendering of the object that references this Appearance) 
     * and the unlit object colour is (1, 1, 1). 
     * Details of the lighting model are in 4.14, Lighting model of the VRML specs.
     */
    Material ParseAppearanceNode(XmlNode appearanceNode)
    {
        XmlNode materialNode;
        Material material;    

        if (appearanceNode.Attributes["USE"] != null)
        {
            string use = appearanceNode.Attributes["USE"].Value;

            return  matDictionary[use];
        }

        materialNode = appearanceNode.SelectSingleNode("Material");

        if (materialNode != null)
        {
            material = ParseMaterialNode(materialNode);

            if (appearanceNode.Attributes["DEF"] != null)
            {
                string def = appearanceNode.Attributes["DEF"].Value;
                matDictionary.Add(def, material);
            }
            
            return material;
        }

        return null;
    }
    /* <Material> Description
     * The Material node specifies surface material properties for associated geometry nodes and is used by the X3D lighting equations during rendering. 
     * All of the fields in the Material node range from 0.0 to 1.0.
     * HTML Encoding and Default Values:
     * <Material ambientIntensity='0.2' 
     *           diffuseColor='0.8,0.8,0.8' 
     *           emissiveColor='0,0,0' 
     *           metadata='X3DMetadataObject' 
     *           shininess='0.2' 
     *           specularColor='0,0,0' 
     *           transparency='0' ></Material>
     */
    Material ParseMaterialNode(XmlNode materialNode)
    {
        //TODO: add support for transparency X3D attribute
        //TODO: create a custom shader to fix this?
        //use diffuseColor as main color (emissive works better?)
        Color emissiveColor = new Color(0, 0, 0), diffuseColor = new Color(0.8f, 0.8f, 0.8f), specularColor = new Color(0, 0, 0);
        //float shininess = 0.2f, transparency = 0;
        string emissiveColorString = "";

        if (materialNode.Attributes["diffuseColor"] != null)
        {
            string diffuseColorString = materialNode.Attributes["diffuseColor"].Value;
            diffuseColor = ParseColor(diffuseColorString);
            //Material emissiveMat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
        }
        if (materialNode.Attributes["emissiveColor"] != null)
        {
            emissiveColorString = materialNode.Attributes["emissiveColor"].Value;
            emissiveColor = ParseColor(emissiveColorString);
        }
        if (materialNode.Attributes["specularColor"] != null)
        {
            string specularColorString = materialNode.Attributes["specularColor"].Value;
            specularColor = ParseColor(specularColorString);
        }
        if (materialNode.Attributes["shininess"] != null)
        {
            //string shininessString = materialNode.Attributes["shininess"].Value;
            //shininess = float.Parse(shininessString);
        }

        Material material;
        material = new Material(Shader.Find("Sprites/Default"));
        if (emissiveColorString != "")
        {

            material.color = emissiveColor;
        }
        else
        {
            material = new Material(Shader.Find("Standard"));
            material.color = diffuseColor;
        }

        return material;
    }

    Mesh ParseIndexedLineSet(XmlNode indexedLineSetNode)
    {
        XmlNodeList coordinateList;
        Mesh lineMesh;
        string coordIndexString = "", lineCoordinatesString = "";

        coordIndexString += indexedLineSetNode.Attributes["coordIndex"].Value;

        coordinateList = indexedLineSetNode.SelectNodes("Coordinate");

        foreach (XmlNode coordinateNode in coordinateList)
        {
            if (coordinateNode.Attributes["point"] != null)
            {
                lineCoordinatesString += coordinateNode.Attributes["point"].Value;

                if (coordinateNode.Attributes["DEF"] != null)
                {
                    string def = coordinateNode.Attributes["DEF"].Value;
                    dictionary.Add(def, lineCoordinatesString);
                }
            }
            else if (coordinateNode.Attributes["USE"] != null)
            {
                string use = coordinateNode.Attributes["USE"].Value;
                lineCoordinatesString += dictionary[use];
            }
        }
        lineMesh = GenerateLineMesh(lineCoordinatesString, coordIndexString);

        return lineMesh;
    }

    Mesh ParseIndexedFaceSet(XmlNode indexedFaceSetNode)
    {
        XmlNodeList coordinateList;
        Mesh faceMesh;
        string coordIndexString = "", faceCoordinatesString = "";

        coordIndexString += indexedFaceSetNode.Attributes["coordIndex"].Value;

        coordinateList = indexedFaceSetNode.SelectNodes("Coordinate");

        foreach (XmlNode coordinateNode in coordinateList)
        {
            if (coordinateNode.Attributes["point"] != null)
            {
                faceCoordinatesString += coordinateNode.Attributes["point"].Value;

                if (coordinateNode.Attributes["DEF"] != null)
                {
                    string def = coordinateNode.Attributes["DEF"].Value;
                    dictionary.Add(def, faceCoordinatesString);
                }
            }
            else if (coordinateNode.Attributes["USE"] != null)
            {
                string use = coordinateNode.Attributes["USE"].Value;
                faceCoordinatesString += dictionary[use];
            }
        }

        if(faceCoordinatesString != "" && coordIndexString != "")
        {
            faceMesh = GenerateFaceMesh(faceCoordinatesString, coordIndexString);
        }
        else
        {
            faceMesh = new Mesh();
        }
        

        return faceMesh;
    }
    #endregion

    #region Unity geometry generation
    Mesh GenerateLineMesh(string vertices, string coordIndexes)
    {
        Vector3[] positions = ParseCoordinates(vertices);
        List<int> intIndexes = new List<int>();
        List<CombineInstance> combineList = new List<CombineInstance>();
        string[] indexes = coordIndexes.Split(' ');
        Mesh combinedMesh = new Mesh();

        for (int i = 0; i < indexes.Length; i++)
        {
            if (indexes[i] != "-1")
            {
                intIndexes.Add(int.Parse(indexes[i]));
            }
            else
            {
                GameObject auxObject = new GameObject();
                LineRenderer lineRenderer = auxObject.AddComponent<LineRenderer>();
                Mesh lineMesh = new Mesh();

                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.positionCount = intIndexes.Count;

                for (int j = 0; j < intIndexes.Count; j++)
                {
                    lineRenderer.SetPosition(j, positions[intIndexes[j]]);
                }

                lineRenderer.BakeMesh(lineMesh);

                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = lineMesh,
                    transform = auxObject.transform.localToWorldMatrix
                };
                combineList.Add(combineInstance);

                Destroy(auxObject);

                intIndexes.Clear();
            }
        }

        combinedMesh.CombineMeshes(combineList.ToArray());

        return combinedMesh;
    }
    Mesh GenerateFaceMesh(string vertices, string triangles)
    {
        Vector3[] verticesArray = ParseCoordinates(vertices);

        string[] sArray = triangles.Split(' ');
        List<int> trianglesList = new List<int>();

        for (int i = 0; i < sArray.Length; i++)
        {
            if (int.Parse(sArray[i]) != -1)
            {
                trianglesList.Add(int.Parse(sArray[i]));
            }
        }

        if (flipYZ)
        {
            for (int i = 0; i < trianglesList.Count; i += 3)
            {
                int t = trianglesList[i + 1];
                trianglesList[i + 1] = trianglesList[i + 2];
                trianglesList[i + 2] = t;
            }
        }

        //for(int i =0; i< trianglesList.Count; i++)
        //{
        //    Debug.Log(trianglesList[i]);
        //}

        Mesh mesh = new Mesh
        {
            vertices = verticesArray,
            triangles = trianglesList.ToArray()
        };
        mesh.RecalculateNormals();

        return mesh;
    }

    #endregion

    #region Helper functions
    Vector3[] ParseCoordinates(string input)
    {
        string[] sArray = input.Split(' ');
        Vector3[] array = new Vector3[sArray.Length / 3];
        if (sArray.Length >= 3)
        {
            int j = 0;
            for (int i = 0; i < sArray.Length; i += 3)
            {
                if (flipYZ)
                {
                    array[j] = new Vector3(float.Parse(sArray[i]),
                                           float.Parse(sArray[i + 2]),
                                           float.Parse(sArray[i + 1]));
                }
                else
                {
                    array[j] = new Vector3(float.Parse(sArray[i]),
                                           float.Parse(sArray[i + 1]),
                                           float.Parse(sArray[i + 2]));
                }
                j++;
            }
        }
        else
        {
            Debug.Log("Tried to parse coordinates from string to Vector3, but there are less than 3 coordinates:" + input);
        }

        return array;
    }

    /* Description:
     * Converts a string of 3 numbers separated by spaces to a Vector3. 
     * If the string can't be parsed in exactly 3 numbers, the function returns a Vector3 with the value 0,0,0.
     */
    Vector3 StringToVector3(string input)
    {
        Vector3 vec3 = Vector3.zero;
        string[] sArray = input.Split(' ');
        if(sArray.Length == 3)
        {
            if(flipYZ)
            {
                vec3 = new Vector3(float.Parse(sArray[0]),
                                   float.Parse(sArray[2]),
                                   float.Parse(sArray[1]));
            }
            else
            {
                vec3 = new Vector3(float.Parse(sArray[0]),
                                   float.Parse(sArray[1]),
                                   float.Parse(sArray[2]));
            }
        }

        return vec3;
    }
    /* Description:
     * Converts a string of 4 numbers separated by spaces to a Quaternion. 
     * If the string can't be parsed in exactly 4 numbers, the function returns a Quaternion with the value 0,0,0,0.
     */
    Quaternion StringToAxisAngles(string input)
    {
        Quaternion q = new Quaternion(0, 0, 0, 0);
        string[] sArray = input.Split(' ');
        if (sArray.Length == 4)
        {
            if (flipYZ)
            {
                q = Quaternion.AngleAxis(float.Parse(sArray[3]) * Mathf.Rad2Deg * -1, new Vector3(float.Parse(sArray[0]) * Mathf.Rad2Deg,
                                                                                                  float.Parse(sArray[2]) * Mathf.Rad2Deg,
                                                                                                  float.Parse(sArray[1]) * Mathf.Rad2Deg));
            }
            else
            {
                q = Quaternion.AngleAxis(float.Parse(sArray[3]) * Mathf.Rad2Deg, new Vector3(float.Parse(sArray[0]) * Mathf.Rad2Deg,
                                                                                             float.Parse(sArray[1]) * Mathf.Rad2Deg,
                                                                                             float.Parse(sArray[2]) * Mathf.Rad2Deg));
            }
        }

        return q;
    }

    int FindAnnotation(string annotationName)
    {
        List<int> indexes = new List<int>();

        for (int i = 0; i < annotationList.Count; i++)
        {
            if (annotationList[i].name.ToUpper().Trim() == annotationName.ToUpper().Trim())
            {
                //indexes.Add(i);
                return i;
            }
        }

        return -1;
    }


    Color ParseColor(string input)
    {
        string[] sArray = input.Split(' ');
        Color color;
        if (sArray.Length >= 3)
        {
            color = new Color(float.Parse(sArray[0]),
                              float.Parse(sArray[1]),
                              float.Parse(sArray[2]));
        }
        else
        {
            color = new Color(1, 1, 1);
        }

        return color;
    }

    void ResetTransform(GameObject gameObject, Vector3 scale = new Vector3(), Vector3 position = new Vector3(), Quaternion rotation = new Quaternion())
    {
        if(scale == Vector3.zero)
            gameObject.transform.localScale =  Vector3.one;
        else
            gameObject.transform.localScale = scale;

        print("SET SCALE OF " + gameObject.name + " TO " + gameObject.transform.localScale.x);

        gameObject.transform.localPosition = position;

        gameObject.transform.rotation = rotation;
    }
    bool ContainsDistinct(List<Material> materialList)
    {
        //TODO: use different criteria for comparing materials?
        for (int i = 0; i < materialList.Count; i++)
        {
            for (int j = i + 1; j < materialList.Count; j++)
            {
                if(materialList[i] != null && materialList[j] != null)
                    if (materialList[i].color != materialList[j].color)
                        return true;
            }
        }
        return false;
    }
    #endregion

    void ScaleToTarget()
    {
        if (scaleToTarget)
        {
            if (scaleToTarget.GetComponent<MeshFilter>())
            {
                float scaleToSizeX = scaleToTarget.GetComponent<MeshFilter>().mesh.bounds.size.x; //TODO: also check y and z
                float scaleToScaleX = scaleToTarget.transform.localScale.x;

                GameObject partGeometry = GameObject.Find("swPart0").transform.GetChild(0).gameObject; //TODO: automate this

                float partSizeX = partGeometry.GetComponent<MeshFilter>().mesh.bounds.size.x;
                float partScaleX = partGeometry.transform.localScale.x;

                float scaleDifference = scaleToSizeX * scaleToScaleX / partSizeX * partScaleX;

                this.gameObject.transform.localScale = new Vector3(scaleDifference, scaleDifference, scaleDifference);
            }
            else
            {
                Debug.Log("Could not scale to target object because the object does not have a mesh.");
            }

        }
    }
}
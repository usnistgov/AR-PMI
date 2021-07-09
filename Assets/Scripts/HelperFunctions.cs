using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelperFunctions
{
    public static void ResetTransform(GameObject gameObject)
    {
        gameObject.transform.localPosition = new Vector3(0, 0, 0);
        gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }


    /*void SaveMesh(Mesh mesh, string name)
    {
        string folderName = "SavedMeshes";
        string path = "Assets/StreamingAssets";
        //Check if folder exists, if not, create it
        if (!AssetDatabase.IsValidFolder(path + "/" + folderName))
        {
            string guid = AssetDatabase.CreateFolder(path, folderName); //TODO: test more
            //Debug.Log("Assets/" + Application.streamingAssetsPath);
            if (guid != "")
                Debug.Log(guid);
            else
                Debug.Log("Failed to create SavedMeshes folder.");

        }

        //AssetDatabase.CreateAsset(mesh, path + "/" + folderName + "/" + name + assetIncrement + ".asset");
        AssetDatabase.CreateAsset(mesh, "Assets/Test/" + name + assetIncrement + ".asset");
        assetIncrement++;
    }*/
}

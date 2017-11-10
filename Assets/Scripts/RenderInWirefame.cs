using UnityEngine;
using System.Collections;

public class RenderInWirefame : MonoBehaviour {


    public bool wireframeMode = false; 


    void OnPreRender()
    {
        GL.wireframe = wireframeMode;
    }


    void OnPostRender()
    {
        GL.wireframe = wireframeMode; 
    }
}

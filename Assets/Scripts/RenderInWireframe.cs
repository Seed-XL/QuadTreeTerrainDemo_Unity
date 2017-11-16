using UnityEngine;
using System.Collections;

public class RenderInWireframe : MonoBehaviour
{
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

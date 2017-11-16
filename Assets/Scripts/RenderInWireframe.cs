using UnityEngine;
using System.Collections;

public class RenderInWireframe : MonoBehaviour
{
    public bool wireframeMode = false;

    void OnPreRender()
    {
        GL.wireframe = true;
    }
    void OnPostRender()
    {
        GL.wireframe = false;
    }
}

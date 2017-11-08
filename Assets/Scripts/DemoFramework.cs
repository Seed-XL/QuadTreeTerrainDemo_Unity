using UnityEngine;


public class DemoFramework : MonoBehaviour {

    public GameObject terrainGo;

    public int heightSize;

    //是否从高度图读取高度信息
    //True从文件读取
    //False动态生成
    public bool isLoadHeightDataFromFile;
    public string heightFileName;

    public bool isGenerateHeightDataRuntime;
    public int iterations;
    public ushort minHeightValue;
    public ushort maxHeightValue;
    public float filter; 
    
   
    //1、读取高度图，
    //2、设置顶点间距离，
    //3、读取纹理
    //4、设置光照阴影
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

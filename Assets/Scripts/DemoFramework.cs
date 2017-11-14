using UnityEngine;
using Assets.Scripts.QuadTree; 


public class DemoFramework : MonoBehaviour {

    //摄像机对象
    public GameObject cameraGo ; 
    //地形对象
    public GameObject terrainGo;

    //地形对象的Material
    public Material terrainMat; 

    //顶点间的距离
    public Vector3 vertexDistance; 
    //高度值的释放
    public float heightScale; 
    //高度图的边长,也就是结点的个数
    public int heightSize;

    //是否从高度图读取高度信息
    //True从文件读取
    //False动态生成
    public bool isLoadHeightDataFromFile;
    public string heightFileName;

    public bool isGenerateHeightDataRuntime;
    public int iterations;
    [Range(0,255)]
    public int minHeightValue;
    [Range(0, 255)]
    public int  maxHeightValue;
    [Range(0,0.9f)]
    public float filter;


    #region 地图Tile
    [Range(1,2048)]
    public int terrainTextureSize = 256; 
    public Texture2D[] tiles; 

    #endregion



    private CQuadTreeTerrain mQuadTreeTerrain; 
   
    //1、读取高度图，
    //2、设置顶点间距离，
    //3、读取纹理
    //4、设置光照阴影
	void Start ()
    {
        mQuadTreeTerrain = new CQuadTreeTerrain();

        //制造高度图
        mQuadTreeTerrain.MakeTerrainFault(heightSize,iterations,(ushort)minHeightValue, (ushort)maxHeightValue,filter);

        //设置对应的纹理块
        AddTile(enTileTypes.lowest_tile);
        AddTile(enTileTypes.low_tile);
        AddTile(enTileTypes.high_tile);
        AddTile(enTileTypes.highest_tile);

        mQuadTreeTerrain.GenerateTextureMap((uint)terrainTextureSize,(ushort)maxHeightValue,(ushort)minHeightValue);

       
	}


    private void SetTerrainTexture( Texture2D texture )
    {
        if( terrainMat != null )
        {
            terrainMat.SetTexture("_MainTex", texture); 
        }
    }



    #region 地图块操作


    void AddTile( enTileTypes tileType )
    {
        int tileIdx = (int)tileType;
        if (tileIdx < tiles.Length
            && tiles[tileIdx] != null )
        {
            mQuadTreeTerrain.AddTile((enTileTypes)tileIdx, tiles[tileIdx]);
        }
    }


    #endregion


    // Update is called once per frame
    void Update ()
    {
        //SetTerrainTexture(mQuadTreeTerrain.TerrainTexture);
    }

}

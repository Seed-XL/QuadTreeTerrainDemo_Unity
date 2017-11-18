//This code came from book 《Focus On 3D Terrain Programming》 ,thanks Trent Polack a lot
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Utility;
using System.IO;
using UnityEditor;

namespace Assets.Scripts.QuadTree
{


    #region 纹理数据 

    public enum enTileTypes
    {
        lowest_tile = 0 , 
        low_tile = 1 , 
        high_tile = 2 , 
        highest_tile = 3 , 
        max_tile = 4 ,
    }

    public class CTerrainTile
    {
        public int lowHeight;
        public int optimalHeight;
        public int highHeight;
        public enTileTypes TileType; 

        public Texture2D mTileTexture; 


        public CTerrainTile( enTileTypes tileType, Texture2D texture )
        {
            lowHeight = 0;
            optimalHeight = 0;
            highHeight = 0;

            TileType = tileType; 
            mTileTexture = texture;
        }

        public bool IsValid()
        {
            return mTileTexture != null; 
        }

    }



    



    #endregion 


    #region  高度度数据

    struct stHeightData
    {
        private ushort[,] mHeightData;
        public int mSize;
        
        public bool IsValid()
        {
            return mHeightData != null; 
        }    
        
        public void Release()
        {
            mHeightData = null;
            mSize = 0; 
        }   


        public void Allocate( int mapSize )
        {
            if( mapSize > 0 )
            {
                mHeightData = new ushort[mapSize,mapSize];
                mSize = mapSize; 
            }
        }

        public void SetHeightValue(ushort value , int x,int y)
        {
            if( IsValid() && InRange(x,y) )
            {
                mHeightData[x, y] = value;
            }
        }

        public ushort GetRawHeightValue( int x, int y )
        {
            ushort ret = 0;
            if( IsValid() && InRange(x,y))
            {
                ret = mHeightData[x, y]; 
            }
            return ret; 
        }

   


        private bool InRange( int x ,int y )
        {
            return x >= 0 && x < mSize && y >= 0 && y < mSize; 
        }
    }

    #endregion


    #region 结点定义 

    class CQuadTreeNode
    {
        public CQuadTreeNode mTopLeftNode;
        public CQuadTreeNode mTopRightNode;
        public CQuadTreeNode mBottomRightNode;
        public CQuadTreeNode mBottomLetfNode;

        public bool mbSubdivide;
        public int mIndexX ;
        public int mIndexZ ; 


        public CQuadTreeNode( int x ,int z )
        {
            mIndexX = x;
            mIndexZ = z; 
        }
    }


    enum enNodeTriFanType
    {
        Complete_Fan = 0 ,
        BottomLeft_TopRight = 5,
        BottomRight_TopLeft = 10,
        No_Fan = 15
    }



    struct stTerrainMeshData
    {
        public Mesh mMesh;
        public Vector3[] mVertices;
        public Vector2[] mUV;
        public Vector3[] mNormals; 
        public int[] mTriangles;


        private int mTriIdx;
          

        public void RenderVertex(
            int idx , 
            Vector3 vertex,
            Vector3 uv
            )
        {
            mVertices[idx] = vertex;
            mUV[idx] = uv;
            mTriangles[mTriIdx++] = idx;             
        }



        public void Reset()
        {
            if( mVertices != null )
            {
                for (int i = 0; i < mVertices.Length; ++i)
                {
                    mVertices[i].x = mVertices[i].y = mVertices[i].z = 0; 
                    if( mUV != null )
                    {
                        mUV[i].x = mUV[i].y = 0; 
                    }
                    if( mNormals != null )
                    {
                        mNormals[i].x = mNormals[i].y = 0; 
                    }
                }
            }

            mTriIdx = 0;
            if ( mTriangles != null )
            {
                for( int i = 0; i < mTriangles.Length; ++i)
                {
                    mTriangles[i] = 0; 
                }
            }
           
        }

    }




    #endregion



    class CQuadTreeTerrain
    {

        #region   核心逻辑
    
        public List<CQuadTreeNode> mNodeList = new List<CQuadTreeNode>();  

        private int GetIndex( int x, int z )
        {
            return z * mHeightData.mSize + x; 
        }

        private CQuadTreeNode GetNode( int x, int z )
        {
            int idx = GetIndex(x, z);
            return idx < mNodeList.Count ? mNodeList[idx] : null;     
        }


        public void GenerateNodes( int size )
        {
            mNodeList.Clear();
            for(int z = 0; z < size; ++z )
            {
                for(int x  = 0; x < size; ++x )
                {
                    CQuadTreeNode node = new CQuadTreeNode(x, z);
                    node.mbSubdivide = true;
                    mNodeList.Add(node);  
                }
            }    
        }

        public CQuadTreeNode RefineNode( 
            float x ,
            float z ,
            int curNodeLength ,   //暂时定为节点的个数
            Camera viewCamera ,
            Vector3 vectorScale, 
            float desiredResolution,
            float minResolution
            )
        {
            if( null == viewCamera )
            {
                Debug.LogError("[RefineNode]View Camera is Null!");
                return null; 
            }

            int tX = (int)x;
            int tZ = (int)z;

            CQuadTreeNode qtNode = GetNode(tX, tZ); 
            if( null == qtNode )
            {
                Debug.LogError( string.Format("[RefineNode]No Such Node at :{0}|{1}",tX,tZ));
                return null ; 
            }

            //评价公式
            ushort nodeHeight = GetTrueHeightAtPoint(qtNode.mIndexX, qtNode.mIndexZ); 
            float fViewDistance = Mathf.Sqrt(
                Mathf.Pow(viewCamera.transform.position.x - qtNode.mIndexX * vectorScale.x ,2)  +
                Mathf.Pow(viewCamera.transform.position.y - nodeHeight * vectorScale.y,2) +
                Mathf.Pow(viewCamera.transform.position.z - qtNode.mIndexZ * vectorScale.z,2)
                  );

            //float fDenominator = (curNodeLength * minResolution * Mathf.Max(desiredResolution * nodeHeight / 3, 1.0f));
            //float f = fViewDistance / fDenominator ;

            float fDenominator = Mathf.Max(curNodeLength *  vectorScale.x ,1.0f) ; 
            float f = fViewDistance / fDenominator;


            qtNode.mbSubdivide = f < 1.0f ? true : false;  
            if( qtNode.mbSubdivide )
            {
                if( !(curNodeLength <= 3) )
                {
                    float fChildeNodeOffset = (float)((curNodeLength - 1) >> 2);
                    int tChildNodeLength = (curNodeLength + 1) >> 1;

                    //bottom-left
                    qtNode.mBottomLetfNode = RefineNode(x - fChildeNodeOffset, z - fChildeNodeOffset, tChildNodeLength, viewCamera,vectorScale, desiredResolution, minResolution);
                    //bottom-right
                    qtNode.mBottomRightNode = RefineNode(x + fChildeNodeOffset, z - fChildeNodeOffset, tChildNodeLength, viewCamera, vectorScale, desiredResolution, minResolution);
                    //top-left 
                    qtNode.mTopLeftNode = RefineNode(x - fChildeNodeOffset, z + fChildeNodeOffset, tChildNodeLength, viewCamera, vectorScale, desiredResolution, minResolution);
                    //top-right
                    qtNode.mTopRightNode = RefineNode(x + fChildeNodeOffset, z + fChildeNodeOffset, tChildNodeLength, viewCamera, vectorScale, desiredResolution, minResolution);
                }        
            }

            return qtNode; 
        }


        #endregion



        #region  将模型渲染上去
     
        


        public void CLOD_Render( ref stTerrainMeshData meshData , Vector3 vertexScale )
        {
            meshData.Reset(); 
            float fCenter = (mHeightData.mSize - 1) >> 1;

            RenderNode(fCenter, fCenter, mHeightData.mSize,ref meshData ,vertexScale);
        }


        private Vector3  GetScaleVector3(float x,float z, Vector3 vectorScale )
        {
            return new Vector3(
                x * vectorScale.x,
                mHeightData.GetRawHeightValue((int)x,(int)z) * vectorScale.y,
                z * vectorScale.z 
                ); 
        }

        private void RenderNode( float x ,float z ,int curNodeLength ,ref stTerrainMeshData meshData, Vector3 vectorScale  )
        {
            int tHeightMapSize = mHeightData.mSize; 
            int tX = (int)x;
            int tZ = (int)z;

            CQuadTreeNode node = GetNode(tX, tZ); 
            if( null == node )
            {
                Debug.LogError(string.Format("[RenderNode]No Node at :{0}|{1}", tX, tZ));
                return; 
            }

            int iHalfNodeLength = (curNodeLength - 1) >> 1;
            float fHalfNodeLength = (curNodeLength - 1) / 2.0f;

            int tAdjOffset = curNodeLength - 1;

            float fTexLeft = Mathf.Abs(x - fHalfNodeLength) / tHeightMapSize;
            float fTexBottom = Mathf.Abs(z - fHalfNodeLength) / tHeightMapSize;
            float fTexRight = Mathf.Abs(x+ fHalfNodeLength) / tHeightMapSize;
            float fTexTop = Mathf.Abs(z + fHalfNodeLength) / tHeightMapSize;

            float fMidX = (fTexLeft + fTexRight) / 2.0f;
            float fMidZ = (fTexBottom + fTexTop) / 2.0f; 
            
            if( node.mbSubdivide )
            {
                #region 已经是最小的LOD
                
                /*
                 *   
                 *   2
                 *   1
                 * 
                 */

                //已经是最小的LOD的
                if( curNodeLength <= 3 )
                {
                    CQuadTreeNode bottomNeighborNode = GetNode(tX, tZ - tAdjOffset);
                    CQuadTreeNode rightNeighborNode =  GetNode(tX + tAdjOffset, tZ );
                    CQuadTreeNode topNeighborNode = GetNode(tX, tZ + tAdjOffset);
                    CQuadTreeNode leftNeighborNode = GetNode(tX - tAdjOffset, tZ);

                    bool bDrawBottomMidVertex = (tZ - tAdjOffset < 0) || ( bottomNeighborNode != null && bottomNeighborNode.mbSubdivide );
                    bool bDrawRightMidVertex = (tX + tAdjOffset >= tHeightMapSize) || ( rightNeighborNode != null && rightNeighborNode.mbSubdivide );
                    bool bDrawTopMidVertex = (tZ + tAdjOffset >= tHeightMapSize) || (topNeighborNode != null && topNeighborNode.mbSubdivide);
                    bool bDrawLeftMidVertex = (tX - tAdjOffset < 0) || (leftNeighborNode != null && leftNeighborNode.mbSubdivide);  

                    //center 
                    meshData.RenderVertex(
                        GetIndex(tX, tZ),
                        GetScaleVector3(x,z,vectorScale),
                        new Vector2(fMidX, fMidZ)
                        );

                    //bottom left
                    meshData.RenderVertex(
                        GetIndex(tX - iHalfNodeLength , tZ - iHalfNodeLength) ,
                        GetScaleVector3(x - fHalfNodeLength, z - fHalfNodeLength, vectorScale),
                        new Vector2(fTexLeft,fTexBottom)
                        );

                    //1、是否到了底部的边界  
                    //2、或者说底部的拆分了
                    //满足以上两个条件，均需要画出中间的点
                    

                    CQuadTreeNode neighborNode = GetNode(tX, tZ - tAdjOffset); 
                    if ( (tZ - tAdjOffset ) < 0  || (neighborNode != null && neighborNode.mbSubdivide) )
                    {
                        //bottom mid
                        meshData.RenderVertex(
                          GetIndex(tX, tZ - iHalfNodeLength),
                          GetScaleVector3(x, z - fHalfNodeLength, vectorScale),
                          new Vector2(fMidX, fTexBottom)
                          );
                    }


                    //bottom right
                    meshData.RenderVertex(
                        GetIndex(tX + iHalfNodeLength, tZ - iHalfNodeLength),
                        GetScaleVector3(x + fHalfNodeLength, z - fHalfNodeLength, vectorScale),
                        new Vector2(fTexRight, fTexBottom)
                        );


                    //1、是否到了右边的边界  
                    //2、或者说右边今结点的拆分了
                    //满足以上两个条件，均需要画出中间的点
                    neighborNode = GetNode(tX + tAdjOffset, tZ );
                    if ((tX + tAdjOffset) >= tHeightMapSize || (neighborNode != null && neighborNode.mbSubdivide))
                    {
                        //right mid
                        meshData.RenderVertex(
                          GetIndex(tX + iHalfNodeLength, tZ),
                          GetScaleVector3(x + fHalfNodeLength, z, vectorScale),
                          new Vector2(fTexRight, fMidZ)
                          );
                    }


                    //top right
                    meshData.RenderVertex(
                        GetIndex(tX + iHalfNodeLength, tZ + iHalfNodeLength),
                        GetScaleVector3(x + fHalfNodeLength, z + fHalfNodeLength, vectorScale),
                        new Vector2(fTexRight, fTexTop)
                        );


                    //1、是否到了上部的边界  
                    //2、或者说上边的邻结点的拆分了
                    //满足以上两个条件，均需要画出中间的点
                    neighborNode = GetNode(tX , tZ + tAdjOffset);
                    if ((tZ + tAdjOffset) >= tHeightMapSize || (neighborNode != null && neighborNode.mbSubdivide))
                    {
                        //top mid
                        meshData.RenderVertex(
                          GetIndex(tX , tZ + iHalfNodeLength),
                          GetScaleVector3(x , z + fHalfNodeLength, vectorScale),
                          new Vector2(fMidX, fTexTop)
                          );
                    }


                    //top left 
                    meshData.RenderVertex(
                        GetIndex(tX - iHalfNodeLength, tZ + iHalfNodeLength),
                        GetScaleVector3(x - fHalfNodeLength, z + fHalfNodeLength, vectorScale),
                        new Vector2(fTexLeft, fTexTop)
                        );


                    //1、是否到了上部的边界  
                    //2、或者说上边的邻结点的拆分了
                    //满足以上两个条件，均需要画出中间的点
                    neighborNode = GetNode(tX - tAdjOffset, tZ);
                    if ((tX - tAdjOffset) < 0 || (neighborNode != null && neighborNode.mbSubdivide))
                    {
                        //left mid
                        meshData.RenderVertex(
                          GetIndex(tX - iHalfNodeLength, tZ ),
                          GetScaleVector3(x - fHalfNodeLength, z , vectorScale),
                          new Vector2(fTexLeft, fMidZ)
                          );
                    }

                    //bottom left again
                    meshData.RenderVertex(
                        GetIndex(tX - iHalfNodeLength, tZ - iHalfNodeLength),
                        GetScaleVector3(x - fHalfNodeLength, z - fHalfNodeLength, vectorScale),
                        new Vector2(fTexLeft, fTexBottom)
                        );

                }
            }  // <= 3 

            #endregion

            #region 还可以继续划分

            else
            {
                int tChildHalfLength = (curNodeLength - 1) >> 2;
                float fChildHalfLength = (float)tChildHalfLength;

                int tChildNodeLength = (curNodeLength + 1) >> 1;

                int tFanCode = 0 ;
                //top right sud divide
                CQuadTreeNode childNode = GetNode(tX + tChildHalfLength, tZ + tChildHalfLength);
                if ( childNode != null && childNode.mbSubdivide )
                {
                    tFanCode |= 8; 
                }

                //top left
                childNode = GetNode(tX - tChildHalfLength, tZ + tChildHalfLength); 
                if( childNode != null && childNode.mbSubdivide )
                {
                    tFanCode |= 4; 
                }

                //bottom left 
                childNode = GetNode(tX - tChildHalfLength, tZ - tChildHalfLength);
                if( childNode != null && childNode.mbSubdivide )
                {
                    tFanCode |= 2;
                }

                //bottom right 
                childNode = GetNode(tX + tChildHalfLength, tZ - tChildHalfLength); 
                if( childNode != null && childNode.mbSubdivide )
                {
                    tFanCode |= 1; 
                }

                #region 各种情况的组合 

                enNodeTriFanType fanType = (enNodeTriFanType)tFanCode;
                switch ( fanType )
                {

                    #region 四个子结点都不分割
                    //子结点一个都不分割
                    case enNodeTriFanType.No_Fan:
                        {
                            //bottom left 
                            RenderNode(x - fChildHalfLength, z - fChildHalfLength, tChildNodeLength, ref meshData, vectorScale);

                            //bottom right 
                            RenderNode(x + fChildHalfLength, z - fChildHalfLength, tChildNodeLength, ref meshData, vectorScale);

                            //top left 
                            RenderNode(x - fChildHalfLength, z + fChildHalfLength, tChildNodeLength, ref meshData, vectorScale);

                            //top right 
                            RenderNode(x + fChildHalfLength, z + fChildHalfLength, tChildNodeLength, ref meshData, vectorScale);
                            break; 
                        }
                    #endregion


                    #region 左上右下分割，左下右下画三角形
                    //左上右下分割，左下右上画三角形
                    case enNodeTriFanType.BottomLeft_TopRight:
                        {
                            
                            //center 
                            meshData.RenderVertex(
                                GetIndex(tX , tZ),
                                GetScaleVector3(x, z, vectorScale),
                                new Vector2(fMidX, fMidZ)
                                );

                            //right mid 
                            meshData.RenderVertex(
                              GetIndex(tX + iHalfNodeLength, tZ),
                              GetScaleVector3(x + fHalfNodeLength, z, vectorScale),
                              new Vector2(fTexRight, fMidZ)
                              );

                            //top right 
                            meshData.RenderVertex(
                                GetIndex(tX + iHalfNodeLength, tZ + iHalfNodeLength),
                                GetScaleVector3(x + fHalfNodeLength, z + fHalfNodeLength, vectorScale),
                                new Vector2(fTexRight, fTexTop)
                                );

                            //top mid

                            //

                            break; 
                        }
                        #endregion

                }

                #endregion



            }

            #endregion
        }



        public void Render( ref stTerrainMeshData meshData ,Vector3 vertexScale )
        {
            Mesh mesh = meshData.mMesh; 
            if( null == mesh  )
            {
                Debug.LogError("Terrain without Mesh");
                return;
            }


            Profiler.BeginSample("Rebuild Vertices & UVs"); 
            Vector2[] uv = meshData.mUV;
            Vector3[] normals = meshData.mNormals; 
            Vector3[] vertices = meshData.mVertices ;
            for( int z = 0; z < mHeightData.mSize ; ++z)
            {
                for(int x = 0; x < mHeightData.mSize ; ++x)
                {
                    float y = mHeightData.GetRawHeightValue(x, z);
                    int vertexIdx = z * mHeightData.mSize + x; 
                    vertices[vertexIdx] = new Vector3(x*vertexScale.x, y * vertexScale.y, z * vertexScale.z);
                    uv[vertexIdx] = new Vector2((float)x / (float)mHeightData.mSize, (float)z / (float)mHeightData.mSize);
                }
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.normals = normals;
            Profiler.EndSample();


            Profiler.BeginSample("Rebuild Triangles");
            int nIdx = 0;
            int[] triangles = meshData.mTriangles; //一个正方形对应两个三角形，6个顶点
            for (int z = 0; z < mHeightData.mSize - 1; ++z)
            {
                for (int x = 0; x < mHeightData.mSize - 1; ++x)
                {
                    int bottomLeftIdx = z * mHeightData.mSize + x;
                    int topLeftIdx = (z + 1) * mHeightData.mSize + x;
                    int topRightIdx = topLeftIdx + 1;
                    int bottomRightIdx = bottomLeftIdx + 1;

                    triangles[nIdx++] = bottomLeftIdx;
                    triangles[nIdx++] = topLeftIdx;
                    triangles[nIdx++] = bottomRightIdx;
                    triangles[nIdx++] = topLeftIdx;
                    triangles[nIdx++] = topRightIdx;
                    triangles[nIdx++] = bottomRightIdx;
                }
            }

            mesh.triangles = triangles;
            Profiler.EndSample(); 
        }



        #endregion



        #region 地形纹理操作

        private List<CTerrainTile> mTerrainTiles = new List<CTerrainTile>();
        private Texture2D mTerrainTexture;
        public Texture2D TerrainTexture
        {
            get
            {
                return mTerrainTexture; 
            }

        }



        public void GenerateTextureMap( uint uiSize ,ushort maxHeight , ushort minHeight )
        {
            if( mTerrainTiles.Count <= 0 )
            {
                return; 
            }

            mTerrainTexture = null;
            int tHeightStride  = maxHeight / mTerrainTiles.Count ;

            float[] fBend = new float[mTerrainTiles.Count]; 

            //注意，这里的区域是互相重叠的
            int lastHeight = -1; 
            for(int i = 0; i < mTerrainTiles.Count; ++i)
            {
                CTerrainTile terrainTile = mTerrainTiles[i];
                //lastHeight += 1;
                terrainTile.lowHeight = lastHeight + 1 ;
                lastHeight += tHeightStride;

                terrainTile.optimalHeight = lastHeight;
                terrainTile.highHeight = (lastHeight - terrainTile.lowHeight) + lastHeight; 
            }

            for(int i = 0; i < mTerrainTiles.Count; ++i )
            {
                CTerrainTile terrainTile = mTerrainTiles[i];
                string log = string.Format("Tile Type:{0}|lowHeight:{1}|optimalHeight:{2}|highHeight:{3}",
                    terrainTile.TileType.ToString(),
                    terrainTile.lowHeight,
                    terrainTile.optimalHeight,
                    terrainTile.highHeight
                    ); 
                Debug.Log(log); 
            }



            mTerrainTexture = new Texture2D((int)uiSize,(int)uiSize, TextureFormat.RGBA32,false);

            CUtility.SetTextureReadble(mTerrainTexture, true);
          
            float fMapRatio = (float)mHeightData.mSize / uiSize;

            for (int z = 0; z < uiSize; ++z)
            {
                for (int x = 0; x < uiSize; ++x)
                {

                    Color totalColor = new Color();

                    for (int i = 0; i < mTerrainTiles.Count; ++i)
                    {
                        CTerrainTile tile = mTerrainTiles[i];
                        if (tile.mTileTexture == null)
                        {
                            continue;
                        }

                        int uiTexX = x;
                        int uiTexZ = z;

                        //CUtility.SetTextureReadble(tile.mTileTexture, true);

                        GetTexCoords(tile.mTileTexture, ref uiTexX, ref uiTexZ);

                       

                        Color color = tile.mTileTexture.GetPixel(uiTexX, uiTexZ);
                        fBend[i] = RegionPercent(tile.TileType, Limit(InterpolateHeight(x, z, fMapRatio),maxHeight,minHeight));

                        totalColor.r = Mathf.Min(color.r * fBend[i] + totalColor.r, 1.0f);
                        totalColor.g = Mathf.Min(color.g * fBend[i] + totalColor.g, 1.0f);
                        totalColor.b = Mathf.Min(color.b * fBend[i] + totalColor.b, 1.0f);
                        totalColor.a = 1.0f;

                        //CUtility.SetTextureReadble(tile.mTileTexture, false);
                    }// 

                    //输出到纹理上
                    if (totalColor.r == 0.0f 
                        && totalColor.g == 0.0f 
                        && totalColor.b == 0.0f)
                    {
                        ushort xHeight = (ushort)(x * fMapRatio);
                        ushort zHeight = (ushort)(z * fMapRatio); 
                        Debug.Log(string.Format("Color is Black | uiX:{0}|uiZ:{1}|hX:{2}|hZ:{3}|h:{4}",x,z,xHeight,zHeight,GetTrueHeightAtPoint(xHeight,zHeight))); 
                    }

                    mTerrainTexture.SetPixel(x, z,totalColor); 
                }
            }

            //OpenGL纹理的操作
            mTerrainTexture.Apply(); 
            CUtility.SetTextureReadble(mTerrainTexture, false);

            //string filePath = string.Format("{0}/{1}", Application.dataPath, "Runtime_TerrainTexture.png"); 
            //File.WriteAllBytes(filePath,mTerrainTexture.EncodeToPNG());
            //AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            //AssetDatabase.SaveAssets();
        } // 


        public CTerrainTile GetTile( enTileTypes tileType )
        {
            return mTerrainTiles.Count > 0  ? mTerrainTiles.Find(curTile => curTile.TileType == tileType) : null; 
        }


        float RegionPercent( enTileTypes tileType , ushort usHeight  )
        {
            CTerrainTile tile = GetTile(tileType);
            if (tile == null)
            {
                Debug.LogError(string.Format("No tileType : Type:{0}|Height:{1}", tileType.ToString(), usHeight));
                return 0.0f ;
            }

            CTerrainTile lowestTile = GetTile(enTileTypes.lowest_tile);
            CTerrainTile lowTile = GetTile(enTileTypes.low_tile);
            CTerrainTile highTile = GetTile(enTileTypes.high_tile);
            CTerrainTile highestTile = GetTile(enTileTypes.highest_tile);

            //如果最低的块已经加载了，且落在它的low Height的块里面
            if (lowestTile != null  )
            {
                if( tileType == enTileTypes.lowest_tile 
                    && IsHeightAllLocateInTile(tileType,usHeight)) 
                {
                    return 1.0f; 
                }
            }

            else if (lowTile != null)
            {
                if (tileType == enTileTypes.low_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }
            else if ( highTile!= null)
            {
                if (tileType == enTileTypes.high_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }
            else if (highestTile != null)
            {
                if (tileType == enTileTypes.highest_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }

            //以[,)左闭右开吧
            if (usHeight < tile.lowHeight || usHeight > tile.highHeight)
            {
                return 0.0f;
            }


            if ( usHeight < tile.optimalHeight )
            {
                float fTemp1 = usHeight - tile.lowHeight;
                float fTemp2 = tile.optimalHeight - tile.lowHeight;

                //这段会产生小斑点，因为有些值可能会比较特殊
                //if (fTemp1 == 0.0f)
                //{
                //    Debug.LogError(string.Format("Lower than Optimal Height: Type:{0}|Height:{1}|fTemp1:{2}|lowHeight:{3}|optimalHeight:{4}", tileType.ToString(), usHeight, fTemp1, tile.lowHeight, tile.optimalHeight));
                //    return 1.0f;
                //}
                return fTemp1 / fTemp2; 
            }
            else if( usHeight == tile.optimalHeight )
            {
                return 1.0f; 
            }
            else if( usHeight > tile.optimalHeight  )
            {
                float fTemp1 = tile.highHeight - tile.optimalHeight;

                //这段会产生小斑点，因为有些值可能会比较特殊
                //if (((fTemp1 - (usHeight - tile.optimalHeight)) / fTemp1) == 0.0f)
                //{
                //    Debug.LogError(string.Format("Higher than Optimal Height: Type:{0}|Height:{1}|fTemp1:{2}|optimalHeight:{3}", tileType.ToString(), usHeight, fTemp1, tile.optimalHeight));
                //    return 1.0f;
                //}
                return ((fTemp1 - (usHeight - tile.optimalHeight)) / fTemp1);
            }

            Debug.LogError(string.Format("Unknow: Type:{0}|Height:{1}", tileType.ToString(), usHeight));
            return 0.0f; 
        }


        private ushort GetTrueHeightAtPoint( int x ,int z )
        {
            return mHeightData.GetRawHeightValue(x, z);
        }

        //两个高度点之间的插值，这里的很有意思的
        private ushort InterpolateHeight( int x,int z , float fHeight2TexRatio )
        {
            float fScaledX = x * fHeight2TexRatio;
            float fScaledZ = z * fHeight2TexRatio;

            ushort usHighX = 0;
            ushort usHighZ = 0; 

            //X的A点
            ushort usLow = GetTrueHeightAtPoint((int)fScaledX, (int)fScaledZ);

            if( ( fScaledX + 1 ) > mHeightData.mSize )
            {
                return usLow; 
            }
            else
            {
                //X的B点
                usHighX = GetTrueHeightAtPoint((int)fScaledX + 1, (int)fScaledZ);    
            }

            //X的A、B两点之间插值
            float fInterpolation = (fScaledX - (int)fScaledX);
            float usX = (usHighX - usLow) * fInterpolation + usLow;   //插值出真正的高度值 


            //Z轴同理
            if ((fScaledZ + 1) > mHeightData.mSize)
            {
                return usLow;
            }
            else
            {
                //X的B点
                usHighZ = GetTrueHeightAtPoint((int)fScaledX, (int)fScaledZ + 1);
            }

            fInterpolation = (fScaledZ - (int)fScaledZ);
            float usZ = (usHighZ - usLow) * fInterpolation + usLow;   //插值出真正的高度值

            return ((ushort)((usX + usZ) / 2));
        }

        private ushort Limit( ushort usValue , ushort maxHeight , ushort minHeight )
        {
            if( usValue > maxHeight )
            {
                return maxHeight ; 
            }
            else if( usValue < minHeight )
            {
                return minHeight; 
            }
            return usValue; 
        }


        private bool IsHeightAllLocateInTile( enTileTypes tileType , ushort usHeight )
        {
            bool bRet = false;
            CTerrainTile tile = GetTile(tileType);
            if (tile != null
                && usHeight <= tile.optimalHeight)
            {
                bRet = true; 
            }
            return bRet; 
        }


        //因为要渲染出来的一张地形纹理，可能会比tile的宽高都要大，所以要tile其实是平铺布满地形纹理的
        public void GetTexCoords( Texture2D texture , ref int x , ref int y)
        {
            int uiWidth = texture.width;
            int uiHeight = texture.height;

            int tRepeatX = -1;
            int tRepeatY = -1;
            int i = 0; 

            while( tRepeatX == -1 )
            {
                i++; 
                if( x < (uiWidth * i))
                {
                    tRepeatX = i - 1; 
                }
            }


            i = 0; 
            while( tRepeatY == -1 )
            {
                ++i; 
                if( y < ( uiHeight * i) )
                {
                    tRepeatY = i - 1; 
                }
            }


            x = x - (uiWidth * tRepeatX);
            y = y - (uiHeight * tRepeatY); 
        }




        public void AddTile( enTileTypes tileType , Texture2D tileTexture ) 
        {
            if( tileTexture != null  )
            {
                if( mTerrainTiles.Exists( curTile => curTile.TileType == tileType )) 
                {
                    CTerrainTile oldTile = mTerrainTiles.Find(curTile => curTile.TileType == tileType); 
                    if( oldTile != null )
                    {
                        oldTile = new CTerrainTile(tileType, tileTexture);  
                    }      
                }
                else
                {
                    mTerrainTiles.Add(new CTerrainTile(tileType, tileTexture)); 
                }
            }
        }


        #endregion

        #region 高度图数据操作

        private stHeightData mHeightData;

        public void UnloadHeightMap()
        {
            mHeightData.Release();

            Debug.Log("Height Map is Unload!"); 
        }


      



        /// <summary>
        /// This fuction came from book 《Focus On 3D Terrain Programming》 ,thanks Trent Polack a lot
        /// </summary>
        /// <param name="size"></param>
        /// <param name="iter"></param>
        /// <param name="minHeightValue"></param>
        /// <param name="maxHeightValue"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public bool MakeTerrainFault( int size , int iter , ushort minHeightValue , ushort maxHeightValue , float fFilter )
        {
            if( mHeightData.IsValid() )
            {
                UnloadHeightMap();      
            }

            mHeightData.Allocate(size);

            float[,] fTempHeightData = new float[size, size]; 

            for( int iCurIter = 0; iCurIter < iter; ++iCurIter )
            {
                //高度递减
                int tHeight = maxHeightValue - ((maxHeightValue - minHeightValue ) * iCurIter) / iter;
                //int tHeight = Random.Range(minHeightValue , maxHeightValue);  //temp 

                int tRandomX1 = Random.Range(0, size);
                int tRandomZ1 = Random.Range(0, size);

                int tRandomX2 = 0;
                int tRandomZ2 = 0;
                do
                {
                    tRandomX2 = Random.Range(0, size);
                    tRandomZ2 = Random.Range(0, size);        
                } while ( tRandomX2 == tRandomX1 && tRandomZ2 == tRandomZ1 );


                //两个方向的矢量
                int tDirX1 = tRandomX2 - tRandomX1;
                int tDirZ1 = tRandomZ2 - tRandomZ1; 

                //遍历每个顶点，看看分布在分割线的哪一边
                for( int z = 0; z < size; ++z)
                {
                    for(int x = 0; x < size; ++x )
                    {
                        int tDirX2 = x - tRandomX1;
                        int tDirZ2 = z - tRandomZ1; 
                        
                        if( (tDirX2 * tDirZ1 - tDirX1 * tDirZ2) > 0  )
                        {
                            fTempHeightData[x, z] += tHeight; //!!!!!自加符号有问题！！！！
                        }       
                    }
                }

                FilterHeightField(ref fTempHeightData,size,fFilter);

            }

            NormalizeTerrain(ref fTempHeightData, size , maxHeightValue ); 

            for(int z = 0; z < size; ++z)
            {
                for(int x = 0; x < size; ++x)
                {
                    SetHeightAtPoint((ushort)fTempHeightData[x, z], x, z);
                }
            }

            return true  ; 
        }


        void SetHeightAtPoint( ushort usHeight , int x, int z)
        {
            mHeightData.SetHeightValue(usHeight, x, z);
        }


        void NormalizeTerrain(ref float[,] fHeightData, int size , ushort maxHeight )
        {
            float fMin = fHeightData[0, 0];
            float fMax = fHeightData[0, 0];

            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x )
                {
                    if( fHeightData[x,z] > fMax)
                    {
                        fMax = fHeightData[x, z]; 
                    }

                    if(fHeightData[x,z] < fMin)
                    {
                        fMin = fHeightData[x, z]; 
                    }

                }   
            }

            Debug.Log(string.Format("Before Normailzed MaxHeight:{0}|MinHeight:{1}",fMax,fMin)); 

            if(fMax <= fMin)
            {
                return; 
            }

            float fHeight = fMax - fMin;
            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    fHeightData[x, z] = ((fHeightData[x, z] - fMin) / fHeight) * maxHeight  ;
                }
            }


            ///////////////打LOG用
            fMax = fHeightData[0, 0];
            fMin = fHeightData[0, 0];
            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    if (fHeightData[x, z] > fMax)
                    {
                        fMax = fHeightData[x, z];
                    }

                    if (fHeightData[x, z] < fMin)
                    {
                        fMin = fHeightData[x, z];
                    }
                }
            }

            Debug.Log(string.Format("After Normailzed MaxHeight:{0}|MinHeight:{1}", fMax, fMin));

            ///////////////////////////
        }

        void FilterHeightField( ref float[,] fHeightData ,int size , float fFilter )
        {
            //四向模糊

            //从左往右的模糊
            for ( int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    i,0,  //初始的x,y
                    0,1,       //数组步进值
                    size,      //数组个数
                    fFilter);
            }

            //从右往左的模糊
            for( int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    i, size -1,
                    0,-1,
                    size,
                    fFilter); 
            }


            //从上到下的模糊
            for (int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    0,i,
                    1,0,
                    size,
                    fFilter);
            }


            //从下到上的模糊
            for (int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    size - 1,i ,
                    -1,0,
                    size,
                    fFilter);
            }
        }



        void FilterHeightBand(
            ref float[,] fBandData ,
            int beginX,
            int beginY,
            int strideX,
            int strideY,
            int count , 
            float fFilter )
        {
            //Debug.Log(string.Format("BeginX:{0} | BeginY:{1} | StrideX:{2} | StrideY:{3}",beginX,beginY,strideX,strideY)); 
       
            //float beginValue = fBandData[beginX, beginY];
            float curValue = fBandData[beginX,beginY];
            int jx = strideX;
            int jy = strideY;

            //float delta = fFilter / (count - 1);
            for( int i = 0; i < count - 1; ++i)
            {
                int nextX = beginX + jx;
                int nextY = beginY + jy;

                fBandData[nextX, nextY] = fFilter * curValue + (1 - fFilter) * fBandData[nextX, nextY];
                curValue = fBandData[nextX, nextY];

                //float tFilter = fFilter - delta * ((jx - beginX + jy - beginY) * 0.5f);
                //fBandData[nextX, nextY] = tFilter * beginValue;

                jx += strideX;
                jy += strideY; 
            }
        }


        #endregion



    }
}

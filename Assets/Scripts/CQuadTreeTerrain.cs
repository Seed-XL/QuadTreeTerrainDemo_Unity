using UnityEngine;
using System.Collections.Generic;


namespace Assets.Scripts.QuadTree
{


    #region 纹理数据 

    public enum enTileTypes
    {
        lowest_tile = 0 , 
        low_tile = 1 , 
        hight_tile = 2 , 
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

        private bool InRange( int x ,int y )
        {
            return x >= 0 && x < mSize && y >= 0 && y < mSize; 
        }
    }

    #endregion

    class CQuadTreeTerrain
    {



        #region 地形纹理操作

        private List<CTerrainTile> mTerrainTiles = new List<CTerrainTile>();
        private Texture2D mTerrainTexture;


        public void GenerateTextureMap( uint uiSize )
        {
            if( mTerrainTiles.Count <= 0 )
            {
                return; 
            }

            mTerrainTexture = null;
            int tHeightStride = 255 / mTerrainTiles.Count; 

            //注意，这里的区域是互相重叠的
            int lastHeight = -1; 
            for(int i = 0; i < mTerrainTiles.Count; ++i)
            {
                CTerrainTile terrainTile = mTerrainTiles[i];
                terrainTile.lowHeight = lastHeight + 1;
                lastHeight += tHeightStride;

                terrainTile.optimalHeight = lastHeight;
                terrainTile.highHeight = (lastHeight - terrainTile.lowHeight) + lastHeight; 
            }

            mTerrainTexture = new Texture2D((int)uiSize,(int)uiSize, TextureFormat.RGB24,false);


            float fMapRatio = (float)mHeightData.mSize / uiSize; 

            for(int z = 0; z < uiSize; ++z )
            {
                for(int x = 0; x < uiSize; ++x)
                {
                    float fTotalRed = 0.0f;
                    float fTotalGreen = 0.0f;
                    float fTotalBlue = 0.0f; 

                    for( int i = 0; i < mTerrainTiles.Count; ++i)
                    {
                        int uiTexX = x;
                        int uiTexZ = z; 

                        //to do

                    }
                }
            }


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
        public bool MakeTerrainFault( int size , int iter , int minHeightValue , int maxHeightValue , float fFilter )
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
                int tHeight = maxHeightValue - ((maxHeightValue - minHeightValue) * iCurIter) / iter;

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
                            fTempHeightData[x, z] = tHeight; 
                        }       
                    }
                }

                FilterHeightField(ref fTempHeightData,size,fFilter);
            }

            NormalizeTerrain(ref fTempHeightData, size); 

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


        void NormalizeTerrain(ref float[,] fHeightData, int size)
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

            if(fMax <= fMin)
            {
                return; 
            }

            float fHeight = fMax - fMin;


            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    fHeightData[x, z] = ((fHeightData[x, z] - fMin) / fHeight) * 255.0f; 
                }
            }
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
            Debug.Log(string.Format("BeginX:{0} | BeginY:{1} | StrideX:{2} | StrideY:{2}",beginX,beginY,strideX,strideY)); 

            float curValue = fBandData[beginX,beginY];
            int jx = strideX;
            int jy = strideY; 
            

            for( int i = 0; i < count - 1; ++i)
            {
                int nextX = beginX + jx;
                int nextY = beginY + jy; 

                fBandData[nextX,nextY] = fFilter * curValue + (1 - fFilter) * fBandData[nextX,nextY];
                curValue = fBandData[nextX, nextY];

                jx += strideX;
                jy += strideY; 
            }
        }


        #endregion



    }
}

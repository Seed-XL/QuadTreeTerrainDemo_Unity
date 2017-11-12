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

        public ushort GetHeightValue( int x, int y )
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

    class CQuadTreeTerrain
    {



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



        public void GenerateTextureMap( uint uiSize )
        {
            if( mTerrainTiles.Count <= 0 )
            {
                return; 
            }

            mTerrainTexture = null;
            int tHeightStride = 255 / ( mTerrainTiles.Count + 1 ) ;

            float[] fBend = new float[mTerrainTiles.Count]; 

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
                        fBend[i] = RegionPercent(tile.TileType, Limit(InterpolateHeight(x, z, fMapRatio)));

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
                        Debug.Log(string.Format("x:{0}|y:{1}|h:{2}",x,z,GetTrueHeightAtPoint(x,z))); 
                    }
                    mTerrainTexture.SetPixel(x, z,totalColor); 
                }
            }

            //OpenGL纹理的操作

            CUtility.SetTextureReadble(mTerrainTexture, false);

            string filePath = string.Format("{0}/{1}", Application.dataPath, "Runtime_TerrainTexture.png"); 
            File.WriteAllBytes(filePath,mTerrainTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
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

            //TO DO 这里的逻辑有边界问题
            //if ( usHeight < tile.lowHeight || usHeight > tile.highHeight)
            //{
            //    return 0.0f; 
            //}

            ////找出最低边界保护
            //if( usHeight < tile.optimalHeight )
            //{

            //}
          
            if( usHeight < tile.optimalHeight )
            {
                float fTemp1 = usHeight - tile.lowHeight;
                float fTemp2 = tile.optimalHeight - tile.lowHeight;

                if( fTemp1/fTemp2 == 0.0f )
                {
                    Debug.LogError(string.Format("Lower than Optimal Height: Type:{0}|Height:{1}|fTemp1:{2}|lowHeight:{3}|optimalHeight:{4}", tileType.ToString(), usHeight,fTemp1,tile.lowHeight,tile.optimalHeight));
                    if( tileType == enTileTypes.lowest_tile )
                    {
                        return Random.Range(0, 0.5f);
                    }
                }
                return Mathf.Max(fTemp1 / fTemp2,0.1f); 
            }
            else if( usHeight == tile.optimalHeight )
            {
                return 1.0f; 
            }
            else if( usHeight > tile.optimalHeight  )
            {
                float fTemp1 = tile.highHeight - tile.optimalHeight;

                if (((fTemp1 - (usHeight - tile.optimalHeight)) / fTemp1) == 0.0f)
                {
                    Debug.LogError(string.Format("Higher than Optimal Height: Type:{0}|Height:{1}|fTemp1:{2}|optimalHeight:{3}", tileType.ToString(), usHeight,fTemp1,tile.optimalHeight));
                    if( tileType == enTileTypes.highest_tile )
                    {
                        //return Random.Range(0, 0.5f); 
                        return 1.0f;
                    }
                }
                return Mathf.Max(((fTemp1 - (usHeight - tile.optimalHeight)) / fTemp1 ),0.1f); 
            }

            Debug.LogError(string.Format("Unknow: Type:{0}|Height:{1}", tileType.ToString(), usHeight));
            return 0.0f; 
        }


        private ushort GetTrueHeightAtPoint( int x ,int z )
        {
            return mHeightData.GetHeightValue(x, z);
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


        private ushort Limit( ushort usValue  )
        {
            if( usValue > 255 )
            {
                return 255; 
            }
            else if( usValue < 0 )
            {
                return 0; 
            }
            return usValue; 
        }


        private bool IsHeightAllLocateInTile( enTileTypes tileType , ushort usHeight )
        {
            bool bRet = false;
            CTerrainTile tile = GetTile(tileType);
            if (tile != null
                && usHeight < tile.optimalHeight)
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
            //Debug.Log(string.Format("BeginX:{0} | BeginY:{1} | StrideX:{2} | StrideY:{3}",beginX,beginY,strideX,strideY)); 

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

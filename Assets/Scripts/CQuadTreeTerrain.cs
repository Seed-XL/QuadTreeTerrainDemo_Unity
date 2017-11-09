using UnityEngine; 


namespace Assets.Scripts.QuadTree
{
    struct stHeightData
    {
        private ushort[,] heightData;
        public int size;    
        
        public bool IsValid()
        {
            return heightData != null; 
        }    
        
        public void Release()
        {
            heightData = null;
            size = 0; 
        }   


        public void Allocate( int mapSize )
        {
            if( mapSize > 0 )
            {
                heightData = new ushort[mapSize,mapSize];
                size = mapSize; 
            }
        }
    }


    class CQuadTreeTerrain
    {
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
        public bool MakeTerrainFault( int size , int iter , int minHeightValue , int maxHeightValue , float filter )
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


            }

            return false ; 
        }



        void FilterHeightField( float[,] fHeightData ,int size , float fFilter )
        {
            //四向模糊

            //从左往右的模糊
            for ( int i = 0; i < size; ++i)
            {
                //FilterHeightBand( fHeightData)    
            }
        }



        void FilterHeightBand( ref float[] fBandData , int stride , int count , float fFilter )
        {
            float v = fBandData[0];
            int j = stride; 
            

            for( int i = 0; i < count - 1; ++i)
            {
                fBandData[j] = fFilter * v + (1 - fFilter) * fBandData[j];
                v = fBandData[j];
                j += stride; 
            }
        }

    }
}

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


        public bool MakeTerrainFault( int size , int iter , int minHeightValue , int maxHeightValue , float filter )
        {
            if( mHeightData.IsValid() )
            {
                UnloadHeightMap();      
            }

            mHeightData.Allocate(size); 

            return false ; 
        }
    }
}

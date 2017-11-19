# QuadTreeTerrainDemo_Unity

### 目的：尝试实现一个地形LOD算法中的QuadTree
 
目前借鉴《Focus On 3D Terrain Programming》书上的代码实现了随机生成高度图和混合纹理的功能,动态调整LOD的代码还有些Bug
 
#### 来张平面的Gif
![平面Gif](https://wx1.sinaimg.cn/mw690/6b98bc8agy1flnw0j327yg20fk09qb29.gif)
 
#### 按所有高度顶点画
![普通模式Texture](http://wx1.sinaimg.cn/mw690/6b98bc8agy1flnqn628iyj20lw0b2agk.jpg)

![普通模式Wireframe](http://wx1.sinaimg.cn/mw690/6b98bc8agy1flnqn81rmlj20m40ap41w.jpg)

 
#### 按QuadTree动态分割来画
![QuadTree Texture](http://wx4.sinaimg.cn/mw690/6b98bc8agy1flnqn2dbhwj20lr0b3gs0.jpg)

![普通模式Wireframe](http://wx3.sinaimg.cn/mw690/6b98bc8agy1flnqn3e967j20lw0aqgni.jpg)


#### 产生裂缝的地方
![Bug](http://wx2.sinaimg.cn/mw690/6b98bc8agy1flnqn036k1j20e809oglw.jpg)


using UnityEngine;


public class TerrainManager : MonoBehaviour {

    static int TERRAIN_SIZE = (int)Mathf.Pow(2f, 12f);

    public static readonly Vector3 TERRAIN_ORIGIN   = new Vector3(0, 0, 0);
    public static readonly Vector3 TERRAIN_END      = new Vector3(TERRAIN_SIZE, 0, TERRAIN_SIZE);

    public static int lowerestBlockSizeOnQuadTree = 32;

    public static double PIXEL_HEIGHT;
    public static double PIXEL_WIDTH;
    public static float TERRAIN_HEIGHT_MULTIPLIER = 100;
    public static float TERRAIN_HEIGHT_LAKE = -1;
    
    public Texture2D heightMap;
    public Texture2D waterMap;

    public static Texture2D m_heightMap;

    public static Plane[] frustumPlanes = new Plane[6];
    

    private void Awake()
    {
        //m_heightMap = heightMap;
        
        //PIXEL_WIDTH = TERRAIN_END.z / m_heightMap.width;
        //PIXEL_HEIGHT = TERRAIN_END.z / m_heightMap.height;

        //Shader.SetGlobalInt("_globalTerrainSize", TERRAIN_SIZE);

        //Shader.SetGlobalFloat("_globalPixelSize", (float)PIXEL_WIDTH);

        //Shader.SetGlobalFloat("TERRAIN_HEIGHT_MULTIPLIER", TERRAIN_HEIGHT_MULTIPLIER);

        //terrain.SetTexture("_waterMaptmp", waterMap); 

        //Shader.SetGlobalTexture("_heightMapAux", m_heightMap);

        //moisture = GameObject.Find("Calculator").GetComponent<MoistureDistribuition>();
        //tex = new Texture2D(1024, 1024, TextureFormat.RFloat, false);

       // quadtree = new QuadTreeNode();

    }
    

    public void Update()
    {
        GeometryUtility.CalculateFrustumPlanes(Camera.main, frustumPlanes);
    }
}


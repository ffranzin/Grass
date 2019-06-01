using System.Collections.Generic;
using UnityEngine;

public class TerrainColliderInteractionShape
{
    /*
    This class is simple manager of possible shapes that interact with the 'CollisionMap'.
    Each shape has a group of the parameters that be sent to the GPU, which defines how the collision will be treated.
    */

    /// maintain this values below matching with the defines in 'GrassCollidionDetect.compute'.
    public enum ShapeType { Plane, Sphere };

    public ShapeType type;

    private Vector4[] originalVertices = new Vector4[4];
    private Vector4[] TRSVertices = new Vector4[4];

    private Vector4 normal;

    public Vector3 meshRadius;
    public static float meshRadiusOffset = 2;

    public Transform transform;
    

    public TerrainColliderInteractionShape(Transform transform, ShapeType type)
    {
        this.type = type;
        this.transform = transform;
    }

    /// <summary>
    /// Apply translation, rotation, scale and remove the camera's offset in vertices of the plane.
    /// </summary>
    private void ApplyTRSOnVertices()
    {
        for (int i = 0; i < 4; i++)
            TRSVertices[i] = transform.TransformPoint(originalVertices[i]);
    }


    /// <summary>
    /// Find the vertices of the plane for an arbitrary mesh. The plane is generated based on the min-max of the bounds. 
    /// </summary>
    public void PlaneInitialize()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;

        Vector3 min = mesh.bounds.min;
        Vector3 max = mesh.bounds.max;

        min.y = max.y = 0;

        originalVertices[0] = min;
        originalVertices[1] = new Vector4(min.x, 0, max.z, 0);
        originalVertices[2] = max;
        originalVertices[3] = new Vector4(max.x, 0, min.z, 0);

        ApplyTRSOnVertices();

        FindShapeRadius(mesh);
    }


    /// <summary>
    /// Find and radius for the mesh. The radius is based on the mesh center and the farthest vertex from it.
    /// For the collision calculation it is used to reduce the amount of dispatch in GPU.
    /// </summary>
    private void FindShapeRadius(Mesh mesh)
    {
        mesh.RecalculateBounds();
        Vector3 meshcenter = transform.TransformPoint(mesh.bounds.center);

        float r = -1;

        foreach (Vector3 v in mesh.vertices)
            r = Mathf.Max(r, (transform.TransformPoint(v) - meshcenter).magnitude);

        meshRadius = new Vector3(r, r, r);
    }


    /// <summary>
    /// Initialize the sphere shape.
    /// </summary>
    public void SphereInitialize()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().mesh;

        FindShapeRadius(mesh);
    }


    /// <summary>
    /// Set the parameters of the plane on the GPU.
    /// </summary>
    private void SetComputeShaderParametersPlane(ComputeShader compute)
    {
        ApplyTRSOnVertices();

        compute.SetVectorArray("_Plane_Vertices", TRSVertices);
        compute.SetVector("_Plane_Normal", transform.up.normalized);

        compute.SetVector("_Plane_Position", transform.position);
    }


    /// <summary>
    /// Set the parameters of the sphere on the GPU.
    /// </summary>
    private void SetComputeShaderParametersSphere(ComputeShader compute)
    {
        compute.SetVector("_Sphere_Position", transform.position);
    }


    /// <summary>
    /// Call the method to pass the parameters of this shape on GPU. 
    /// </summary>
    public void SetShapeComputeShader(ComputeShader compute)
    {
        switch (type)
        {
            case ShapeType.Plane:
                SetComputeShaderParametersPlane(compute);
                break;
            case ShapeType.Sphere:
                SetComputeShaderParametersSphere(compute);
                break;
            default:
                break;
        }
        compute.SetInt("_colliderShape", (int)type);
        compute.SetFloat("_objectRadius", meshRadius.x);
    }
}



public class TerrainColliderInteraction : MonoBehaviour
{
    /*
    This class is a manager of all colliders that interact with the 'CollisionMap'. 
    */
    public static List<TerrainColliderInteraction> allColliders { get; } = new List<TerrainColliderInteraction>();


    public enum CollisionType { Permanent, Temporary, None};
    
    [HideInInspector] public TerrainColliderInteractionShape shape;

    public TerrainColliderInteractionShape.ShapeType type;
    public CollisionType generatedCollisionType;

    private Vector3 lastPos;

    private Vector3 _force, _forceX0Z;
    [HideInInspector] public Vector3 force { get { return _force; } }
    [HideInInspector] public Vector3 forceXZ { get { return _forceX0Z; } }

    public bool consideOnlyInsideCell;
    public bool activeCollision { get; private set; }


    public static void DeleteAllColliders()
    {
        for (int i = 0; i < allColliders.Count; i++)
        {
            GameObject.DestroyImmediate(allColliders[i].transform);
            allColliders[i] = null;
        }
        allColliders.Clear();
    }

    public void Start()
    {
        lastPos = transform.position;
        allColliders.Add(this);
        activeCollision = GrassHashManager.Instance.IsPositionInsideHashBounds(transform.position);

        _force = Vector3.zero;
        _forceX0Z = Vector3.zero;

        shape = new TerrainColliderInteractionShape(transform, type);
       
        switch (type)
        {
            case TerrainColliderInteractionShape.ShapeType.Plane:
                shape.PlaneInitialize();
                break;
            case TerrainColliderInteractionShape.ShapeType.Sphere:
                shape.SphereInitialize();
                break;
        }
        
        try
        {
            //Destroy(GetComponent<MeshRenderer>());
        }
        catch
        {

        }
        _cell = GrassHashManager.Instance.GetHashCell(transform.position);
    }
    

    private List<GrassHashCell> _cells = new List<GrassHashCell>(); 
    public List<GrassHashCell> cells
    {
        get
        {
            _cells.Clear();
            GrassHashManager.Instance.GetCellsAroundPos(transform.position, shape.meshRadius.x + TerrainColliderInteractionShape.meshRadiusOffset, ref _cells);

            for (int i = 0; i < _cells.Count; i++)
            {
                GrassHashCell c = _cells[i];
                c.hadCollisionInsideCell = true;
                c.hadPermanentCollisionInsideCell = generatedCollisionType == CollisionType.Permanent;
            }

            return _cells;
        }
    }

    public GrassHashCell _cell;
    public GrassHashCell cell
    {
        get
        {
            if(_cell == null || !_cell.boundsWorld.Contains(transform.position))
            {
                _cell = GrassHashManager.Instance.GetHashCell(transform.position);
                
                if(_cell != null)
                {
                    _cell.hadCollisionInsideCell = true;
                    _cell.hadPermanentCollisionInsideCell = generatedCollisionType == CollisionType.Permanent;
                }
            }
            
            return _cell;
        }
    }

    

    public void FixedUpdate()
    {
        if(!GrassHashManager.Instance.IsPositionInsideHashBounds(transform.position))
        {
            activeCollision = false;
            return;
        }

        activeCollision = true;

        _force = _forceX0Z = transform.position - lastPos;
        _forceX0Z.y = 0;

        lastPos = transform.position;
    }



#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (shape != null && type == TerrainColliderInteractionShape.ShapeType.Plane)
        {
            Gizmos.DrawWireSphere(transform.position, shape.meshRadius.x);
        }
    }
#endif
}

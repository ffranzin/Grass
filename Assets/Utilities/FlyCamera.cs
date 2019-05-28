using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float minSpeed  = 0.5f;
    public float mainSpeed = 10f;        /// Regular speed
    public float camMouseSens    = 4f;   /// Camera sensitivity by mouse input
    public float camJoystickSens = 100f; /// Camera sensitivity by joystick input
    public float shiftMultiplier = 2f;   /// Multiplied by how long shift is held. Basically running
    public float accel = 2f;             /// Increases/decreases the speed increment

    public bool clickToMove       = true;
    public bool preventUpsideDown = true;

    Vector3 currentUp       = Vector3.up;
    Vector3 currentRotation = Vector3.zero;

    public static float velocity;

    private void Start()
    {
       
    }

    //void MatchSurfaceNormal(float maxDegreesDelta = 9999)
    //{
    //    //dvec3 planetCenter = SphericalTerrain.centerWithOffset;
    //    //currentUp = (transform.position.ToDvec3() - planetCenter).Normalized.ToVector3();
    //    //transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.forward, currentUp), maxDegreesDelta);
    //}

    private void Update()
    {
        Vector3 lastPosition = transform.position;

        //if (Input.GetKey(KeyCode.X))
        //    MatchSurfaceNormal(1f);

        //dvec3 planetCenter = SphericalTerrain.centerWithOffset;
        //double dist = dvec3.Distance(planetCenter, transform.position.ToDvec3()) - SphericalTerrain.Radius;

        // Transform the camera to match the planet's surface orientation
        //if (dist < heightToMatchSurfaceUp)
        //	MatchSurfaceNormal(2f);

        //ProcessJoystick();
        ProcessMouse();

        if (Input.GetKey(KeyCode.L))
            Level();

        //if (dynamicNearPlane)
        //{
        //	dvec3 camPosWithoutOffset = OriginOffsetManager.RemoveOffset(Camera.main.transform.position.ToDvec3());
        //	double d = dvec3.Distance(camPosWithoutOffset, SphericalTerrain.centerWithoutOffset);
        //	double near = d - SphericalTerrain.DefaultMaxRadius;
        //	near *= 0.5;

        //	if (near < 1)
        //		near = 1;
        //	if (near > 100000)
        //		near = 100000;

        //	Camera.main.nearClipPlane = (float)near;
        //	Camera.main.farClipPlane = (float)(d + SphericalTerrain.DefaultMaxRadius);
        //}

        float distance = (transform.position - lastPosition).magnitude;
        velocity = (distance / Time.deltaTime) * 3.6f;
    }

    private void ProcessJoystick()
    {
        transform.Rotate(currentUp, Input.GetAxis("RightAnalog_Horizontal") * Time.unscaledDeltaTime * camJoystickSens, Space.World);

        Vector3 surfaceRight = Vector3.Cross(currentUp, transform.forward);
        transform.Rotate(surfaceRight, -Input.GetAxis("RightAnalog_Vertical") * Time.unscaledDeltaTime * camJoystickSens, Space.World);

        mainSpeed += (Input.GetAxis("10th Joystick Axis") - Input.GetAxis("9th Joystick Axis")) * mainSpeed * 1.5f * Time.unscaledDeltaTime;
        if (mainSpeed < minSpeed)
            mainSpeed = minSpeed;

        float translateX =  Input.GetAxis("LeftAnalog_Horizontal") * mainSpeed * Time.unscaledDeltaTime;
        float translateZ = -Input.GetAxis("LeftAnalog_Vertical")   * mainSpeed * Time.unscaledDeltaTime;

        transform.Translate(new Vector3(translateX, 0, translateZ));
    }

    private void ProcessMouse()
    {
        if (clickToMove && !Input.GetMouseButton(0)) /// No click, no move
            return;

        mainSpeed +=  Input.GetAxis("Mouse ScrollWheel")    * mainSpeed * accel; /// speed decrease/increase
        mainSpeed += (Input.GetKey(KeyCode.R) ?  0.01f : 0) * mainSpeed * accel; /// speed increase
        mainSpeed += (Input.GetKey(KeyCode.F) ? -0.01f : 0) * mainSpeed * accel; /// speed decrease

        mainSpeed = Mathf.Max(minSpeed, mainSpeed); /// clamp minimun

        /// Rotation was modified. Now prevents the camera from being upside down.
        /// Mouse look
        Vector3 mouseAxis = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0f);
        Vector3 newRotation = currentRotation + mouseAxis * camMouseSens;
        if (preventUpsideDown)
            newRotation.x = Mathf.Clamp(newRotation.x, -90f, 90f);
        transform.rotation = Quaternion.Euler(newRotation);
        currentRotation = newRotation;

        /// Movement
        Vector3 p = GetDirection() * mainSpeed * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.LeftShift))
            p *= shiftMultiplier;
        transform.Translate(p);
    }

    private Vector3 GetDirection()
    {
        Vector3 p_Velocity = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            p_Velocity += new Vector3(0, 1, 0);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            p_Velocity += new Vector3(0, -1, 0);
        }
        return p_Velocity;
    }

    public void ResetRotation()
    {
        transform.rotation = Quaternion.identity;
        currentRotation = Vector3.zero;
    }

    public void Level()
    {
        currentRotation.x = 0f;
        transform.rotation = Quaternion.Euler(currentRotation);
    }
}
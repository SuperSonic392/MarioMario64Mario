using UnityEngine;

[ExecuteInEditMode]
public class billboardedSprite : MonoBehaviour
{
    
    public Camera cam;

    public bool doX, doY, doZ;
    // Update is called once per frame
    void Update()
    {
        Cam();
    }

    private void OnDrawGizmos()
    {
        Cam();
    }

    private void Cam()
    {
        cam = Camera.main;

        float x = 0;
        float y = 0;
        float z = 0;
        if (doX)
            x = cam.transform.rotation.eulerAngles.x;
        if (doY)
            y = cam.transform.rotation.eulerAngles.y;
        if (doZ)
            z = cam.transform.rotation.eulerAngles.z;

        transform.rotation = Quaternion.Euler(x, y, z);
    }
}

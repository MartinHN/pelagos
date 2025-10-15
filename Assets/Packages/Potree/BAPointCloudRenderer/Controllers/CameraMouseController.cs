using UnityEngine;

namespace BAPointCloudRenderer.Controllers
{
    /*
     * CameraMouseController for flying-controls
     */
    public class CameraMouseController : MonoBehaviour
    {

        //Current yaw
        private float yaw = 0.0f;
        //Current pitch
        private float pitch = 0.0f;

        public float normalSpeed = 100;

        private Camera linkedCam;
        private GameObject linkedAnchor;

        void Start()
        {
            //Hide the cursor
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
            linkedCam = GetComponent<Camera>();
            linkedAnchor = linkedCam.transform.parent.parent.parent.gameObject;
            pitch = transform.eulerAngles.x;
            yaw = transform.eulerAngles.y;
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void FixedUpdate()
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                return;
            }
            //React to controls. (WASD, EQ and Mouse)
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            float scaleV = .4f;
            float moveUp = Input.GetKey(KeyCode.E) ? scaleV : Input.GetKey(KeyCode.Q) ? -scaleV : 0;

            float speed = normalSpeed;
            // if (Input.GetKey(KeyCode.C))
            // {
            //     speed /= 10; ;
            // }
            // else if (Input.GetKey(KeyCode.LeftShift))
            // {
            //     speed *= 5;
            // }

            if (linkedCam.orthographic)
            {
                float scaleFactor = 1;
                linkedCam.orthographicSize -= (moveVertical * speed * Time.deltaTime) * scaleFactor;
                // moveVertical = 0;
            }
            linkedAnchor.transform.Translate(transform.localToWorldMatrix * (new Vector3(moveHorizontal * speed * Time.deltaTime, moveUp * speed * Time.deltaTime, moveVertical * speed * Time.deltaTime)));
            yaw = transform.eulerAngles.y;
            yaw += 2 * Input.GetAxis("Mouse X");
            pitch = transform.eulerAngles.x;
            pitch -= 2 * Input.GetAxis("Mouse Y");
            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }
    }

}

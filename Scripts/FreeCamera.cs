using UnityEngine;

namespace FCG
{

    public class FreeCamera : MonoBehaviour
    {
        public float speedNormal = 10.0f;
        public float speedFast = 50.0f;
        public float speedVertical = 10.0f;
        public float speedMultiplierMin = 0.2f;
        public float speedMultiplierMax = 4.0f;
        public float speedMultiplierStep = 0.15f;
        public bool lockCursorOnLook = false;

        public float mouseSensitivityX = 5.0f;
        public float mouseSensitivityY = 5.0f;

        float rotY = 0.0f;
        float speedMultiplier = 1f;

        void Start()
        {
            if (GetComponent<Rigidbody>())
                GetComponent<Rigidbody>().freezeRotation = true;
        }

        void Update()
        {
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            if (wheel != 0f)
                speedMultiplier = Mathf.Clamp(speedMultiplier + wheel * speedMultiplierStep * 10f, speedMultiplierMin, speedMultiplierMax);

            // rotation        
            if (Input.GetMouseButton(1))
            {
                if (lockCursorOnLook)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }

                float rotX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivityX;
                rotY += Input.GetAxis("Mouse Y") * mouseSensitivityY;
                rotY = Mathf.Clamp(rotY, -89.5f, 89.5f);
                transform.localEulerAngles = new Vector3(-rotY, rotX, 0.0f);
            }
            else if (lockCursorOnLook)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            float forward = Input.GetAxis("Vertical");
            float strafe = Input.GetAxis("Horizontal");
            float vertical = 0f;
            if (Input.GetKey(KeyCode.E)) vertical += 1f;
            if (Input.GetKey(KeyCode.Q)) vertical -= 1f;

            // move forwards/backwards
            if (forward != 0.0f)
            {
                float speed = (Input.GetKey(KeyCode.LeftShift) ? speedFast : speedNormal) * speedMultiplier;
                Vector3 trans = new Vector3(0.0f, 0.0f, forward * speed * Time.deltaTime);
                gameObject.transform.localPosition += gameObject.transform.localRotation * trans;
            }

            // strafe left/right
            if (strafe != 0.0f)
            {
                float speed = (Input.GetKey(KeyCode.LeftShift) ? speedFast : speedNormal) * speedMultiplier;
                Vector3 trans = new Vector3(strafe * speed * Time.deltaTime, 0.0f, 0.0f);
                gameObject.transform.localPosition += gameObject.transform.localRotation * trans;
            }

            if (vertical != 0.0f)
            {
                float speed = speedVertical * speedMultiplier;
                Vector3 trans = new Vector3(0.0f, vertical * speed * Time.deltaTime, 0.0f);
                gameObject.transform.localPosition += trans;
            }
        }


    }

}

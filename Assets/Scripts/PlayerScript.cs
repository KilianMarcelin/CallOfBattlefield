using Mirror;
using UnityEngine;

namespace QuickStart
{
    public class PlayerScript : NetworkBehaviour
    {
        public Rigidbody rb;
        
        public float jumpForce = 5f;
        
        public float health = 100f;

        public void Hit(float damage)
        {
            
        }
        
        public override void OnStartLocalPlayer()
        {
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 1.8f, 0);
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            if (!isLocalPlayer) { return; }

            float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * 4f;
            float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * 4f;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            transform.Rotate(0, moveX, 0);
            transform.Translate(moveX, 0, moveZ);
            
            // Rotate with clamping
            Camera.main.transform.Rotate(-mouseY, 0, 0);
            transform.Rotate(0, mouseX, 0);
            
            // Clamp camera rotation
            if (Input.GetButton("Jump"))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }
}
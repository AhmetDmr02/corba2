using UnityEngine;
using UnityEngine.InputSystem;

namespace Corba
{
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        Transform target;
        float zoom = 1f;
        float orbit;
        Vector3 shakeOffset;
        float shake;

        void Awake()
        {
            Instance = this;
        }

        public void Follow(Transform t) => target = t;

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var mouse = Mouse.current;
            if (mouse != null) zoom = Mathf.Clamp(zoom - mouse.scroll.ReadValue().y * 0.0008f, 0.6f, 1.6f);

            Vector3 desiredPos;
            Quaternion desiredRot;
            if (target != null)
            {
                Vector3 offset = new Vector3(0f, 17f, -12f) * zoom;
                Vector3 focus = target.position;
                focus.x = Mathf.Clamp(focus.x, -CorbaMap.HalfX + 13f, CorbaMap.HalfX - 13f);
                focus.z = Mathf.Clamp(focus.z, -CorbaMap.HalfZ + 4f, CorbaMap.HalfZ - 9f);
                desiredPos = focus + offset;
                desiredRot = Quaternion.LookRotation(focus + Vector3.up * 1f - desiredPos);
                transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-6f * dt));
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-8f * dt));
            }
            else
            {
                orbit += dt * 3f;
                var rot = Quaternion.Euler(0f, orbit, 0f);
                desiredPos = rot * new Vector3(0f, 52f, -58f);
                transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-2f * dt));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-desiredPos + Vector3.down * 4f), 1f - Mathf.Exp(-3f * dt));
            }

            shake = Mathf.MoveTowards(shake, 0f, dt * 2f);
            transform.position -= shakeOffset;
            shakeOffset = Random.insideUnitSphere * shake * 0.3f;
            transform.position += shakeOffset;
        }
    }
}

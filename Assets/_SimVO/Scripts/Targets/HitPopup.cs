using TMPro;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Floating "+10" label that drifts up, faces the player and fades out.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class HitPopup : MonoBehaviour
    {
        public float lifetime = 1.1f;
        public float riseSpeed = 0.35f;
        public Color goodColor = new Color(1f, 0.85f, 0.2f);
        public Color missColor = new Color(0.8f, 0.8f, 0.8f);

        TMP_Text label;
        float age;
        Color baseColor;

        void Awake() => label = GetComponent<TMP_Text>();

        public void Show(int points)
        {
            baseColor = points > 0 ? goodColor : missColor;
            label.text = points > 0 ? $"+{points}" : "0";
            label.color = baseColor;
            FaceCamera();
            transform.position += -transform.forward * 0.05f;
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            FaceCamera();
            var alpha = 1f - Mathf.Clamp01((age - lifetime * 0.5f) / (lifetime * 0.5f));
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            if (age >= lifetime) Destroy(gameObject);
        }

        void FaceCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var toCamera = transform.position - cam.transform.position;
            if (toCamera.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(toCamera);
        }
    }
}

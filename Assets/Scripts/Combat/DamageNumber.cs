using UnityEngine;

namespace DreamDungeon
{
    public sealed class DamageNumber : MonoBehaviour
    {
        private TextMesh textMesh;
        private float lifeTime = 0.85f;
        private float elapsed;
        private Vector3 velocity;
        private Color startColor;

        public static void Spawn(Vector3 position, string text, Color color, float size = 0.038f)
        {
            GameObject go = new GameObject("DamageNumber");
            go.transform.position = position + new Vector3(Random.Range(-0.08f, 0.08f), 0.22f, -0.5f);
            DamageNumber comp = go.AddComponent<DamageNumber>();
            comp.Init(text, color, size);
        }

        public void Init(string text, Color color, float size)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 32;
            textMesh.characterSize = size;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 25;
            }

            startColor = color;
            velocity = new Vector3(Random.Range(-0.15f, 0.15f), 1.15f, 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            transform.position += velocity * dt;
            velocity.y = Mathf.Max(0.2f, velocity.y - dt * 0.9f);

            if (textMesh != null)
            {
                float alpha = Mathf.Clamp01(1f - (elapsed / lifeTime));
                Color c = startColor;
                c.a = alpha;
                textMesh.color = c;
            }

            if (elapsed >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }
}

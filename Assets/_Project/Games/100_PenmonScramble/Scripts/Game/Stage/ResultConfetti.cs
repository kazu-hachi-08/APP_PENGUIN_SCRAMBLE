using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// クリアの紙ふぶき: 画面の下の左右の角から上へ打ち上げ、ひらひら回りながら落ちて消える。
    /// 紙は最初にまとめて作って使い回す（クリアのたびに作り直さない）。見た目だけなので乱数は UnityEngine.Random
    /// </summary>
    public class ResultConfetti : MonoBehaviour
    {
        private const int PieceCount = 90;
        private static readonly Vector2 PieceSizeMin = new Vector2(12f, 20f);
        private static readonly Vector2 PieceSizeMax = new Vector2(20f, 34f);
        // 打ち上げの速さ（画面の単位/秒）と、まん中の上へ向けた角度の幅（度。真上から内側へ）
        private const float SpeedMin = 1300f;
        private const float SpeedMax = 2100f;
        private const float AngleMin = 8f;
        private const float AngleMax = 38f;
        private const float Gravity = 1600f;
        // 空気でゆっくりになり、ひらひら落ちて見える
        private const float Drag = 1.6f;
        private const float FallSpeedMax = 260f;
        private const float SwayAmount = 90f;
        private const float SwayRate = 2.2f;
        private const float SpinMax = 540f;
        private const float LifeSeconds = 3.6f;
        private const float FadeSeconds = 0.6f;

        [SerializeField] private Color[] _colors =
        {
            new Color(1f, 0.82f, 0.25f),
            new Color(0.35f, 0.75f, 1f),
            new Color(1f, 0.45f, 0.55f),
            new Color(0.45f, 0.9f, 0.5f),
            new Color(1f, 1f, 1f),
        };

        private Piece[] _pieces;
        private RectTransform _area;

        private struct Piece
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Spin;
            public float Angle;
            public float Phase;
            public float Age;
        }

        public void Burst()
        {
            Build();
            var half = _area.rect.size * 0.5f;
            for (var i = 0; i < _pieces.Length; i++)
            {
                // 半分ずつ左右の角から、内側の上へ
                var side = i % 2 == 0 ? -1f : 1f;
                var angle = Random.Range(AngleMin, AngleMax) * Mathf.Deg2Rad;
                var speed = Random.Range(SpeedMin, SpeedMax);
                ref var piece = ref _pieces[i];
                piece.Position = new Vector2(side * half.x, -half.y);
                piece.Velocity = new Vector2(-side * Mathf.Sin(angle), Mathf.Cos(angle)) * speed;
                piece.Spin = Random.Range(-SpinMax, SpinMax);
                piece.Angle = Random.Range(0f, 360f);
                piece.Phase = Random.Range(0f, Mathf.PI * 2f);
                piece.Age = 0f;
                piece.Image.color = _colors[Random.Range(0, _colors.Length)];
                piece.Rect.sizeDelta = new Vector2(Random.Range(PieceSizeMin.x, PieceSizeMax.x), Random.Range(PieceSizeMin.y, PieceSizeMax.y));
                piece.Rect.gameObject.SetActive(true);
            }
        }

        public void Clear()
        {
            if (_pieces == null)
            {
                return;
            }

            foreach (var piece in _pieces)
            {
                piece.Rect.gameObject.SetActive(false);
            }
        }

        private void Build()
        {
            if (_pieces != null)
            {
                return;
            }

            _area = (RectTransform)transform;
            _pieces = new Piece[PieceCount];
            for (var i = 0; i < PieceCount; i++)
            {
                var obj = new GameObject($"Piece{i}", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(transform, false);
                var image = obj.GetComponent<Image>();
                // 飾りなので、ボタンを押す邪魔をしない
                image.raycastTarget = false;
                obj.SetActive(false);
                _pieces[i] = new Piece { Rect = (RectTransform)obj.transform, Image = image };
            }
        }

        private void Update()
        {
            if (_pieces == null)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < _pieces.Length; i++)
            {
                ref var piece = ref _pieces[i];
                if (!piece.Rect.gameObject.activeSelf)
                {
                    continue;
                }

                Step(ref piece, dt);
            }
        }

        private static void Step(ref Piece piece, float dt)
        {
            piece.Age += dt;
            if (piece.Age >= LifeSeconds)
            {
                piece.Rect.gameObject.SetActive(false);
                return;
            }

            piece.Velocity.y -= Gravity * dt;
            piece.Velocity *= Mathf.Exp(-Drag * dt);
            piece.Velocity.y = Mathf.Max(piece.Velocity.y, -FallSpeedMax);
            piece.Position += piece.Velocity * dt;
            piece.Angle += piece.Spin * dt;
            var sway = Mathf.Sin(piece.Age * SwayRate + piece.Phase) * SwayAmount;
            piece.Rect.anchoredPosition = piece.Position + new Vector2(sway, 0f);
            // 紙が裏返るように横幅だけを揺らす
            piece.Rect.localRotation = Quaternion.Euler(0f, 0f, piece.Angle);
            piece.Rect.localScale = new Vector3(Mathf.Cos(piece.Age * SwayRate * 2f + piece.Phase), 1f, 1f);
            var color = piece.Image.color;
            color.a = Mathf.Clamp01((LifeSeconds - piece.Age) / FadeSeconds);
            piece.Image.color = color;
        }
    }
}

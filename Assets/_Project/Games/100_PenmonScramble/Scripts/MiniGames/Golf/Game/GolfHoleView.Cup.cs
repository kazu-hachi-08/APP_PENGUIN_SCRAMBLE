using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// カップ（黒い穴に白い縁）・旗（紅白の棒の先で、ゆっくりはためく。入ると跳ねる）・ティー（打ち出す所の濃い色のマット）・
    /// 池に落ちたときの水しぶきの輪
    /// </summary>
    public partial class GolfHoleView
    {
        private const float CupLift = 0.012f;
        private const float CupRimWiden = 0.07f;
        private const float CupRimHeight = 0.5f;
        private const float PoleHeight = 1.5f;
        private const float PoleWidth = 0.05f;
        private const int PoleStripes = 3;
        private const float PoleStripeHeight = 0.12f;
        private static readonly Vector3 FlagScale = new Vector3(0.55f, 0.34f, 0.04f);
        private const float FlagTopGap = 0.04f;

        // はためき: 揺れる角度と速さ。カップに入ると、旗が上へ跳ねてくるっと回る
        private const float FlagWaveAngle = 14f;
        private const float FlagWaveRate = 0.7f;
        private const float FlagHopSeconds = 0.6f;
        private const float FlagHopHeight = 0.35f;

        // ティーのマット（カップの方へ向けた角の丸い板）と、左右の目印の玉
        private static readonly Vector3 TeeMatScale = new Vector3(1.6f, 0.04f, 1.0f);
        private const float TeeMarkerSize = 0.16f;
        private const float TeeMarkerSide = 0.95f;
        private static readonly Color TeeMatColor = new Color(0.25f, 0.55f, 0.32f);

        // 水しぶき: 白い輪が広がって消える
        private const float SplashSeconds = 0.45f;
        private const float SplashStartSize = 0.3f;
        private const float SplashEndSize = 1.4f;
        private const float SplashHeight = 0.25f;

        private static readonly Color CupColor = new Color(0.08f, 0.08f, 0.10f);
        private static readonly Color PoleColor = new Color(0.97f, 0.97f, 0.97f);
        private static readonly Color PoleStripeColor = new Color(0.92f, 0.28f, 0.26f);

        private readonly List<Splash> _splashes = new List<Splash>();
        private Transform _flag;
        private float _flagTime;
        private float _flagHop = 1f;

        private void BuildCup(GolfHoleDef hole, StageTheme theme)
        {
            var ground = Surface.HeightAt(hole.Cup);
            var at = GolfSpace.ToWorld(hole.Cup, ground);
            var diameter = GolfBalance.CupRadius * 2f;
            AddPart("Cup", ToyMeshes.FloorDisc, _course, at + Vector3.up * CupLift, Quaternion.identity,
                new Vector3(diameter, 1f, diameter), Materials.Floor(Texture2D.whiteTexture, CupColor));
            var rim = diameter + CupRimWiden;
            AddPart("CupRim", ToyMeshes.Torus, _course, at + Vector3.up * CupLift, Quaternion.identity,
                new Vector3(rim, CupRimHeight, rim), Materials.Get(Color.white, 0.4f));

            var pole = new GameObject("Pole").transform;
            pole.SetParent(_course, false);
            pole.localPosition = at;
            AddPart("Stick", ToyMeshes.Puck, pole, Vector3.up * (PoleHeight * 0.5f), Quaternion.identity,
                new Vector3(PoleWidth, PoleHeight, PoleWidth), Materials.Get(PoleColor, 0.5f));
            for (var i = 0; i < PoleStripes; i++)
            {
                var y = PoleHeight * (i + 1) / (PoleStripes + 1f);
                AddPart("Stripe", ToyMeshes.Puck, pole, Vector3.up * y, Quaternion.identity,
                    new Vector3(PoleWidth * 1.15f, PoleStripeHeight, PoleWidth * 1.15f), Materials.Get(PoleStripeColor, 0.5f));
            }

            // 旗は棒のてっぺんを軸に回す（はためく・跳ねる）
            _flag = new GameObject("Flag").transform;
            _flag.SetParent(pole, false);
            _flag.localPosition = Vector3.up * (PoleHeight - FlagScale.y * 0.5f - FlagTopGap);
            AddPart("Cloth", ToyMeshes.Puck, _flag, new Vector3(FlagScale.x * 0.5f, 0f, 0f), Quaternion.identity, FlagScale, Materials.Get(theme.FenceCap, 0.2f));
            _flagHop = 1f;
        }

        private void BuildTee(GolfHoleDef hole)
        {
            var toCup = GolfSpace.ToWorld(hole.Cup - hole.Tee);
            var rotation = Quaternion.LookRotation(toCup.sqrMagnitude > 0f ? toCup : Vector3.forward);
            var at = GroundPoint(hole.Tee);
            AddPart("TeeMat", ToyMeshes.Puck, _course, at, rotation, TeeMatScale, Materials.Get(TeeMatColor, 0.1f));
            foreach (var side in new[] { -1f, 1f })
            {
                AddPart("TeeMarker", ToyMeshes.Sphere, _course, at + rotation * new Vector3(side * TeeMarkerSide, TeeMarkerSize * 0.5f, 0f), Quaternion.identity,
                    Vector3.one * TeeMarkerSize, Materials.Get(Color.white, 0.8f));
            }
        }

        /// <summary>カップに入った: 旗が跳ねて1回転する</summary>
        public void CelebrateCup() => _flagHop = 0f;

        private void WaveFlag(float dt)
        {
            if (_flag == null)
            {
                return;
            }

            _flagTime += dt;
            _flagHop = Mathf.Min(1f, _flagHop + dt / FlagHopSeconds);
            var hop = Mathf.Sin(_flagHop * Mathf.PI) * FlagHopHeight;
            var spin = Mathf.SmoothStep(0f, 360f, _flagHop);
            var wave = Mathf.Sin(_flagTime * FlagWaveRate * Mathf.PI * 2f) * FlagWaveAngle;
            _flag.localPosition = Vector3.up * (PoleHeight - FlagScale.y * 0.5f - FlagTopGap + hop);
            _flag.localRotation = Quaternion.Euler(0f, wave + spin, 0f);
        }

        /// <summary>池に落ちた所に、白い輪を広げる</summary>
        public void SplashAt(Vec2 point)
        {
            var ring = AddPart("Splash", ToyMeshes.Torus, _course, GroundPoint(point) + Vector3.up * AreaLift * 4f, Quaternion.identity,
                Vector3.one * SplashStartSize, Materials.Get(FoamColor, 0.6f));
            _splashes.Add(new Splash(ring));
        }

        private void TickSplashes(float dt)
        {
            for (var i = _splashes.Count - 1; i >= 0; i--)
            {
                var splash = _splashes[i];
                splash.Time += dt;
                var t = Mathf.Clamp01(splash.Time / SplashSeconds);
                var size = Mathf.Lerp(SplashStartSize, SplashEndSize, 1f - (1f - t) * (1f - t));
                // 広がるほど輪を低くして、水面に消えていくように見せる
                splash.Ring.localScale = new Vector3(size, SplashHeight * size * (1f - t), size);
                if (t >= 1f)
                {
                    Destroy(splash.Ring.gameObject);
                    _splashes.RemoveAt(i);
                }
            }
        }

        private sealed class Splash
        {
            public readonly Transform Ring;
            public float Time;

            public Splash(Transform ring)
            {
                Ring = ring;
            }
        }
    }
}

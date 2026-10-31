using System.Collections;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// カメラの追い方と、闘技場に入ったときの演出（カメラが引いて全体を見せ、まん中に大ボスが降りてくる）
    /// </summary>
    public partial class StageView
    {
        // 協力で離れたときに引く上限。これより引くと、スマホの横画面でペンモンが小さくなりすぎる
        private const float ArenaMaxZoom = 1.3f;
        // プレイヤーと画面の端の間に空けておく幅（マス）。端に立つ仲間の体と頭の上の名前まで映す
        private const float FramingPadding = 1.5f;
        // 闘技場の全体を見せる長さ。大ボスが降りてきてから、これだけ見せたらプレイヤーへ戻る（秒）
        private const float ArenaIntroHoldSeconds = 1.2f;
        // 大ボスが出てこなかったときでも、いつかは戻る（秒）
        private const float ArenaIntroTimeoutSeconds = 4f;
        // 全体を見せた後、プレイヤーへゆっくり戻る長さ（秒）
        private const float ArenaIntroReturnSeconds = 1.5f;
        // 大ボスが降りてきて床に着いたときの揺れ（マス・秒）
        private const float BossLandShake = 0.35f;
        private const float BossLandShakeSeconds = 0.45f;
        private const float BossLandRingSeconds = 0.5f;
        private const float BossLandRingSpread = 3f;

        [SerializeField] private StageCamera _stageCamera;

        private readonly List<Vector2> _framed = new List<Vector2>();
        private int _cameraArea = -1;
        private float _introEndsAt = float.NegativeInfinity;

        private bool IsArenaIntro => Time.time < _introEndsAt;

        /// <summary>
        /// ふつうのエリアで出入口が閉じているときは、範囲がちょうど1エリアなので真ん中に止まる。
        /// 出入口が開いている間は、元気なプレイヤーの真ん中を横に追う（縦は今のエリアの中）。
        /// 闘技場の中では縦横に追い、協力で離れたら少し引く。入った直後は全体を見せる
        /// </summary>
        private void FollowCamera(StageSnapshot snapshot, bool snap)
        {
            var area = StageLayout.Area(StageTable.Get(snapshot.StageNo), snapshot.AreaIndex);
            if (snapshot.AreaIndex != _cameraArea)
            {
                // 挑戦の始め（snap）に闘技場から始まることはないが、作り直しのときは演出をしない
                var entered = !snap && _cameraArea >= 0 && area.IsEllipse;
                _cameraArea = snapshot.AreaIndex;
                _introEndsAt = entered ? Time.time + ArenaIntroTimeoutSeconds : float.NegativeInfinity;
            }

            var min = new Vector2(snapshot.AreaMin.X, area.Min.Y);
            var max = new Vector2(snapshot.AreaMax.X, area.Max.Y);
            if (IsArenaIntro)
            {
                var whole = area.Max.ToVector2() - area.Min.ToVector2();
                _stageCamera.Follow(area.Center.ToVector2(), CameraFraming.ZoomToShow(whole, StageCamera.View), snap, slow: true);
                return;
            }

            CollectFramedPlayers(snapshot);
            var maxZoom = area.IsEllipse ? ArenaMaxZoom : 1f;
            var (center, zoom) = CameraFraming.FollowPlayers(_framed, min, max, StageCamera.View, FramingPadding, maxZoom);
            // 全体を見せた後は、プレイヤーへゆっくり戻る
            var returning = Time.time < _introEndsAt + ArenaIntroReturnSeconds;
            _stageCamera.Follow(center, zoom, snap, returning);
        }

        private void CollectFramedPlayers(StageSnapshot snapshot)
        {
            _framed.Clear();
            foreach (var unit in snapshot.Units)
            {
                if (unit.Team == Team.Player && !unit.Has(UnitFlags.Down))
                {
                    _framed.Add(unit.Position.ToVector2());
                }
            }
        }

        /// <summary>大ボスが現れた: 空から闘技場のまん中へ降りてきて、着いたら床が揺れる</summary>
        private void DropInBigBoss(UnitView view)
        {
            view.PlayDropIn();
            if (!_preview)
            {
                StartCoroutine(LandBigBoss(view));
            }
        }

        private IEnumerator LandBigBoss(UnitView view)
        {
            yield return new WaitForSeconds(BigBossMotion.DropFallSeconds);
            // 全体を見せている間に降りてきたら、着いてから少し見せて戻る
            if (IsArenaIntro)
            {
                _introEndsAt = Mathf.Min(_introEndsAt, Time.time + ArenaIntroHoldSeconds);
            }

            if (view == null)
            {
                yield break;
            }

            _cameraShake.Shake(BossLandShake, BossLandShakeSeconds);
            GameSounds.Play(GameSound.BossLand);
            var floor = new Vector2(view.transform.position.x, view.transform.position.z);
            BurstFx.SpawnOnFloor(transform, DecalSprites.Ring, _materials.Decal, Color.white, floor, view.Radius, view.Radius * BossLandRingSpread, BossLandRingSeconds);
        }
    }
}

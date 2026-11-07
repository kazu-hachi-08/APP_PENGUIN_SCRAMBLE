using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 盤の上の子たち（打つ子・投げる子・守備の子とキャッチャー）。起きたこと（当たった・穴に入った・ミット）と1球の結果に合わせて、
    /// 走る・捕る・喜ぶ・悔しがるを振り分ける
    /// </summary>
    public sealed class BaseballFigures
    {
        // 盤に重ねた面（土・線・穴の札）より上に立たせる（足元の丸い影が面の下に隠れないように）
        private const float GroundLift = 0.06f;

        private readonly BaseballBatterView _batter;
        private readonly BaseballPitcherView _pitcher;
        private readonly BaseballFielders _fielders;

        public BaseballFigures(BaseballFieldDef field, ToonMaterials materials, Transform parent)
        {
            var ground = new GameObject("Figures").transform;
            ground.SetParent(parent, false);
            ground.localPosition = Vector3.up * GroundLift;
            parent = ground;
            _batter = new BaseballBatterView(field, materials, parent);
            _pitcher = new BaseballPitcherView(field, materials, parent);
            _fielders = new BaseballFielders(field, materials, parent);
            Runners = new BaseballRunners(field, materials, parent);
        }

        /// <summary>塁の上の走者</summary>
        public BaseballRunners Runners { get; }

        /// <summary>打つ子の今いる所（塁に出たら、ここから走者の子が走り出す）</summary>
        public Vector3 BatterPosition => _batter.Position;

        /// <summary>打つ子を隠す（塁に出て、走者の子と入れ替わった）</summary>
        public void HideBatter() => _batter.Hide();

        /// <summary>投げる子だけ反応する（打者が塁に出たとき。打つ子は走者になって走る）</summary>
        public void PitcherReact(bool cheer, bool despair) => _pitcher.React(cheer, despair);

        /// <summary>守る側の子を並べる（投げる子は SetPitcher で別に選ぶ）</summary>
        public void SetDefense(IReadOnlyList<int> species, bool wild) => _fielders.Use(species, wild);

        public void SetPitcher(int species, bool wild) => _pitcher.Use(species, wild);

        public void SetBatter(int species, bool wild) => _batter.Use(species, wild);

        public void Tick(BaseballBoard board, BaseballFrame frame, Vector3 ball, float deltaTime)
        {
            var ballVisible = board.Phase != BaseballBoardPhase.Ready;
            _batter.Tick(board, frame.BatDegrees, deltaTime);
            _pitcher.Tick(board, ball, ballVisible, deltaTime);
            _fielders.Tick(board, ball, ballVisible, deltaTime);
            Runners.Tick(deltaTime);
        }

        /// <summary>振りかぶりの深さ（0〜1）。1 になったら Release で投げる</summary>
        public void WindUp(float depth) => _pitcher.WindUp(depth);

        public void Release() => _pitcher.Release();

        public void OnEvent(BaseballEvent e, BaseballBoard board)
        {
            if (e.Kind == BaseballEventKind.Contact)
            {
                _batter.RunToFirst();
            }

            _fielders.OnEvent(e);
        }

        /// <summary>
        /// 1球の結果への反応。打席が終わった（三振・四球・打った）ときだけ大きく反応する
        /// （ホームラン・ヒットで打つ子が喜んで投げる子が悔しがる、アウト・三振はその逆、四球は一塁へ歩く）
        /// </summary>
        public void React(BaseballPitchOutcome outcome, BaseballPlateResult plate)
        {
            switch (plate)
            {
                case BaseballPlateResult.Hit:
                    _batter.React(true, false);
                    _pitcher.React(false, true);
                    break;
                case BaseballPlateResult.Out:
                case BaseballPlateResult.Strikeout:
                    _batter.React(false, true);
                    _pitcher.React(true, false);
                    break;
                case BaseballPlateResult.Walk:
                    _batter.WalkToFirst();
                    break;
            }
        }

        /// <summary>次の球の構えに戻す（打者は打席へ、投げる子はマウンドへ、守備は構えへ）</summary>
        public void ResetForPitch()
        {
            _batter.EnterStance(false);
            _pitcher.Ready(false);
            _fielders.Ready();
        }
    }
}

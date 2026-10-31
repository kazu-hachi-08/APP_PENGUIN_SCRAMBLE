using System.Collections.Generic;
using System.Text;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 「確率」の窓: ★ごとの出る確率と、その★の子の名前、引いたときのパワーの範囲。
    /// 数値は Balance と SpeciesTable から作るので、調整しても窓を直さなくてよい
    /// </summary>
    public partial class GachaScreen
    {
        [SerializeField] private Button _ratesButton;
        [SerializeField] private GameObject _ratesDialog;
        [SerializeField] private Text _ratesText;
        [SerializeField] private Button _ratesCloseButton;

        private void AwakeRates()
        {
            _ratesButton.onClick.AddListener(OpenRates);
            _ratesCloseButton.onClick.AddListener(CloseRates);
        }

        private void OpenRates()
        {
            _ratesText.text = RatesText(Record.WorldsUnlocked);
            _ratesDialog.SetActive(true);
        }

        private void CloseRates() => _ratesDialog.SetActive(false);

        private static string RatesText(int worldsUnlocked)
        {
            var text = new StringBuilder();
            for (var stars = Balance.MaxStars; stars >= 1; stars--)
            {
                var names = SpeciesNames(stars);
                var rate = Balance.StarRatePercent(stars);
                var color = ColorUtility.ToHtmlStringRGB(PenguinLabels.StarColor(stars));
                var each = names.Count > 0 ? rate / (float)names.Count : 0f;
                text.AppendLine($"<color=#{color}>{PenguinLabels.Stars(stars)}</color>　<b>{rate}%</b>　<size=26>（1種類あたり {each:0.##}%）</size>");
                text.AppendLine($"<size=28>{string.Join("・", names)}</size>");
                text.AppendLine();
            }

            text.Append($"パワー: {Balance.PowerMin(worldsUnlocked)}〜{Balance.PowerMax(worldsUnlocked)}" +
                        $"（★2 は ×{Balance.StarPowerScale(2):0.0#}、★3 は ×{Balance.StarPowerScale(3):0.0#}）\n" +
                        "<size=26>ワールドを解放するほど、強い子が出る</size>");
            return text.ToString();
        }

        private static List<string> SpeciesNames(int stars)
        {
            var names = new List<string>();
            foreach (var species in SpeciesTable.Species)
            {
                if (species.Stars == stars)
                {
                    names.Add(species.Name);
                }
            }

            return names;
        }
    }
}

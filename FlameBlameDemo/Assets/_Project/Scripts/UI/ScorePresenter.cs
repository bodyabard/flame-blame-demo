using FlameBlame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace FlameBlame.UI
{
    public class ScorePresenter : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI scoreLabel, timerLabel, summaryLabel, reasonLabel, popupTemplate;
        [SerializeField] GameObject resultPanel;
        [SerializeField] Button restartButton;
        [SerializeField] Image shockFlash;
        [SerializeField] float popupSeconds = 2.2f;
        [SerializeField] float popupRise = 35f;
        [SerializeField] float flashDecay = 2.5f;
        sealed class Popup
        {
            public TextMeshProUGUI Text;
            public RectTransform Rect;
            public float TimeLeft;
            public Vector2 Origin;
            public readonly char[] Buffer = new char[192];
        }
        readonly Popup[] active = new Popup[8];
        ObjectPool<Popup> pool;
        int count, previousSecond = -1;
        [SerializeField] WindowEntryGate entryGate;
        float flash;
        DamageLedger ledger;
        GameStateMachine game;
        IGameEvents events;
        RestartSceneCommand restart;

        public void Initialize(DamageLedger damageLedger, GameStateMachine state, IGameEvents gameEvents)
        {
            ledger = damageLedger;
            game = state;
            timerLabel.enableAutoSizing=true;
            timerLabel.fontSizeMin=18;
            timerLabel.fontSizeMax=timerLabel.fontSize;
            events = gameEvents;
            restart = new RestartSceneCommand();
            scoreLabel.SetText("ЗБИТКИ  $999999999"); scoreLabel.ForceMeshUpdate(true, true);
            timerLabel.SetText("ЧАС  999 с"); timerLabel.ForceMeshUpdate(true, true);
            resultPanel.SetActive(false);
            popupTemplate.gameObject.SetActive(false);
            pool = new ObjectPool<Popup>(() =>
            {
                var text = Instantiate(popupTemplate, popupTemplate.transform.parent);
                text.SetText(new string('W', 192));
                text.ForceMeshUpdate(true, true);
                return new Popup { Text = text, Rect = text.rectTransform };
            }, p => p.Text.gameObject.SetActive(true), p => p.Text.gameObject.SetActive(false),
                p => Destroy(p.Text.gameObject), false, active.Length, active.Length);
            for (int i = 0; i < active.Length; i++) active[i] = pool.Get();
            for (int i = 0; i < active.Length; i++) { pool.Release(active[i]); active[i] = null; }
            ledger.Changed += OnScore;
            events.DamageReported += OnReport;
            events.PlayerShocked += OnShock;
            game.Changed += OnPhase;
            restartButton.onClick.AddListener(Restart);
            OnScore(ledger.Total);
        }
        void Restart() => restart.Execute();
        void OnScore(int total) => scoreLabel.SetText("ЗБИТКИ  ${0:0}", total);
        void OnShock() { flash = 0.65f; }
        void OnReport(DamageReport report)
        {
            if (game.Phase != GamePhase.Playing) return;
            if (count == active.Length) Release(0);
            var p = pool.Get();
            p.TimeLeft = popupSeconds;
            p.Origin = popupTemplate.rectTransform.anchoredPosition + Vector2.down * count * 30f;
            p.Rect.anchoredPosition = p.Origin;
            p.Text.alpha = 1f;
            int length = 0;
            Append(p.Buffer, ref length, report.Subject);
            Append(p.Buffer, ref length, ": -$");
            AppendNumber(p.Buffer, ref length, report.Amount);
            Append(p.Buffer, ref length, " (");
            Append(p.Buffer, ref length, report.Reason);
            Append(p.Buffer, ref length, ")");
            p.Text.SetCharArray(p.Buffer, 0, length);
            active[count++] = p;
        }
        static void Append(char[] buffer, ref int length, string value)
        {
            for (int i = 0; i < value.Length && length < buffer.Length; i++) buffer[length++] = value[i];
        }
        static void AppendNumber(char[] buffer, ref int length, int number)
        {
            int divisor = 1;
            while (number / divisor >= 10 && divisor <= 100000000) divisor *= 10;
            do
            {
                if (length < buffer.Length) buffer[length++] = (char)('0' + number / divisor % 10);
                divisor /= 10;
            } while (divisor > 0);
        }
        void OnPhase(GamePhase phase)
        {
            if (phase != GamePhase.Result) return;
            resultPanel.SetActive(true);
            summaryLabel.SetText("ПІДСУМОК ХАОСУ\n\nЗбитки: ${0:0}\nПригод: {1:0}\nЧас: {2:0} с", ledger.Total, ledger.Incidents, game.Elapsed);
            reasonLabel.text = game.Ending == RoundEnding.FiresOut ? "Вогню немає. Рахунок залишився."
                : game.Ending == RoundEnding.DamageLimit ? "Страхова компанія вийшла з чату." : "Зміна закінчилась. Ремонт — ні.";
        }
        void Update()
        {
            if (game == null) return;
            int second = Mathf.CeilToInt(game.Remaining);
            if(entryGate!=null && !entryGate.Entered) timerLabel.SetText("Злети до вікна: Q — режим, ЛКМ — тяга");
            if ((entryGate==null || entryGate.Entered) && second != previousSecond) { previousSecond = second; timerLabel.SetText("ЧАС  {0:0} с", second); }
            float dt = Time.unscaledDeltaTime;
            flash = Mathf.MoveTowards(flash, 0f, dt * flashDecay);
            shockFlash.color = new Color(1f, 0.05f, 0f, flash);
            for (int i = count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.TimeLeft -= dt;
                if (p.TimeLeft <= 0f) { Release(i); continue; }
                p.Rect.anchoredPosition = p.Origin + Vector2.up * ((popupSeconds - p.TimeLeft) * popupRise);
                p.Text.alpha = Mathf.Clamp01(p.TimeLeft);
            }
        }
        void Release(int index)
        {
            pool.Release(active[index]);
            active[index] = active[--count];
            active[count] = null;
        }
        void OnDestroy()
        {
            if (events == null) return;
            ledger.Changed -= OnScore;
            events.DamageReported -= OnReport;
            events.PlayerShocked -= OnShock;
            game.Changed -= OnPhase;
            restartButton.onClick.RemoveListener(Restart);
        }
    }
}

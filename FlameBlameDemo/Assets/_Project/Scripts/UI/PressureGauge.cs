using FlameBlame.Feedback;
using FlameBlame.Hose;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FlameBlame.UI
{
    public sealed class PressureGauge : MonoBehaviour
    {
        [SerializeField] HoseController hose;
        [SerializeField] VisualTuning tuning;
        [SerializeField] RectTransform needle;
        [SerializeField] Image fill;
        [SerializeField] TextMeshProUGUI label;
        float pressure;
        int last=-1;
        void Awake() {label.SetText("ТИСК 100%");label.ForceMeshUpdate();}
        void Update()
        {
            pressure=Mathf.Lerp(pressure,Mathf.Clamp01(hose.Pressure),1-Mathf.Exp(-tuning.gaugeResponse*Time.deltaTime));
            needle.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(120,-120,pressure));
            fill.fillAmount=pressure; fill.color=Color.Lerp(new Color(.9f,.12f,.05f),tuning.hudAccent,pressure);
            int value=Mathf.RoundToInt(pressure*100);
            if(value!=last) {last=value;label.SetText("ТИСК  {0:0}%",value);}
        }
    }
}

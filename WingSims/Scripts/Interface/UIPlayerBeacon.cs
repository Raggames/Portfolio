using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WingsSim.Control;

public class UIPlayerBeacon : MonoBehaviour
{
    public AircraftController AircraftController;
    public Vector3 Offset = new Vector3(0, 15, 0);
    public float Margin;

    private TextMeshProUGUI info_Text;
    private Vector3 pos;
    private RectTransform rect;

    private void Awake()
    {
        info_Text = GetComponentInChildren<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
    }

    public void Initialize(AircraftController aircraftController)
    {
        this.AircraftController = aircraftController;
    }

    private void Update()
    {
        if (GameManager.LocalPlayerAircraftController == null)
            return;

        if (Vector3.Dot(AircraftController.transform.position - GameManager.LocalPlayerAircraftController.transform.position, GameManager.LocalPlayerAircraftController.transform.forward) <= 0)
        {

            pos = Camera.main.WorldToScreenPoint(AircraftController.transform.position);

            Vector3 dir = pos - Camera.main.transform.position;
            dir.z = 0;
            dir.x = -dir.x;
            dir.Normalize();
            Vector3 center = new Vector3(Screen.width / 2, Screen.height / 2, 0);

            pos = center + dir * Screen.width;
        }
        else
            pos = Camera.main.WorldToScreenPoint(AircraftController.transform.position) + Offset;

        if (pos.x > Screen.width - Margin)
        {
            pos.x = Screen.width - Margin;
        }
        else if (pos.x < 0 + Margin)
        {
            pos.x = Margin;
        }

        if (pos.y > Screen.height - Margin)
        {
            pos.y = Screen.height - Margin;
        }
        else if (pos.y < 0 + Margin)
        {
            pos.y = Margin;
        }

        this.transform.position = pos;

        float offset = GetHorizontalOffset();
        if(offset != 0)
        {
            this.transform.position += offset * Vector3.right;
        }

        info_Text.text = "Player_" + AircraftController.netIdentity.netId + " - " + Mathf.Round(Vector3.Distance(GameManager.LocalPlayerAircraftController.transform.position, AircraftController.transform.position)).ToString() + " m";
    }

    public float GetHorizontalOffset()
    {
        if (this.rect.anchoredPosition.x + info_Text.rectTransform.sizeDelta.x / 2 > Screen.width)
            return -info_Text.rectTransform.sizeDelta.x / 4;

        if (this.rect.anchoredPosition.x + info_Text.rectTransform.sizeDelta.x / 2 < 0)
            return info_Text.rectTransform.sizeDelta.x / 4;
        return 0;
    }
}
